---
name: dotnet-adversarial-testing
description: |
  Activa cuando se escriben tests adversariales en un microservicio ASP.NET Core 10:
  inputs hostiles, valores extremos, inyecciones, fallos de dependencias, o
  comportamiento bajo condiciones de error y concurrencia. Triggers: "test adversarial",
  "adversarial testing", "boundary testing", "valor extremo", "null input",
  "empty string", "string vacío", "input muy largo", "overflow", "underflow",
  "int.MaxValue", "long.MaxValue", "decimal muy grande", "inyección SQL en test",
  "XSS en test", "input malicioso", "upstream caído", "timeout simulado",
  "respuesta malformada", "dependency failure", "falla de dependencia",
  "CancellationToken cancelado", "token expirado", "race condition en test",
  "concurrencia en test", "WireMock.Net", "MockHttpMessageHandler",
  "Polly en test", "circuit breaker en test", "simular falla", "JSON inválido",
  "campo faltante en respuesta", "límite de recursos", "payload malicioso",
  "session expirada", "token revocado", "fuzzing", "property-based testing",
  "FsCheck", "inputs hostiles", "respuesta inesperada del upstream". Garantiza que
  el sistema se comporta correctamente ante inputs hostiles, dependencias que fallan,
  y condiciones de borde que no aparecen en tests normales. Complementa a
  dotnet-unit-testing. NO activar para: tests de flujo feliz (eso es
  dotnet-unit-testing), ni tests de carga/performance.
---

# .NET Adversarial Testing

## Objetivo

Los tests unitarios normales prueban el flujo feliz. Los tests adversariales prueban
lo que pasa cuando algo sale mal: el input viene vacío, el upstream tarda 30 segundos
y devuelve un JSON malformado, se recibe `long.MaxValue` como monto, el
`CancellationToken` está cancelado antes de empezar, o hay una race condition al
acceder a estado compartido. Este skill define cómo escribir tests que buscan
vulnerabilidades y comportamientos incorrectos ante condiciones hostiles en
microservicios ASP.NET Core 10.

## Cuándo activar

- Se escribe un test que simula un input inválido, extremo o malicioso.
- Se testea el comportamiento cuando una dependencia (repositorio, cliente HTTP) falla.
- Se usan `MockHttpMessageHandler`, `WireMock.Net`, o `Mock<T>.Setup(...).ThrowsAsync(...)`.
- Se testea la resiliencia de Polly (retry, circuit breaker) o el comportamiento
  ante `CancellationToken` cancelado.
- Se testa el comportamiento ante payloads con caracteres de inyección (XSS, SQL injection).
- Se testea concurrencia o acceso a estado compartido bajo carga simulada.

## Cuándo NO activar

- Tests del flujo feliz (ver `dotnet-unit-testing`).
- Tests de carga/performance.

## Estado actual vs target

- **Target:** xUnit 2.x + Moq para simular fallas de dependencias (`.ThrowsAsync()`,
  `.ReturnsAsync()` con respuestas malformadas), `MockHttpMessageHandler` o
  `WireMock.Net` para simular upstreams HTTP lentos, con errores o con respuestas
  inesperadas, `FsCheck` (opcional) para property-based testing de validadores y
  parsers, `CancellationTokenSource` para simular cancelaciones.
- La suite adversarial debe correr en CI junto con los tests unitarios.

## Decisiones del proyecto

- **Tres categorías de tests adversariales:**
  1. **Inputs hostiles:** `null`, string vacío, string muy largo, caracteres
     especiales (SQL injection strings, XSS strings, Unicode extremo, secuencias
     de escape), números en límites (`int.MaxValue`, `long.MinValue`,
     `decimal.MaxValue`, `0`, `-1`, `NaN` para floats), GUIDs malformados, fechas
     inválidas.
  2. **Dependencias que fallan:** timeout, connection refused, 5xx, 4xx inesperado,
     JSON inválido en la respuesta, campos faltantes en la respuesta JSON, DB no
     disponible, `CancellationToken` cancelado.
  3. **Condiciones de borde de negocio:** monto cero, monto negativo, moneda
     inválida, cuenta inexistente, token expirado/revocado, sesión concurrente.
- **`MockHttpMessageHandler` para upstreams HTTP** en tests unitarios: simula
  latencia, errores, respuestas malformadas sin necesidad de servidor real.
- **`WireMock.Net`** para tests de integración que necesitan simular un servidor
  HTTP real con comportamientos complejos (delays, fault injection, respuestas
  condicionales).
- **`CancellationTokenSource`** para simular cancelación: el código debe respetar
  el token y no operar en estado corrupto tras la cancelación.
- **FsCheck para property-based testing** de validadores y parsers: genera cientos
  de inputs al azar y verifica que se cumpla una propiedad invariante (ej.: todo
  input procesado produce un resultado no-null o lanza una excepción tipada EPA).

## Reglas obligatorias

### MUST

1. **MUST testear `null` y strings vacíos** en todos los inputs que acepta el
   endpoint o el service. El sistema debe rechazar (excepción EPA tipada o 400)
   o manejar controladamente, nunca lanzar `NullReferenceException`.

2. **MUST testear valores extremos** en números (`0`, `-1`, `int.MaxValue`,
   `long.MinValue`, `decimal.MaxValue`, montos con muchos decimales) y fechas
   (`DateTimeOffset.MinValue`, fechas en el pasado lejano, fechas futuras lejanas).

3. **MUST testear la respuesta ante fallas del upstream.** Simular timeout (delay
   - `CancellationToken`), 500, 503, y respuesta malformada (JSON inválido, campos
     nulos inesperados). Verificar que Polly (retry/circuit breaker) y el fallback
     funcionan.

4. **MUST testear que los inputs con caracteres de inyección** (SQL injection strings
   como `' OR '1'='1`, XSS strings como `<script>alert(1)</script>`, path traversal
   como `../../etc/passwd`) son rechazados en el borde (400/excepción EPA) o
   neutralizados, nunca procesados ciegamente.

5. **MUST testear el comportamiento ante `CancellationToken` cancelado.** El
   código que recibe un token cancelado debe respetar la cancelación:
   `OperationCanceledException` se propaga o el método termina sin efectos
   colaterales parciales.

6. **MUST testear respuestas upstream inesperadas:** JSON inválido (no parseable),
   campos requeridos ausentes en el payload de respuesta, tipos de datos erróneos.
   El service no debe lanzar `JsonException` sin atrapar ni propagar como
   excepción EPA tipada.

### MUST NOT

7. **MUST NOT asumir que Data Annotations cubre todos los casos adversariales.**
   Las annotations cubren el flujo feliz; los tests adversariales buscan los
   bordes que los constraints no alcanzan (combinaciones de campos, overflow de
   tipos, secuencias de eventos, estado parcial).

8. **MUST NOT dejar solo en el dev la responsabilidad de imaginar casos adversariales.**
   Usar `[Theory]` + `[InlineData]` con payloads hostiles enumerados, y considerar
   FsCheck para generación automática de casos.

9. **MUST NOT usar I/O real** (HTTP a servicios reales, DB real) en tests
   adversariales unitarios. Mockear con `MockHttpMessageHandler` o `WireMock.Net`
   en modo standalone.

## Recomendaciones

### SHOULD

- Organizar los tests adversariales en una clase separada por categoría:
  `SaldoService_InputHostilesTests`, `SaldoService_UpstreamFalluresTests`,
  `SaldoService_CancellationTests`.
- Usar `FsCheck` para validadores y parsers que aceptan texto libre: genera
  combinaciones que el dev nunca imaginaría manualmente.
- Incluir los tests adversariales en el mismo pipeline de CI que los tests
  unitarios; no son opcionales.
- Verificar que los errores adversariales producen el tipo de excepción EPA
  correcto (`ValidationException`, `NetworkException`, `NotFoundException`) y no
  excepciones genéricas del framework.

### SHOULD NOT

- No duplicar la lógica del flujo feliz en los tests adversariales. El objetivo
  es cubrir únicamente los paths de error y los bordes.
- No simular condiciones de error que no pueden ocurrir en producción. Cada caso
  adversarial debe ser realista o derivar de un requisito de seguridad.

## Anti-patrones prohibidos

❌ Solo testear el flujo feliz:

```csharp
[Fact]
public async Task Should_CrearCuenta_When_DatosValidos()
{
    // ❌ solo el caso feliz; qué pasa con nombre null? monto negativo? CBU malformado?
    await _service.CrearAsync(new CuentaRequest("Juan", "0000003100014477", 1000m), CancellationToken.None);
}
```

✅ Testear inputs adversariales con `[Theory]`:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
[InlineData("<script>alert(1)</script>")]
[InlineData("' OR '1'='1")]
[InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 256+ chars
public async Task Should_Throw_ValidationException_When_NombreInvalido(string? nombre)
{
    var request = new CuentaRequest(nombre!, "0000003100014477", 100m);
    await Assert.ThrowsAsync<ValidationException>(() =>
        _service.CrearAsync(request, CancellationToken.None)); // ✅ rechazado con excepción EPA tipada
}
```

❌ No testear fallas del upstream:

```csharp
// Solo se testa cuando el upstream responde 200;
// qué pasa con 503, timeout, JSON inválido, campo faltante?
```

✅ Simular upstream caído con `MockHttpMessageHandler`:

```csharp
[Fact]
public async Task Should_Throw_NetworkException_When_UpstreamTimesOut()
{
    // Arrange
    var handler = new Mock<HttpMessageHandler>();
    handler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
        .ThrowsAsync(new TaskCanceledException("timeout simulado")); // ✅ simula timeout

    var http = new HttpClient(handler.Object);
    var factoryMock = new Mock<IHttpClientFactory>();
    factoryMock.Setup(f => f.CreateClient("SaldoClient")).Returns(http);

    var service = new SaldoService(factoryMock.Object, _logger.Object);

    // Act & Assert
    await Assert.ThrowsAsync<NetworkException>(() =>
        service.GetSaldoAsync("ACC-001", CancellationToken.None)); // ✅ excepción EPA correcta
}

[Fact]
public async Task Should_Throw_NetworkException_When_UpstreamReturnsInvalidJson()
{
    // Arrange
    var handler = new Mock<HttpMessageHandler>();
    handler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{{invalid json}}") // ✅ JSON malformado
        });
    var http = new HttpClient(handler.Object);
    var factoryMock = new Mock<IHttpClientFactory>();
    factoryMock.Setup(f => f.CreateClient("SaldoClient")).Returns(http);

    var service = new SaldoService(factoryMock.Object, _logger.Object);

    // Act & Assert
    await Assert.ThrowsAsync<NetworkException>(() =>
        service.GetSaldoAsync("ACC-001", CancellationToken.None)); // ✅ no JsonException sin atrapar
}
```

❌ No testear cancelación:

```csharp
// Si el CancellationToken se cancela durante la operación, ¿qué pasa?
// ¿El método continúa? ¿Deja estado corrupto?
```

✅ Testear `CancellationToken` cancelado:

```csharp
[Fact]
public async Task Should_Cancel_Gracefully_When_TokenIsCancelled()
{
    // Arrange
    using var cts = new CancellationTokenSource();
    cts.Cancel(); // ✅ cancelar antes de llamar

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(() =>
        _service.ProcesarPagoAsync(new PagoRequest("ACC-001", 100m), cts.Token)); // ✅ propagado limpiamente
}
```

✅ Property-based testing con FsCheck para validadores:

```csharp
[Property]
public Property Monto_Negativo_SiempreRechazado(NegativeInt monto)
{
    var act = () => _service.ValidarMonto((decimal)monto.Item);
    // ✅ FsCheck genera cientos de montos negativos; la propiedad invariante es que siempre se rechaza
    return act.Should().Throw<ValidationException>().ToProperty();
}
```

❌ Valores extremos de números no testeados:

```csharp
[Fact]
public void Should_ValidarMonto_When_MontoPositivo()
{
    Assert.True(_service.ValidarMonto(100m)); // ❌ solo el caso común; qué pasa con decimal.MaxValue?
}
```

✅ Casos límite incluidos:

```csharp
[Theory]
[InlineData(0)]
[InlineData(-0.01)]
[InlineData(-1)]
[InlineData(long.MaxValue)] // overflow a decimal
[InlineData(double.NaN)]    // si se acepta double
public void Should_Throw_ValidationException_When_MontoInvalido(double monto)
{
    Assert.Throws<ValidationException>(() =>
        _service.ValidarMonto((decimal)monto)); // ✅ rechaza todos los bordes inválidos
}
```

## Checklist antes de devolver código

- [ ] Se testearon inputs `null`, vacíos, y con caracteres especiales (XSS, SQL injection).
- [ ] Se testearon valores numéricos extremos (0, negativos, `int.MaxValue`, `decimal.MaxValue`).
- [ ] Se simuló la falla del upstream (timeout, 5xx, JSON inválido) y se verificó la excepción EPA.
- [ ] Se testeó el comportamiento ante `CancellationToken` cancelado.
- [ ] Los tests adversariales están en CI junto con los unitarios.
- [ ] Los errores producen excepciones EPA tipadas, no excepciones genéricas del framework.

## Conexiones con otros skills

- `dotnet-unit-testing` — los adversariales usan las mismas herramientas (xUnit, Moq, `WebApplicationFactory`) pero con foco en inputs hostiles y fallas.
- `aspnetcore-security-owasp-baseline` — los tests adversariales validan las defensas OWASP (injection, SSRF, IDOR).
- `aspnetcore-outgoing-http` — testear Polly (retry, circuit breaker) y fallback con `MockHttpMessageHandler` o `WireMock.Net`.
- `aspnetcore-error-and-observability` — verificar que los errores adversariales producen logs de nivel `LogWarning`/`LogError` con `ISSUE_LOG` EPA.

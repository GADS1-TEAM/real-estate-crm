---
name: dotnet-unit-testing
description: |
  Activa cuando se escriben, modifican o revisan tests unitarios o de integración
  en un microservicio ASP.NET Core 10 con xUnit y Moq. Triggers: "test unitario",
  "unit test", "xUnit", "[Fact]", "[Theory]", "[InlineData]", "Moq", "Mock<T>",
  "Setup", "Verify", "Returns", "ReturnsAsync", "mock de ILogger", "mock de ILogger<T>",
  "WebApplicationFactory", "WebApplicationFactory<T>", "integration test",
  "test de controller", "test de endpoint", "test de service", "test de repositorio",
  "cobertura", "coverage", "arrange-act-assert", "AAA", "test determinista",
  "mock del repositorio", "mock del HttpClient", "mock de IHttpClientFactory",
  "TimeProvider", "FluentAssertions", "Should()", "naming de test", "test aislado",
  "dependencias mockeadas", "InMemoryDatabase", "TestServer", "test de minimal API",
  "xUnit fixture", "IClassFixture", "test sin assertions", "test siempre pasa".
  Garantiza tests rápidos, deterministas, aislados (sin red ni I/O real en unitarios),
  con arrange-act-assert, mocks en las fronteras y assertions concretas. NO activar
  para: tests de carga/performance, ni tests adversariales (ver dotnet-adversarial-testing).
---

# .NET Unit Testing

## Objetivo

Un unit test bien hecho prueba **una unidad** (service, controller, clase de dominio)
con sus dependencias mockeadas en las fronteras (DB, HTTP, tiempo), es rápido
(milisegundos), determinista (mismo resultado siempre), y falla con un mensaje que
dice **qué** está mal sin abrir el código. Este skill define cómo escribir tests en
un microservicio ASP.NET Core 10 con xUnit y Moq, qué mockear, qué no, y cómo
estructurarlos para que sean mantenibles.

## Cuándo activar

- Se escribe o modifica un test `*Tests.cs`.
- Se agrega cobertura a un service, controller, repositorio, validator o parser.
- Se usan `Mock<T>`, `WebApplicationFactory<T>`, o `InMemory` de EF Core en tests.
- Se evalúa el setup de testing (mocks, fixtures, assertions).

## Cuándo NO activar

- Tests de carga/performance.
- Tests adversariales de inputs hostiles o fallas de dependencias (ver `dotnet-adversarial-testing`).

## Estado actual vs target

- **Target:** xUnit 2.x con `[Fact]` y `[Theory]` + `[InlineData]`, Moq 4.x para
  mocks, `WebApplicationFactory<T>` para tests de integración de endpoints,
  `FluentAssertions` (opcional) para assertions expresivas, `TimeProvider`
  de .NET 10 para inyectar tiempo determinista.
- **`WebApplicationFactory<T>`** levanta el host completo de ASP.NET Core en memoria
  sin I/O de red real; es el estándar para testear endpoints, middleware y pipeline.
- Evitar dependencias de I/O real (red, DB real, reloj del sistema) en tests
  unitarios. Usar `TimeProvider` fijo en vez de `DateTime.UtcNow`.

## Decisiones del proyecto

- **Un test = una unidad** con dependencias mockeadas en la frontera. No tests de
  integración disfrazados de unit.
- **Estructura AAA:** Arrange (setup), Act (llamada bajo test), Assert (verificación).
  Separados visualmente con una línea en blanco o comentario.
- **Mockear en la frontera:** mockear el repositorio, el cliente HTTP, el clock,
  no la clase que se está probando.
- **Tests deterministas:** usar `TimeProvider` (inyectable en .NET 10) para tiempo;
  no `DateTime.UtcNow` ni `DateTimeOffset.Now` en el código de producción.
- **`IHttpClientFactory` mockeado** para cualquier llamada HTTP saliente en tests
  unitarios. Nunca llamadas HTTP reales en tests unitarios.
- **Nombre descriptivo del test:** formato `Should_{resultado}_{when_condicion}` o
  equivalente BDD `Given_{contexto}_When_{accion}_Then_{resultado}`. El nombre del
  test es la documentación del comportamiento.
- **`Mock<ILogger<T>>`** para verificar logs cuando el comportamiento observable es
  que se loggea algo; de lo contrario, simplemente pasar `Mock<ILogger<T>>().Object`
  sin assertions sobre él.

## Reglas obligatorias

### MUST

1. **MUST estructurar cada test con AAA explícito** (Arrange-Act-Assert), con
   nombre descriptivo que describe el comportamiento esperado, no la implementación.

2. **MUST mockear las fronteras** (repositorios, clientes HTTP, `TimeProvider`,
   servicios externos) en tests unitarios. No usar la implementación real de la
   frontera.

3. **MUST usar `WebApplicationFactory<T>`** para tests de integración de endpoints
   y middleware del pipeline ASP.NET Core. Mockear los servicios de aplicación con
   `builder.ConfigureTestServices` o con el contenedor de sustitución.

4. **MUST usar `TimeProvider` inyectable** cuando el código de producción usa la
   fecha/hora. No `DateTime.UtcNow` hardcodeado en el código bajo test sin inyección.
   En tests, usar `TimeProvider.System` o un `FakeTimeProvider` de `Microsoft.Extensions.TimeProvider.Testing`.

5. **MUST usar assertions concretas.** Con xUnit: `Assert.Equal`, `Assert.True`,
   `Assert.Throws<T>`, etc. Con FluentAssertions: `.Should().Be()`, `.Should().Throw<T>()`.
   Los mensajes de error deben decir qué se esperaba vs. qué se obtuvo.

6. **MUST verificar el comportamiento, no la implementación.** `mock.Verify()` de
   Moq solo cuando el comportamiento observable es la llamada al colaborador, no
   para verificar que "se llamó el método interno X".

7. **MUST usar `[Theory]` + `[InlineData]`** para testear múltiples variantes de
   inputs con la misma lógica de test. Evita duplicación de tests con un solo dato
   cambiante.

8. **MUST iterar los tests** hasta que todos pasen y todos los branches alcanzables
   estén cubiertos con assertions reales. Si quedan branches sin ejercitar, agregar
   tests antes de entregar.

9. **MUST verificar que el proyecto tenga `sonar-project.properties`**. Si no existe,
   crearlo con:

   ```
   sonar.sources=.
   sonar.cs.opencover.reportsPaths=TestResults/**/coverage.opencover.xml
   ```

10. **MUST generar reporte de cobertura con Coverlet** en formato OpenCover al correr los tests:
    ```
    dotnet test --collect:"XPlat Code Coverage" ^
                --results-directory ./TestResults ^
                /p:CoverletOutputFormat=opencover
    ```

### MUST NOT

11. **MUST NOT tener I/O real** (llamadas HTTP, acceso a DB real, lectura de archivos,
    sleep/delay real) en tests unitarios. Mockear o usar implementaciones en memoria.

12. **MUST NOT mockear la clase bajo test** (`new Mock<MiService>()`). Es síntoma
    de diseño incorrecto. Se crea la instancia real y se mockean sus dependencias.

13. **MUST NOT tener assertions vacías** (`Assert.True(true)`) ni tests sin
    assertions. Un test que siempre pasa no prueba nada.

14. **MUST NOT testear código de frameworks** (que EF Core persiste, que el
    serializador JSON funciona). Testear el comportamiento propio del código.

15. **MUST NOT compartir estado mutable entre tests** en la misma clase xUnit.
    xUnit crea una nueva instancia de la clase por cada test; si se necesita estado
    compartido costoso, usar `IClassFixture<T>`.

## Recomendaciones

### SHOULD

- Usar `FluentAssertions` para assertions más expresivas y mensajes de error más
  claros: `resultado.Should().Be(expected)` vs. `Assert.Equal(expected, resultado)`.
- Usar `Mock<ILogger<T>>()` y no loggear noise en los tests; si se necesita
  verificar que se loggea un error, usar `mock.Verify(...)` con la expresión lambda
  adecuada.
- Nombrar el archivo de tests `{ClaseBajoTest}Tests.cs` y ubicarlo en el mismo
  namespace espejado del proyecto principal pero en el proyecto de tests.
- Preferir `IAsyncEnumerable` y `async Task` en tests async sobre `.Result` o
  `.GetAwaiter().GetResult()` que pueden causar deadlocks.

### SHOULD NOT

- No usar `Thread.Sleep` ni `Task.Delay` real en tests unitarios; si se necesita
  avanzar el tiempo, usar `FakeTimeProvider.Advance()`.
- No usar `Assert.Equal(true, condicion)` en lugar de `Assert.True(condicion)`.
  Los matchers específicos dan mejores mensajes de error.

## Anti-patrones prohibidos

❌ Test unitario con I/O real de HTTP:

```csharp
[Fact]
public async Task Should_ReturnSaldo_When_CuentaExists()
{
    // ❌ llama al HTTP real; no determinista, no portable, no rápido
    var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    var service = new SaldoService(client);
    var result = await service.GetSaldoAsync("123", CancellationToken.None);
    Assert.NotNull(result);
}
```

✅ Test unitario con `IHttpClientFactory` mockeado:

```csharp
[Fact]
public async Task Should_ReturnSaldo_When_CuentaExists()
{
    // Arrange
    var handlerMock = new Mock<HttpMessageHandler>();
    handlerMock.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SaldoDto { Monto = 1000m }) // ✅ respuesta simulada
        });
    var httpClient = new HttpClient(handlerMock.Object);
    var factoryMock = new Mock<IHttpClientFactory>();
    factoryMock.Setup(f => f.CreateClient("SaldoClient")).Returns(httpClient);

    var service = new SaldoService(factoryMock.Object, _logger.Object);

    // Act
    var result = await service.GetSaldoAsync("123", CancellationToken.None);

    // Assert
    Assert.Equal(1000m, result.Monto); // ✅ determinista, sin red real
}
```

❌ Nombre de test sin contexto, sin assertion concreta:

```csharp
[Fact]
public async Task Test1() // ❌ nombre sin semántica
{
    var result = await _service.ProcesarPagoAsync(new PagoRequest());
    Assert.True(result != null); // ❌ assertion débil: cualquier objeto no-null pasa
}
```

✅ Nombre descriptivo con assertion concreta:

```csharp
[Fact]
public async Task Should_DebitarImporte_When_SaldoEsSuficiente()
{
    // Arrange
    var cuentaId = "ACC-001";
    _cuentaRepoMock.Setup(r => r.FindAsync(cuentaId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Cuenta(cuentaId, saldo: 500m));

    // Act
    var resultado = await _service.ProcesarPagoAsync(
        new PagoRequest(cuentaId, Importe: 100m), CancellationToken.None);

    // Assert
    resultado.NuevoSaldo.Should().Be(400m); // ✅ assertion específica del valor esperado
    _cuentaRepoMock.Verify(r => r.SaveAsync(
        It.Is<Cuenta>(c => c.Saldo == 400m), It.IsAny<CancellationToken>()), Times.Once); // ✅ comportamiento verificado
}
```

❌ `DateTime.UtcNow` en el código de producción (no inyectable, no determinista en tests):

```csharp
public class TokenService
{
    public bool EsValido(Token token) =>
        token.Expira > DateTime.UtcNow; // ❌ no se puede controlar en tests
}
```

✅ `TimeProvider` inyectable (determinista en tests):

```csharp
public class TokenService(TimeProvider clock)
{
    public bool EsValido(Token token) =>
        token.Expira > clock.GetUtcNow(); // ✅ TimeProvider fijo en tests
}

// En el test:
var fakeTime = new FakeTimeProvider(DateTimeOffset.Parse("2026-07-05T12:00:00Z"));
var service = new TokenService(fakeTime);
Assert.True(service.EsValido(new Token(Expira: fakeTime.GetUtcNow().AddHours(1)))); // ✅
```

❌ Test de integración con `WebApplicationFactory` sin mockear dependencias externas:

```csharp
public class PagoControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Should_Return200_When_PagoValido()
    {
        // ❌ usa el HttpClient real que llama a servicios externos de QA
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/pagos", new { Monto = 100 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

✅ `WebApplicationFactory` con `ConfigureTestServices` para reemplazar dependencias:

```csharp
public class PagoControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PagoControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // ✅ reemplazar el servicio real por un mock
                var mockPagoService = new Mock<IPagoService>();
                mockPagoService.Setup(s => s.ProcesarAsync(
                    It.IsAny<PagoRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PagoResult(Aprobado: true, NuevoSaldo: 900m));
                services.AddSingleton(mockPagoService.Object);
            });
        });
    }

    [Fact]
    public async Task Should_Return200_When_PagoAprobado()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/pagos",
            new { CuentaId = "ACC-001", Monto = 100 });
        response.StatusCode.Should().Be(HttpStatusCode.OK); // ✅ sin I/O externo real
    }
}
```

## Checklist antes de devolver código

- [ ] Tests unitarios no tienen I/O real (HTTP, DB, reloj del sistema).
- [ ] Tests de endpoint usan `WebApplicationFactory<T>` con `ConfigureTestServices`.
- [ ] Cada test tiene estructura AAA explícita y nombre descriptivo.
- [ ] Assertions son concretas (no `Assert.True(x != null)`).
- [ ] El código de producción usa `TimeProvider` inyectable, no `DateTime.UtcNow`.
- [ ] No hay tests sin assertions.
- [ ] Se usa `[Theory]` + `[InlineData]` para múltiples variantes de input.
- [ ] Se iteró para cubrir todos los branches alcanzables con assertions reales.
- [ ] `sonar-project.properties` existe con `sonar.cs.opencover.reportsPaths`.
- [ ] Coverlet genera cobertura OpenCover en `TestResults/`.

## Conexiones con otros skills

- `dotnet-adversarial-testing` — los adversariales usan las mismas herramientas (xUnit, Moq, WebApplicationFactory) pero con foco en inputs hostiles y fallas.
- `aspnetcore-rest-layer` — `WebApplicationFactory<T>` testa la capa de controller incluyendo validación de DTOs.
- `aspnetcore-di-and-middleware-pipeline` — inyección por constructor facilita la creación de tests con Moq.
- `aspnetcore-error-and-observability` — mockear `ILogger<T>` y verificar que se loggean errores en las condiciones correctas.

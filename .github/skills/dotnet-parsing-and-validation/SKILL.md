---
name: dotnet-parsing-and-validation
description: |
  Activa cuando se parsea, valida o transforma datos que cruzan un borde de
  confianza en un microservicio ASP.NET Core 8 con arquetipo epa-net-paas:
  body/query/params de un request, respuestas de otros microservicios, documentos
  de la DB, mensajes de mensajería, o cualquier dato externo antes de usarlo.
  Triggers: "DTO", "[Required]", "[Range]", "[StringLength]", "[RegularExpression]",
  "[EmailAddress]", "[MinLength]", "[MaxLength]", "DataAnnotations", "IValidatableObject",
  "FluentValidation", "IValidator", "AbstractValidator", "ValidationException",
  "ModelState", "int.TryParse", "int.Parse", "DateTime.TryParse", "TryParse",
  "System.Text.Json", "JsonSerializerOptions", "JsonSerializer.Deserialize",
  "DeserializationException", "ValidationProblemDetails", "validar input",
  "validar request", "parsear payload", "mapear la respuesta de", "normalizar",
  "deserializar JSON", "campos opcionales", "input del usuario", "respuesta del
  servicio upstream", "mensaje de Kafka", "los datos vienen mal", "campo null
  inesperado", "tipo incorrecto", "límite de tamaño", "tamaño máximo del body",
  "MaxRequestBodySize". Garantiza que todo dato externo se valide en el borde con
  un esquema, se coercionen tipos de forma segura, se rechace lo inválido con la
  ValidationException EPA, y que la validación sea consistente en todos los bordes.
  NO activar para: datos ya validados que circulan internamente, ni constantes del
  código.
---

# .NET Parsing & Validation

## Objetivo

Todo dato que entra al microservicio desde afuera es **no confiable hasta probarlo**:
body del cliente, query params, respuesta de un microservicio upstream, mensaje de
mensajería, documento de la DB. La regla es **validar en el borde**: convertir el
dato externo en un objeto válido al punto de entrada, de forma que el resto del
código trabaje con datos que se sabe que son correctos. Este skill define cómo
validar con DataAnnotations y FluentValidation en ASP.NET Core 8, cuándo usar
`IValidatableObject` para validación cross-field, cómo parsear de forma segura con
`TryParse`, y cómo deserializar JSON con `System.Text.Json` con opciones estrictas.
La `ValidationException` del arquetipo `epa-net-paas` es el mecanismo estándar para
reportar errores de validación al cliente.

## Cuándo activar

- Se recibe input del usuario (body, query params, path variables, headers).
- Se consume la respuesta de un microservicio upstream.
- Se procesa un mensaje de mensajería antes de usarlo en la lógica de negocio.
- Se lee un documento/entidad de la DB y se transforma antes de devolver.
- Se hace deserialización JSON de cualquier fuente externa.
- Se parsea un string a un tipo (entero, fecha, enum, GUID).
- Se mapea o normaliza un payload externo.

## Cuándo NO activar

- Datos que ya fueron validados en el borde y circulan internamente tipados.
- Constantes del código (enums, valores fijos de dominio).
- Configuración del microservicio (tiene su propio skill).

## Estado actual vs target

- **Target:** DataAnnotations en DTOs de entrada para validaciones estructurales,
  `[ApiController]` activa la validación automática de modelo devolviendo 400 antes
  de llegar al action, FluentValidation con `IValidator<T>` para validaciones
  complejas o condicionales, `IValidatableObject` para validaciones cross-field en
  el mismo DTO, `ValidationException` EPA para errores de negocio que requieren
  acceso a la DB o lógica del service layer.
- **Arquetipo `epa-net-paas`:** `ValidationException` es la excepción tipada EPA
  para errores de validación; el middleware global la traduce a 400 con el formato
  `meta-data-error`. No hacer `try/catch` de `ValidationException` en el controller.

## Decisiones del proyecto

- **Validar en el borde, una vez, lo más temprano posible.** Después se confía
  en el tipo.
- **DataAnnotations como primera línea** para validaciones estructurales (campos
  no nulos, longitudes, formatos, rangos). Para validaciones de negocio que
  requieren acceso a la DB o dependencias externas, usar `ValidationException`
  en el service layer.
- **DTOs de entrada separados de los DTOs de respuesta** y de las entidades EF Core.
  No exponer entidades EF Core directamente como request body ni como response.
- **Validar respuestas de upstreams.** Un servicio puede devolver campos null
  inesperados o cambiar su contrato. No asumir que la respuesta es siempre válida.
- **`TryParse` sobre `Parse`** para datos externos. `int.Parse` lanza excepción
  si el input es inválido; `int.TryParse` permite manejar el error sin excepciones
  como control de flujo.
- **`System.Text.Json` con opciones estrictas** para deserialización: configurar
  `PropertyNameCaseInsensitive = false` en producción y definir explícitamente
  el comportamiento ante propiedades desconocidas según el caso.

## Reglas obligatorias

### MUST

1. **MUST anotar los DTOs de entrada con DataAnnotations** para validaciones
   estructurales. `[ApiController]` activa la validación automática de modelo;
   si la validación falla, devuelve 400 antes de ejecutar el action.

2. **MUST definir constraints en los DTOs de entrada**, no en la lógica del
   service. Las validaciones estructurales (campo requerido, longitud, formato,
   rango) van en la clase DTO con anotaciones.

3. **MUST lanzar `ValidationException` EPA** cuando la validación de negocio
   falla en el service layer (ej. "el CUIT ya existe", "la fecha de vencimiento
   es anterior a hoy"). El middleware global la traduce a 400 con formato EPA.

4. **MUST tratar los campos opcionales de upstreams como opcionales** en el DTO
   de mapeo. Usar tipos nullable (`string?`, `int?`) o `JsonIgnoreCondition` con
   manejo explícito, no asumir que siempre estarán presentes.

5. **MUST usar `TryParse` en lugar de `Parse`** para parsear strings de fuentes
   externas a tipos primitivos. `int.Parse("abc")` lanza excepción; `int.TryParse`
   retorna `false` y permite manejar el error sin usar excepciones como control
   de flujo.

6. **MUST usar DTOs de entrada separados** de las entidades EF Core y los DTOs de
   respuesta. No `[FromBody] CuentaEntity` ni retornar la entidad directamente.

7. **MUST configurar un límite de tamaño del body** en el endpoint o globalmente
   en el pipeline. Sin límite, un cliente puede enviar un payload de GBs y
   provocar OOM o DoS.

### MUST NOT

8. **MUST NOT hacer validación manual con `if`** para cosas que DataAnnotations
   puede cubrir (`if (dto.Nombre == null)` → usar `[Required]`).

9. **MUST NOT devolver mensajes de error de validación que incluyan** valores
   internos, stack traces, o información sobre el modelo de datos interno. El
   middleware EPA filtra esto, pero no lanzar excepciones con mensajes internos.

10. **MUST NOT asumir que los tipos de los campos de un JSON externo son los
    esperados.** Configurar `System.Text.Json` con las opciones correctas y
    manejar `JsonException` en la deserialización de datos de upstreams.

11. **MUST NOT usar `int.Parse`, `DateTime.Parse`, `Guid.Parse` en datos de
    fuentes externas** sin manejo de excepción o sin preferir la variante
    `TryParse`. El input externo nunca es garantizado.

## Recomendaciones

### SHOULD

- Usar FluentValidation (`AbstractValidator<T>`) cuando las reglas de validación
  son complejas, condicionales, o requieren dependencias (ej. un repositorio para
  verificar unicidad); es más testeable que DataAnnotations.
- Implementar `IValidatableObject` en el DTO para validaciones cross-field que
  no expresan las anotaciones estándar (ej. "la fecha de fin debe ser posterior
  a la de inicio").
- Centralizar el manejo de `ValidationProblemDetails` en el `IExceptionHandler`
  global para respuestas de error consistentes en formato EPA.
- Usar `[JsonPropertyName]` en los DTOs de respuesta para controlar exactamente
  los nombres de campo serializados, independientemente de la convención de nombres
  del proyecto.

### SHOULD NOT

- No usar `[Required]` en campos de tipo `int` o `bool` no-nullable; los tipos
  no-nullable ya son required por el runtime. Usar tipos nullable
  (`int?`, `bool?`) cuando el campo es genuinamente opcional.
- No usar `[RegularExpression]` para validar formatos complejos como CUIT/CUIL
  o CBU; preferir un `ConstraintValidator` o método de validación dedicado con
  lógica propia y un mensaje de error claro.

## Anti-patrones prohibidos

❌ DTO de entrada sin DataAnnotations (validación manual ad-hoc):

```csharp
// ❌ validación manual en el service; no llega al borde; permite input inválido
public async Task<CuentaDto> CrearCuentaAsync(CuentaRequest req, CancellationToken ct)
{
    if (string.IsNullOrEmpty(req.Titular))
        throw new ArgumentException("Titular requerido");
    if (req.Saldo < 0)
        throw new ArgumentException("Saldo inválido");
    // ...
}
```

✅ DataAnnotations en el DTO de entrada:

```csharp
// ✅ [ApiController] valida automáticamente y devuelve 400 antes del action
public class CuentaRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Titular { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "El saldo inicial no puede ser negativo")]
    public decimal SaldoInicial { get; set; }

    [Required]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "CUIT debe tener 11 dígitos")]
    public string Cuit { get; set; } = string.Empty;
}
```

❌ Parseo sin TryParse (lanza excepción ante input inválido):

```csharp
// ❌ si el query param no es un número, lanza FormatException
public IActionResult ObtenerPorId([FromQuery] string id)
{
    int idParsed = int.Parse(id); // ❌ explota con "abc"
    return Ok(_service.Obtener(idParsed));
}
```

✅ TryParse con manejo del caso inválido:

```csharp
// ✅ mejor aún: usar int directamente en el parámetro (el framework lo valida)
public IActionResult ObtenerPorId([FromQuery] int id) // ✅ el binder valida automáticamente
    => Ok(_service.Obtener(id));

// O si viene como string por alguna razón:
if (!int.TryParse(rawId, out int id))
    throw new ValidationException("El identificador debe ser un número entero válido.");
```

❌ Deserialización de upstream sin manejo de campos faltantes:

```csharp
// ❌ si el upstream agrega un campo nuevo o cambia un tipo, explota silenciosamente
var respuesta = JsonSerializer.Deserialize<CuentaUpstreamDto>(json);
var saldo = respuesta.Saldo; // ❌ NullReferenceException si el campo no vino
```

✅ Deserialización defensiva con opciones y null-check:

```csharp
// ✅ opciones explícitas; campos opcionales son nullable; null-check antes de usar
var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
CuentaUpstreamDto? respuesta = null;
try
{
    respuesta = JsonSerializer.Deserialize<CuentaUpstreamDto>(json, opciones);
}
catch (JsonException ex)
{
    _logger.LogError(ex, "Respuesta de upstream con formato inesperado");
    throw new NetworkException("El servicio upstream devolvió una respuesta inválida.");
}
var saldo = respuesta?.Saldo
    ?? throw new BusinessException("El campo 'saldo' no vino en la respuesta del upstream.");
```

❌ Entidad EF Core como request body:

```csharp
// ❌ expone la entidad al exterior; permite que el cliente setee campos internos
[HttpPost("clientes")]
public async Task<IActionResult> Crear([FromBody] ClienteEntity cliente, CancellationToken ct)
{
    await _dbContext.Clientes.AddAsync(cliente, ct);
    await _dbContext.SaveChangesAsync(ct);
    return Created();
}
```

✅ DTO de entrada separado:

```csharp
// ✅ DTO de entrada con solo los campos que el cliente puede enviar
[HttpPost("clientes")]
public async Task<IActionResult> Crear(
    [FromBody] CrearClienteRequest req, CancellationToken ct)
{
    var resultado = await _clienteService.CrearAsync(req, ct);
    return _responseBuilder.AddData(resultado).BuildResponse(StatusCodes.Status201Created);
}

public class CrearClienteRequest
{
    [Required] public string Nombre { get; set; } = string.Empty;
    [Required][EmailAddress] public string Email { get; set; } = string.Empty;
}
```

❌ Sin límite de tamaño del body:

```csharp
// ❌ sin límite; un cliente malicioso puede enviar un payload de GBs
app.MapControllers(); // sin configurar RequestSizeLimitAttribute ni KestrelOptions
```

✅ Límite de tamaño configurado:

```csharp
// ✅ en el endpoint específico con RequestSizeLimit
[HttpPost("importar")]
[RequestSizeLimit(5 * 1024 * 1024)] // 5 MB máximo para este endpoint
public async Task<IActionResult> Importar([FromBody] ImportRequest req, CancellationToken ct)
    => await _importService.ProcesarAsync(req, ct);

// O globalmente en Program.cs para Kestrel:
// builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
```

## Checklist antes de devolver código

- [ ] Todos los DTOs de entrada tienen DataAnnotations para campos obligatorios
      y con restricciones de formato/rango.
- [ ] Los DTOs de entrada son clases separadas (no entidades EF Core).
- [ ] Los campos opcionales de upstreams son tipos nullable con manejo explícito.
- [ ] Se usa `TryParse` (no `Parse`) para parsear strings de fuentes externas.
- [ ] La deserialización de upstreams tiene manejo de `JsonException`.
- [ ] Se lanza `ValidationException` EPA (no `ArgumentException` ni `InvalidOperationException`)
      para errores de validación de negocio en el service layer.
- [ ] Los mensajes de error de validación no exponen detalles internos.
- [ ] Hay un límite de tamaño configurado para endpoints que reciben payloads grandes.

## Conexiones con otros skills

- `aspnetcore-rest-layer` — `[ApiController]` activa la validación automática de
  DataAnnotations; `IExceptionHandler` maneja la `ValidationException` EPA.
- `aspnetcore-security-owasp-baseline` — validar en el borde es la primera línea
  de defensa contra inyección y payloads maliciosos.
- `aspnetcore-error-and-observability` — los errores de validación se loggean con
  correlation id; el formato EPA de respuesta es controlado por el arquetipo.
- `dotnet-parsing-and-validation` — consumir mensajes de mensajería también
  requiere validar el payload antes de procesarlo.

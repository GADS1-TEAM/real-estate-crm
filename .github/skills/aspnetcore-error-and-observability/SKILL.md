---
name: aspnetcore-error-and-observability
description: |
  Activa cuando se maneja errores, se escribe logs, se configuran métricas o trazas,
  o se define qué se devuelve al cliente ante un fallo en un microservicio ASP.NET
  Core 8 con el arquetipo epa-net-paas. Triggers: "ILogger", "ILogger<T>",
  "Console.WriteLine en producción", "log", "logging", "ISSUE_LOG",
  "APPLICATION_DEFAULT", "REQUEST", "RESPONSE", "OUTGOING_RESPONSE", "MASKED_DATA",
  "PII", "datos sensibles", "ofuscar", "enmascarar", "APP_ID en log", "APP_KEY log",
  "epa-net-paas logging", "excepción EPA", "ConfigurationException",
  "DependencyInjectionException", "DatabaseException", "IOException",
  "NetworkException", "BusinessException", "NotFoundException",
  "ValidationException", "excepciones tipadas EPA", "mapear excepción",
  "IExceptionHandler", "middleware de errores", "ProblemDetails",
  "meta-data-error", "IResponseBuilder error", "BaseErrorBuilder",
  "try/catch vacío", "tragar excepción", "catch vacío", "Jaeger",
  "JAEGER_COLLECTOR_HOST", "JAEGER_COLLECTOR_PORT", "OTLP", "gRPC tracing",
  "AddOtlpExporter", "tracing distribuido", "correlation id", "TraceId",
  "span", "Activity", "OpenTelemetry .NET", "métricas", "health check",
  "structured logging", "Serilog", "epa-net-paas observabilidad". Garantiza
  logging estructurado con tipos de log EPA, excepciones tipadas mapeadas a
  meta-data-error con IResponseBuilder, tracing Jaeger OTLP/gRPC, sin PII en
  logs, sin catch vacíos. MUST referenciar epa-net-paas. NO activar para
  lógica de negocio sin dimensión de error/observabilidad.
---

# ASP.NET Core Error Handling & Observability

## Objetivo

Cuando algo falla en producción a las 3am, la diferencia entre un incidente de 5
minutos y uno de 5 horas es la **observabilidad**: logs estructurados con correlation
id y tipos de log EPA, trazas distribuidas en Jaeger, y métricas de los caminos
críticos. Y la diferencia entre un fallo controlado y una caída en cascada es el
**manejo de errores**: excepciones tipadas EPA traducidas a un contrato uniforme
(`meta-data-error` con `IResponseBuilder`), sin tragar errores en silencio, sin
filtrar detalles internos al cliente. Este skill define ambas cosas para
microservicios ASP.NET Core 8 con el arquetipo `epa-net-paas`.

## Cuándo activar

- Se escribe o revisa código de logging.
- Se lanza o captura una excepción.
- Se define el contrato de error devuelto al cliente.
- Se configura tracing (Jaeger, OpenTelemetry), métricas o health checks.
- Se decide si un dato es logueable (PII, secreto, credencial).
- Se detecta un `catch` vacío o un `Console.WriteLine` en código de producción.

## Cuándo NO activar

- Lógica de negocio pura sin dimensión de error/observabilidad.
- Strings de UI o mensajes de presentación.

## Estado actual vs target

- **Target:** `ILogger<T>` con provider de logging estructurado (Serilog o
  `Microsoft.Extensions.Logging` con JSON formatter), tipos de log EPA
  correctamente usados (`ISSUE_LOG` para Warning/Error,
  `APPLICATION_DEFAULT` para el resto, `REQUEST`/`RESPONSE`/`OUTGOING_RESPONSE`
  para trazas de red). Env var `MASKED_DATA` para enmascarar PII configurada en
  el arquetipo. Excepciones tipadas EPA mapeadas en un `IExceptionHandler` global
  a respuestas `meta-data-error` con `IResponseBuilder`/`BaseErrorBuilder`.
  Tracing distribuido con Jaeger via OTLP/gRPC: env var `JAEGER_COLLECTOR_HOST`
  (incluir prefijo `http://`) y `JAEGER_COLLECTOR_PORT` (default `4317`).
  `ProblemDetails` como formato HTTP estándar de errores.
- **Arquetipo `epa-net-paas`:** los headers `APP_ID`/`APP_KEY` se auto-ofuscan
  en logs cuando pasan por el pipeline del arquetipo. El resto del PII es
  responsabilidad del desarrollador: el código no debe enviar el dato sensible
  al logger en primer lugar.

## Decisiones del proyecto

- **Tipos de log EPA bien usados:**
  - `ISSUE_LOG`: `LogWarning` y `LogError` — condiciones que deben alertar
    operacionalmente (fallas de upstream, errores de negocio graves, configuración
    incorrecta).
  - `APPLICATION_DEFAULT`: `LogInformation` y `LogDebug` — flujo normal de la
    aplicación.
  - `REQUEST` / `RESPONSE`: log del request HTTP entrante y la respuesta saliente.
  - `OUTGOING_RESPONSE`: log de la respuesta de calls HTTP salientes.
- **`MASKED_DATA`** (env var del arquetipo): enumera los campos cuyos valores
  deben enmascararse en logs. El desarrollador es responsable de no pasar datos
  sensibles al logger aunque no estén en `MASKED_DATA`.
- **Excepciones tipadas EPA** como contrato entre capas:
  - `ValidationException` → 400 Bad Request.
  - `NotFoundException` → 404 Not Found.
  - `BusinessException` → 422 Unprocessable Entity.
  - `NetworkException` → 500 Internal Server Error (falla de upstream HTTP).
  - `IOException` → 500 Internal Server Error (I/O genérico).
  - `DatabaseException` → 500 Internal Server Error.
  - `ConfigurationException` → 500 Internal Server Error (config incorrecta en startup).
  - `DependencyInjectionException` → 500 Internal Server Error.
- **Tracing con Jaeger via OTLP/gRPC:** configurar el exporter de OpenTelemetry
  con `JAEGER_COLLECTOR_HOST` (con prefijo `http://`) y `JAEGER_COLLECTOR_PORT`
  (default `4317`). Propagar el `TraceId` activo como campo en los logs para
  correlacionar logs con trazas en Jaeger.
- **Sin catch vacíos.** Un `catch (Exception) {}` en producción es un bug: oculta
  fallas, dificulta el debugging y hace imposible el alerting.

## Reglas obligatorias

### MUST

1. **MUST usar `ILogger<T>` para todo logging.** Nunca `Console.WriteLine`,
   `Debug.WriteLine`, ni logging directo a archivos. El logger se inyecta por DI.

2. **MUST usar los tipos de log EPA correctos:**
   - `ISSUE_LOG`: `LogWarning` y `LogError` (condiciones que deben alertar).
   - `APPLICATION_DEFAULT`: `LogInformation` y `LogDebug` (flujo normal).
     Configurar el tipo de log como campo estructurado (`LogType`) en el logger
     provider o vía la configuración del arquetipo.

3. **MUST NUNCA loggear:** números de cuenta completos, PAN/CVV, contraseñas,
   tokens de autenticación, datos biométricos, ni ningún dato personal sensible.
   Enmascarar o excluir explícitamente. Confiar en `MASKED_DATA` solo para los
   campos ya configurados; el código no debe enviar el dato en primer lugar si
   no está cubierto.

4. **MUST usar excepciones tipadas EPA** para comunicar errores entre capas:
   lanzar `DatabaseException` desde el repositorio, `NetworkException` desde el
   cliente HTTP saliente, `ValidationException` desde la validación de negocio,
   `NotFoundException` cuando no se encuentra un recurso.

5. **MUST centralizar el mapeo de excepciones EPA a respuestas HTTP** en un
   `IExceptionHandler` registrado en `Program.cs`. Usar `IResponseBuilder` /
   `BaseErrorBuilder` del arquetipo para construir la respuesta `meta-data-error`.
   No hay `try/catch` en los controllers para excepciones EPA.

6. **MUST configurar el tracing con Jaeger via OTLP/gRPC** usando:
   - `JAEGER_COLLECTOR_HOST`: host del collector (**incluir el prefijo `http://`**).
   - `JAEGER_COLLECTOR_PORT`: puerto (default `4317`).
     Usar el SDK de OpenTelemetry .NET con `AddOtlpExporter` y protocolo gRPC.

7. **MUST propagar el `TraceId` de la traza activa como campo en los logs** para
   poder correlacionar logs con trazas en Jaeger. Agregar
   `Activity.Current?.TraceId` al log scope o configurar el enriquecedor
   correspondiente en Serilog / `ILogger`.

8. **MUST NUNCA tragar una excepción en silencio.** Un `catch` que no relanza,
   no loggea, y no toma ninguna acción es un bug. Si se captura y se maneja,
   loggear o transformar la excepción; si no se puede manejar, relanzar.

### MUST NOT

9. **MUST NOT usar `Console.WriteLine`** en código de producción. No aparece en
   el log centralizado del arquetipo y no tiene contexto estructurado.

10. **MUST NOT loggear el stack trace completo para errores de negocio esperados**
    (404, 400, 422). Solo loggear el stack trace (nivel Error) para errores
    inesperados (5xx).

11. **MUST NOT exponer mensajes de excepción de EF Core, SQL Server, o librerías
    internas** al cliente. Traducir siempre a un mensaje seguro con
    `IResponseBuilder`/`BaseErrorBuilder`.

12. **MUST NOT devolver `ex.Message` crudo al cliente** en el `IExceptionHandler`.
    Puede contener detalles de la query SQL, nombres de tablas, stack traces o
    información de la infraestructura.

## Recomendaciones

### SHOULD

- Usar Serilog con sink de OpenTelemetry o con JSON formatter para integración
  directa con el stack de observabilidad del arquetipo y con agregadores como
  Elasticsearch o Splunk.
- Agregar el `TraceId` y `SpanId` activos al scope del logger (`ILogger.BeginScope`)
  para que aparezcan automáticamente en cada log dentro de una request.
- Instrumentar endpoints críticos y llamadas salientes con `Activity`
  (OpenTelemetry) para generar spans con nombre en Jaeger.
- Configurar un health check (`IHealthCheck`) para cada dependencia externa (DB,
  upstreams HTTP, broker de mensajería) y exponerlo en `/health`.

### SHOULD NOT

- No usar `LogError(ex.ToString())` para loggear excepciones; usar la sobrecarga
  `LogError(ex, "mensaje con {Campo}", valor)` que serializa el stack trace como
  campo estructurado separado.
- No loggear al nivel `LogWarning` eventos que son normales del negocio (ej.
  "cliente no encontrado" en un GET del usuario); usar `LogInformation` para esos
  casos; `LogWarning` es para condiciones que deben ser revisadas.

## Anti-patrones prohibidos

❌ `Console.WriteLine` en producción (no llega al log centralizado):

```csharp
public class SaldoService
{
    public async Task<SaldoDto> ObtenerAsync(int cuentaId, CancellationToken ct)
    {
        Console.WriteLine($"Obteniendo saldo de cuenta {cuentaId}"); // ❌ no va al log del arquetipo
        return await _repo.GetAsync(cuentaId, ct);
    }
}
```

✅ `ILogger<T>` con structured logging:

```csharp
public class SaldoService
{
    private readonly ILogger<SaldoService> _logger;

    public SaldoService(ILogger<SaldoService> logger) => _logger = logger;

    public async Task<SaldoDto> ObtenerAsync(int cuentaId, CancellationToken ct)
    {
        _logger.LogInformation("Obteniendo saldo. CuentaId: {CuentaId}", cuentaId); // ✅ structured log APPLICATION_DEFAULT
        return await _repo.GetAsync(cuentaId, ct);
    }
}
```

❌ Catch vacío que traga el error:

```csharp
try
{
    await _saldoService.ActualizarAsync(cuentaId, monto, ct);
}
catch (Exception)
{
    // ❌ excepción tragada silenciosamente: nadie sabe que falló; alerting imposible
}
```

✅ Log del error y relanzar con excepción EPA:

```csharp
try
{
    await _saldoService.ActualizarAsync(cuentaId, monto, ct);
}
catch (DatabaseException ex)
{
    _logger.LogError(ex, // ✅ ISSUE_LOG: LogError → stack trace como campo estructurado
        "Error de DB actualizando saldo. CuentaId: {CuentaId}", cuentaId);
    throw; // ✅ relanzar para que IExceptionHandler construya la respuesta meta-data-error
}
```

❌ Detalle interno de EF Core / SQL expuesto al cliente:

```csharp
public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
{
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new { error = ex.Message }, ct);
    // ❌ ex.Message puede ser "Invalid column name 'SaldoViejo'" → fuga de infraestructura
    return true;
}
```

✅ `IResponseBuilder` con excepción EPA opaca al cliente:

```csharp
public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
{
    if (ex is not DatabaseException dbEx) return false;

    _logger.LogError(dbEx, "Falla de base de datos no manejada."); // ✅ log interno con detalle completo
    var error = _errorBuilder
        .WithCode("DB_ERROR")
        .WithReason("Error interno de base de datos.")
        .WithErrorType(ErrorType.TECHNICAL)
        .Build();
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(
        _responseBuilder.AddError(error).BuildResponse(StatusCodes.Status500InternalServerError), ct);
    return true; // ✅ respuesta opaca con formato meta-data-error; sin detalles de EF Core
}
```

❌ PII en logs:

```csharp
_logger.LogInformation(
    "Procesando pago. Cliente: {Nombre}, CBU: {Cbu}, Monto: {Monto}",
    cliente.NombreCompleto, cliente.Cbu, pago.Monto);
// ❌ nombre completo y CBU son PII / datos financieros sensibles
```

✅ Log con identificadores no sensibles:

```csharp
_logger.LogInformation(
    "Procesando pago. ClienteId: {ClienteId}, PagoId: {PagoId}, Monto: {Monto}",
    cliente.Id, pago.Id, pago.Monto);
// ✅ IDs internos opacos; el monto puede ser aceptable según política del banco
```

❌ Tracing configurado sin exportador OTLP (las trazas nunca llegan a Jaeger):

```csharp
// Program.cs — falta el exporter
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddAspNetCoreInstrumentation());
// ❌ sin AddOtlpExporter las trazas se generan localmente y se descartan
```

✅ Tracing con Jaeger OTLP/gRPC usando env vars del arquetipo:

```csharp
// Program.cs
var jaegerHost = builder.Configuration["JAEGER_COLLECTOR_HOST"]; // ej: "http://jaeger-collector"
var jaegerPort = builder.Configuration["JAEGER_COLLECTOR_PORT"] ?? "4317";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation() // ✅ instrumenta llamadas salientes automáticamente
        .AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri($"{jaegerHost}:{jaegerPort}");
            otlp.Protocol = OtlpExportProtocol.Grpc; // ✅ OTLP/gRPC a Jaeger
        }));
// JAEGER_COLLECTOR_HOST debe incluir el prefijo http:// — ej: "http://jaeger-collector"
// JAEGER_COLLECTOR_PORT default: 4317
```

## Checklist antes de devolver código

- [ ] Todo logging usa `ILogger<T>`; no hay `Console.WriteLine` en producción.
- [ ] Se usan los tipos de log EPA correctos (`ISSUE_LOG` para Warning/Error, `APPLICATION_DEFAULT` para el resto).
- [ ] No hay PII, secretos, tokens, ni números de cuenta completos en los logs.
- [ ] Las excepciones tipadas EPA se lanzan desde la capa correcta (repo → `DatabaseException`, HTTP client → `NetworkException`, etc.).
- [ ] Hay un `IExceptionHandler` global que mapea excepciones EPA a respuestas `meta-data-error` con `IResponseBuilder`.
- [ ] El cliente nunca recibe un stack trace ni un mensaje de error de EF Core/SQL.
- [ ] El tracing de Jaeger está configurado con `JAEGER_COLLECTOR_HOST` (prefijo `http://`) y `JAEGER_COLLECTOR_PORT`.
- [ ] El `TraceId` activo se propaga como campo en los logs.
- [ ] No hay `catch` vacíos en el código.
- [ ] `APP_ID`/`APP_KEY` se auto-ofuscan vía arquetipo; el código no los loggea manualmente.

## Conexiones con otros skills

- `aspnetcore-rest-layer` — el `IExceptionHandler` es la pieza que une el manejo de errores EPA con el contrato de respuesta HTTP y `IResponseBuilder`.
- `aspnetcore-outgoing-http` — logging de fallas de upstream con tipo `ISSUE_LOG`; las llamadas salientes generan spans en Jaeger via `AddHttpClientInstrumentation`.
- `aspnetcore-database-access-efcore` — mapear `DbUpdateException` a `DatabaseException`; métricas de latencia de queries con OpenTelemetry.
- `aspnetcore-di-and-middleware-pipeline` — registro del `IExceptionHandler` en el pipeline; orden del middleware de errores respecto a otros middlewares.
- `aspnetcore-security-owasp-baseline` — no exponer detalles de implementación en respuestas de error ni en logs públicos.

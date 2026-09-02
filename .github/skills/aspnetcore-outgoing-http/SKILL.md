---
name: aspnetcore-outgoing-http
description: |
  Activa cuando el microservicio ASP.NET Core 10 hace llamadas HTTP salientes a otros
  microservicios, APIs internas o servicios externos. Triggers: "IHttpClientFactory", "HttpClient", "named client",
  "CreateClient", "llamar al servicio de", "consumir la API de", "request al
  microservicio", "timeout", "reintento", "retry", "circuit breaker", "Polly",
  "ResilienceHandler", "AddResilienceHandler", "Microsoft.Extensions.Http.Resilience",
  "AddStandardResilienceHandler", "AddPolicyHandler", "IAsyncPolicy<HttpResponseMessage>",
  "HttpRetryStrategyOptions", "HttpCircuitBreakerStrategyOptions", "backoff", "jitter",
  "connection refused", "socket exhaustion", "new HttpClient", "SERVICES:DATAS",
  "WITHAPIMCREDENTIALS", "APP_ID", "APP_KEY", "APP_ID_B2C", "APP_KEY_B2C",
  "credenciales APIM", "NetworkException",
  "DependencyException", "falla del upstream", "CancellationToken HTTP saliente",
  "HttpRequestException", "OperationCanceledException HTTP". Garantiza que TODA
  llamada saliente use IHttpClientFactory con named clients del arquetipo, tenga
  timeout acotado, resiliencia con Polly o Microsoft.Extensions.Http.Resilience,
  CancellationToken propagado, y fallas mapeadas a excepciones EPA tipadas. NO
  activar para: acceso a datos, mensajería, ni lógica in-process.
---

# ASP.NET Core Outgoing HTTP

## Objetivo

Un microservicio que llama a otros es tan resiliente como el más débil de sus
dependencias. La causa más frecuente de caídas en cascada es un cliente HTTP sin
timeout apuntando a un upstream lento: los threads de ASP.NET Core se acumulan
esperando, el pool se agota, y el microservicio cae también. Además, crear
instancias de `HttpClient` con `new` agota los sockets del sistema operativo
(socket exhaustion) porque los TIME_WAIT impiden reutilizar conexiones.

Este skill define cómo hacer llamadas HTTP salientes usando `IHttpClientFactory`
con **named clients configurados por el arquetipo `epa-net-paas`**, propagación
automática de credenciales APIM, resiliencia con Polly 8 o
`Microsoft.Extensions.Http.Resilience`, `CancellationToken` propagado en toda la
cadena, y mapeo de fallas a excepciones tipadas EPA.

## Cuándo activar

- Se hace cualquier request HTTP saliente a otro microservicio o API.
- Se configura o revisa timeouts, reintentos, circuit breaking, o fallbacks.
- Se usa `IHttpClientFactory`, `HttpClient`, o un SDK que lo envuelve.
- Se configura la sección `SERVICES:DATAS` para named clients del arquetipo EPA.
- Se gestionan credenciales APIM (`APP_ID`/`APP_KEY`, `WITHAPIMCREDENTIALS`).
- Se reporta lentitud, timeout, socket exhaustion, o caída en cascada.

## Cuándo NO activar

- Acceso a DB con EF Core (ver `aspnetcore-database-access-efcore`).
- Mensajería con Kafka o Service Bus (ver `aspnetcore-messaging`).
- Lógica in-process sin red.

## Estado actual vs target

- **Target:** ASP.NET Core 10, `IHttpClientFactory` con named clients registrados
  en `Program.cs`, configuración de upstreams vía env vars
  `SERVICES:DATAS:{i}:NAME`, `SERVICES:DATAS:{i}:URL`,
  `SERVICES:DATAS:{i}:WITHAPIMCREDENTIALS`. El arquetipo `epa-net-paas` registra
  y configura estos clientes automáticamente; el código solo consume con
  `_clientFactory.CreateClient("nombre")`. Resiliencia con Polly 8
  (`AddResilienceHandler`) o `Microsoft.Extensions.Http.Resilience`.
  `CancellationToken` propagado en toda la cadena: controller → service → client.
  Fallas de red y upstream mapeadas a `NetworkException` o `DependencyException` EPA.
- **Nunca `new HttpClient()`** en código de aplicación.

## Decisiones del proyecto

- **Named clients gestionados por `epa-net-paas`:** el arquetipo lee las env vars
  `SERVICES:DATAS:{i}:NAME/URL/WITHAPIMCREDENTIALS` y registra un named
  `HttpClient` por cada entrada. El código obtiene el cliente con
  `_clientFactory.CreateClient("nombre")`. No hace falta configurar URLs ni
  credenciales en el código: vienen del entorno.
- **Credenciales APIM auto-propagadas:** cuando `WITHAPIMCREDENTIALS=true`, el
  arquetipo inyecta los headers `APP_ID`/`APP_KEY` (o `APP_ID_B2C`/`APP_KEY_B2C`
  para flujos B2C) en cada request saliente. No hace falta agregarlos manualmente.
  Los valores se **auto-ofuscan** en los logs del arquetipo.
- **Timeouts explícitos siempre.** Timeout de conexión y de respuesta configurados
  en el registro del cliente; no depender de los defaults de .NET (100 segundos
  por defecto en `HttpClient`, que es excesivo).
- **Resiliencia con Polly o `Microsoft.Extensions.Http.Resilience`:** retry con
  jitter para operaciones idempotentes, circuit breaker en upstreams críticos,
  timeout policy.
- **Reintentos solo para operaciones idempotentes** (GET, PUT idempotente, DELETE).
  Nunca reintentar POST sin idempotency key verificado en el upstream.
- **`CancellationToken` obligatorio** en toda llamada async HTTP. Propagarlo desde
  el controller hasta el método que llama a `HttpClient.SendAsync`.

## Reglas obligatorias

### MUST

1. **MUST obtener `HttpClient` siempre con `IHttpClientFactory.CreateClient("nombre")`.**
   Nunca `new HttpClient()`. La factory gestiona el ciclo de vida del
   `HttpMessageHandler` y reutiliza conexiones correctamente.

2. **MUST usar named clients definidos en las env vars `SERVICES:DATAS:{i}`** del
   arquetipo `epa-net-paas`. Nombre del cliente = `SERVICES:DATAS:{i}:NAME`. URL
   base = `SERVICES:DATAS:{i}:URL`. Propagación de credenciales APIM habilitada
   con `SERVICES:DATAS:{i}:WITHAPIMCREDENTIALS=true`.

3. **MUST configurar timeouts explícitos** en el registro del named client
   (`HttpClient.Timeout` o via `AddResilienceHandler` con policy de timeout). No
   depender del timeout por defecto.

4. **MUST aplicar resiliencia** en upstreams críticos: retry con backoff exponencial
   - jitter para operaciones idempotentes, circuit breaker para cortar el flujo ante
     fallas repetidas. Usar Polly 8 (`AddResilienceHandler`) o
     `AddStandardResilienceHandler()` de `Microsoft.Extensions.Http.Resilience`.

5. **MUST propagar `CancellationToken`** en toda llamada HTTP saliente. El token
   viene del request de ASP.NET Core y debe llegar hasta `HttpClient.SendAsync`.

6. **MUST mapear errores de red y upstream a excepciones EPA:**
   - `NetworkException` para errores de conectividad (timeout, `HttpRequestException`).
   - `DependencyException` (o `IOException` EPA si aplica) para fallas del upstream (5xx).
   - No relanzar `HttpRequestException` cruda al caller.

7. **MUST manejar diferencialmente 4xx vs 5xx del upstream:** los 4xx son errores
   del cliente y no deben reintentarse; los 5xx son candidatos a retry si la
   operación es idempotente.

### MUST NOT

8. **MUST NOT crear `HttpClient` con `new HttpClient()`** en ningún contexto de
   aplicación. Produce socket exhaustion bajo carga sostenida.

9. **MUST NOT reintentar POST no idempotente** sin un idempotency key comprobado
   en el upstream. Un retry de una transferencia puede ejecutarla dos veces.

10. **MUST NOT hardcodear URLs, `APP_ID`, `APP_KEY` ni credenciales** en el código.
    Deben venir de las env vars del arquetipo.

11. **MUST NOT ignorar el status code de la respuesta** y deserializar el body
    incondicionalmente. Verificar el status antes de parsear.

## Recomendaciones

### SHOULD

- Usar `AddStandardResilienceHandler()` de `Microsoft.Extensions.Http.Resilience`
  (.NET 10+) como baseline de resiliencia; ofrece retry, circuit breaker y timeout
  configurados con valores razonables por defecto, reduciendo el boilerplate de Polly.
- Usar `IOptions<T>` para exponer la configuración de named clients en un tipo
  fuertemente tipado si se necesita acceso programático (timeouts custom, configuración
  extra no cubierta por las env vars del arquetipo).
- Instrumentar las llamadas salientes con `Activity` (OpenTelemetry) para generar
  spans en Jaeger y correlacionar con el tracing distribuido del arquetipo.
- Registrar en `ISSUE_LOG` (Warning/Error EPA) toda falla de upstream con el nombre
  del servicio, el status code y el tiempo de respuesta.

### SHOULD NOT

- No crear un `ITypedHttpClient` que acople fuertemente un servicio específico cuando
  `IHttpClientFactory` con named client ya provee la flexibilidad necesaria.
- No silenciar `OperationCanceledException`; propagarla al caller (es una cancelación
  legítima del request o un timeout).

## Anti-patrones prohibidos

❌ `new HttpClient()` sin factory (socket exhaustion):

```csharp
public class SaldoService
{
    public async Task<SaldoDto> ObtenerAsync(string cuentaId, CancellationToken ct)
    {
        using var client = new HttpClient(); // ❌ cada llamada crea y destruye sockets; agota el OS
        var resp = await client.GetAsync($"https://saldos-api/saldos/{cuentaId}", ct);
        return await resp.Content.ReadFromJsonAsync<SaldoDto>(ct);
    }
}
```

✅ Named client del arquetipo EPA con `IHttpClientFactory`:

```csharp
public class SaldoService
{
    private readonly IHttpClientFactory _clientFactory;

    public SaldoService(IHttpClientFactory clientFactory) =>
        _clientFactory = clientFactory;

    public async Task<SaldoDto> ObtenerAsync(string cuentaId, CancellationToken ct)
    {
        // ✅ el nombre debe coincidir con SERVICES:DATAS:{i}:NAME en las env vars
        var client = _clientFactory.CreateClient("saldos-service");
        try
        {
            var resp = await client.GetAsync($"saldos/{cuentaId}", ct);
            if (!resp.IsSuccessStatusCode)
                throw new NetworkException($"Upstream devolvió {(int)resp.StatusCode}.");
            return await resp.Content.ReadFromJsonAsync<SaldoDto>(ct)
                ?? throw new NetworkException("Respuesta vacía del servicio de saldos.");
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkException("Falla de conectividad con servicio de saldos.", ex); // ✅ excepción EPA
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new NetworkException("Timeout al llamar al servicio de saldos.", ex); // ✅
        }
    }
}
```

❌ Retry sobre POST no idempotente:

```csharp
// En Program.cs
builder.Services.AddHttpClient("transferencias-service")
    .AddResilienceHandler("retry", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions { MaxRetryAttempts = 3 });
        // ❌ si el endpoint /transferencias es POST sin idempotency key, se puede ejecutar 3 veces
    });
```

✅ Retry + circuit breaker solo en clientes para operaciones idempotentes:

```csharp
// En Program.cs — named client para servicio de consulta (GET idempotente)
builder.Services.AddHttpClient("consultas-service")
    .AddResilienceHandler("consultas-resilience", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(300),
            UseJitter = true,
            ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Exception is HttpRequestException ||
                args.Outcome.Result?.StatusCode >= HttpStatusCode.InternalServerError) // ✅ solo 5xx
        });
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(15)
        });
        pipeline.AddTimeout(TimeSpan.FromSeconds(5)); // ✅ timeout explícito
    });
```

❌ Credenciales APIM hardcodeadas en el código:

```csharp
client.DefaultRequestHeaders.Add("APP_ID", "mi-app-id-hardcodeado"); // ❌ secreto en código
client.DefaultRequestHeaders.Add("APP_KEY", "mi-app-key-hardcodeado");
```

✅ Credenciales APIM auto-propagadas por el arquetipo `epa-net-paas`:

```csharp
// En env vars:
//   SERVICES:DATAS:0:NAME=saldos-service
//   SERVICES:DATAS:0:URL=https://apim.banco.com/saldos
//   SERVICES:DATAS:0:WITHAPIMCREDENTIALS=true
// El arquetipo epa-net-paas inyecta APP_ID/APP_KEY automáticamente en cada request
// y los ofusca en los logs. No hay nada que agregar en el código. ✅
var client = _clientFactory.CreateClient("saldos-service");
```

## Checklist antes de devolver código

- [ ] Todo `HttpClient` se obtiene con `IHttpClientFactory.CreateClient("nombre")`.
- [ ] No hay `new HttpClient()` en ningún lugar del código de aplicación.
- [ ] El nombre del client coincide con `SERVICES:DATAS:{i}:NAME` de las env vars.
- [ ] URLs y credenciales APIM vienen de las env vars, no del código.
- [ ] Hay timeout explícito configurado en el named client.
- [ ] La política de retry solo aplica a operaciones idempotentes.
- [ ] Los errores de red se mapean a `NetworkException` EPA (no se relanza `HttpRequestException` cruda).
- [ ] `CancellationToken` se propaga hasta `HttpClient.SendAsync`.
- [ ] Los status 4xx no se reintentan.

## Conexiones con otros skills

- `dotnet-async-and-concurrency` — fan-out a múltiples upstreams con concurrencia acotada.
- `dotnet-parsing-and-validation` — deserialización y validación defensiva de la respuesta del upstream.
- `aspnetcore-error-and-observability` — logging de fallas de upstream con tipo `ISSUE_LOG`; configuración de Jaeger para spans de llamadas salientes.
- `aspnetcore-di-and-middleware-pipeline` — registro del named client en `Program.cs`; ciclo de vida del `HttpMessageHandler`.
- `aspnetcore-security-owasp-baseline` — prevenir SSRF: no construir URLs con datos del usuario.

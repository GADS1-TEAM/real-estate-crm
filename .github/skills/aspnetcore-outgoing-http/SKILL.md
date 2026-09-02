---
name: aspnetcore-outgoing-http
description: |
  Activa cuando un servicio o BFF de este CRM llama por HTTP a otro servicio interno
  o a un proveedor externo. Triggers: "IHttpClientFactory", "HttpClient",
  "typed client", "named client", "AddHttpClient", "CreateClient",
  "llamar al servicio de", "consumir la API de", "request al microservicio",
  "timeout", "reintento", "retry", "circuit breaker", "Polly",
  "AddStandardResilienceHandler", "Microsoft.Extensions.Http.Resilience",
  "backoff", "jitter", "socket exhaustion", "new HttpClient",
  "HttpRequestException", "connection refused", "falla del upstream",
  "portal inmobiliario", "adapter externo", "propagar el token",
  "DependencyUnavailableException", "CancellationToken HTTP saliente".
  Garantiza que toda llamada saliente use IHttpClientFactory con timeout acotado,
  resiliencia, CancellationToken propagado, contexto de actor explícito y fallas
  traducidas a excepciones de dominio. NO activar para: acceso a datos, mensajería
  ni lógica in-process.
---

# HTTP Saliente

## Objetivo

En este CRM hay dos clases de llamada saliente, y confundirlas es caro:

- **Entre servicios propios.** `matching-service` necesita un dato de
  `property-service`. Acá la regla dura no es de HTTP sino de arquitectura: si el
  dato puede llegar por evento, **no se llama**. Y cuando se llama, el actor viaja
  explícito y el otro extremo revalida.
- **A proveedores externos.** Portales, WhatsApp, Google Calendar, Outlook. Son
  lentos, se caen y cambian sin avisar. Todo eso vive detrás de un **adapter**, no
  esparcido por la aplicación.

En ambos casos, un `HttpClient` mal usado agota sockets y una llamada sin timeout
cuelga el servicio entero.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §2 y §6.

## Cuándo activar

- Se hace una llamada HTTP a otro servicio o a un proveedor externo.
- Se registra o configura un `HttpClient`.
- Se define timeout, reintentos o circuit breaker.
- Se escribe un adapter de integración.
- Se diagnostica una falla de upstream o de conectividad.

## Cuándo NO activar

- Acceso a datos (ver las skills de MongoDB).
- Mensajería y eventos (ver `rabbitmq-dotnet`).
- Lógica in-process sin salida de red.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Cliente | **`IHttpClientFactory`** con **typed clients**. Nunca `new HttpClient()` |
| Configuración | URLs y credenciales por opciones tipadas, nunca hardcodeadas |
| Resiliencia | `Microsoft.Extensions.Http.Resilience` con `AddStandardResilienceHandler()` |
| Timeout | **Siempre acotado y explícito**, por cliente |
| Actor | El `ActorContext` viaja en la llamada; el otro extremo revalida |
| Externos | Detrás de un **adapter** que implementa un puerto del dominio |
| Fallas | Traducidas a `DependencyUnavailableException` u otra excepción tipada |
| Cancelación | `CancellationToken` propagado siempre |

> **Antes de escribir el cliente, la pregunta de arquitectura:** ¿este dato puede
> llegar por evento? Si sí, no se llama sincrónicamente. Ver
> `ddd-hexagonal-architecture` y `cqrs-read-models-projections`.

## Estado actual vs target

- **Estado:** no hay clientes. `FND-003` define los contratos; `FND-007` la
  resiliencia compartida.
- **Target:** un typed client por dependencia, registrado con timeout y resiliencia
  desde configuración; los adapters externos detrás de puertos.

## Reglas obligatorias

### MUST

- **MUST** usar `IHttpClientFactory` con typed clients registrados en el composition
  root.
- **MUST** fijar un **timeout explícito** por cliente.
- **MUST** propagar `CancellationToken` a toda llamada.
- **MUST** aplicar resiliencia: reintentos con backoff y jitter, y circuit breaker,
  solo sobre operaciones **idempotentes**.
- **MUST** propagar el `ActorContext` y el `correlationId` en las llamadas entre
  servicios propios.
- **MUST** traducir las fallas a excepciones tipadas: nunca dejar escapar
  `HttpRequestException` cruda hacia el dominio.
- **MUST** poner los proveedores externos detrás de un puerto del dominio, con su
  adapter en `Infrastructure`.
- **MUST** tomar URLs, claves y timeouts de configuración.

### MUST NOT

- **MUST NOT** hacer `new HttpClient()`: agota sockets y no respeta cambios de DNS.
- **MUST NOT** llamar sin timeout.
- **MUST NOT** reintentar una operación no idempotente: duplica efectos de negocio.
- **MUST NOT** llamar sincrónicamente a otro servicio dentro de un command handler
  si el dato puede llegar por evento.
- **MUST NOT** consultar la base de otro servicio "porque es más rápido que la API".
- **MUST NOT** hardcodear URLs, tokens ni claves.
- **MUST NOT** loguear el cuerpo de una respuesta que puede contener PII.
- **MUST NOT** dejar que la falla de un proveedor externo tumbe un caso de uso que
  podría seguir sin él.

## Recomendaciones

### SHOULD

- **SHOULD** usar typed clients en vez de named: el compilador ayuda y la
  dependencia queda explícita.
- **SHOULD** dar timeouts distintos según la dependencia: un servicio propio no
  tolera lo mismo que un portal externo.
- **SHOULD** tratar el circuit breaker abierto como estado esperado y degradar la
  funcionalidad, no romperla.
- **SHOULD** medir latencia y tasa de error por dependencia: es la señal temprana de
  que un proveedor se degrada.
- **SHOULD** testear el adapter contra un servidor HTTP de prueba, incluyendo
  timeout, 500 y respuesta malformada.
- **SHOULD** versionar el contrato con el proveedor externo dentro del adapter, para
  que un cambio suyo no se propague al dominio.

### SHOULD NOT

- **SHOULD NOT** reintentar un 4xx: el problema es el request, no el momento.
- **SHOULD NOT** encadenar llamadas sincrónicas entre varios servicios: cada eslabón
  suma latencia y probabilidad de falla.

## Anti-patrones prohibidos

### 1. `new HttpClient()`

```csharp
// ❌ Agota sockets bajo carga y no ve cambios de DNS.
public async Task<PropertyDto> GetAsync(Guid id, CancellationToken ct)
{
    using var client = new HttpClient();
    return await client.GetFromJsonAsync<PropertyDto>($"{_url}/properties/{id}", ct);
}
```

```csharp
// ✅ Typed client registrado una vez, con timeout y resiliencia.
builder.Services.AddHttpClient<IPropertyClient, PropertyClient>((sp, c) =>
{
    var opt = sp.GetRequiredService<IOptions<ServicesOptions>>().Value;
    c.BaseAddress = new Uri(opt.PropertyService.BaseUrl);
    c.Timeout = opt.PropertyService.Timeout;
})
.AddStandardResilienceHandler();
```

### 2. Llamada sincrónica evitable

```csharp
// ❌ Acopla en tiempo real: si property-service está caído, no se puede
//    registrar un Requirement.
public async Task HandleAsync(CreateRequirement cmd, CancellationToken ct)
{
    var property = await _propertyClient.GetAsync(cmd.PropertyId, ct);
    // ...
}
```

```csharp
// ✅ El dato ya está en un read model alimentado por eventos.
var snapshot = await _snapshots.GetAsync(cmd.PropertyId, ct);
```

### 3. Reintento sobre operación no idempotente

```csharp
// ❌ Tres reintentos = tres reservas creadas.
.AddStandardResilienceHandler();     // aplicado a un POST que crea
```

```csharp
// ✅ Idempotencia explícita, y recién entonces reintentos seguros.
request.Headers.Add("Idempotency-Key", operationId.ToString());
```

### 4. Falla cruda del upstream llegando al dominio

```csharp
// ❌ El dominio termina conociendo HTTP.
var response = await _client.GetAsync($"/listings/{id}", ct);
response.EnsureSuccessStatusCode();
```

```csharp
// ✅ Traducida a excepción tipada del proyecto.
try
{
    var response = await _client.GetAsync($"/listings/{id}", ct);

    if (response.StatusCode == HttpStatusCode.NotFound)
        return null;

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<ListingDto>(ct);
}
catch (HttpRequestException ex)
{
    throw new DependencyUnavailableException("supply-service", ex);
}
catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
{
    throw new DependencyUnavailableException("supply-service", ex);
}
```

### 5. Llamada entre servicios sin contexto de actor

```csharp
// ❌ El otro extremo no sabe en nombre de quién responde.
var listing = await _client.GetFromJsonAsync<ListingDto>($"/listings/{id}", ct);
```

```csharp
// ✅ Actor y correlación explícitos; el otro extremo revalida.
var listing = await _supplyClient.GetListingAsync(id, _actor.Current, ct);
```

### 6. Credenciales y URLs en el código

```csharp
// ❌ Secreto en el repositorio y URL que cambia por entorno.
client.BaseAddress = new Uri("https://portal.example.com/api");
client.DefaultRequestHeaders.Add("X-Api-Key", "ak_live_9f2c…");
```

```csharp
// ✅ Todo desde configuración tipada y validada.
c.BaseAddress = new Uri(opt.Portal.BaseUrl);
c.DefaultRequestHeaders.Add("X-Api-Key", opt.Portal.ApiKey);
```

## Checklist antes de devolver código

- [ ] Ningún `new HttpClient()`.
- [ ] Typed clients registrados con `BaseAddress` y timeout desde configuración.
- [ ] Resiliencia aplicada solo a operaciones idempotentes.
- [ ] `CancellationToken` propagado.
- [ ] `ActorContext` y `correlationId` en llamadas entre servicios propios.
- [ ] Fallas traducidas a excepciones tipadas.
- [ ] Proveedores externos detrás de un puerto con su adapter.
- [ ] Ninguna URL, clave o token hardcodeado.
- [ ] Ningún cuerpo con PII logueado.
- [ ] Se evaluó si el dato podía llegar por evento antes de llamar.
- [ ] Hay tests de timeout, 500 y respuesta malformada.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | Los externos van detrás de puertos; el dominio no conoce HTTP. |
| `event-driven-outbox-inbox` | Si el dato puede llegar por evento, no se llama sincrónicamente. |
| `multitenancy-authorization` | Propagación del `ActorContext`; el receptor revalida. |
| `aspnetcore-config-and-secrets` | URLs, claves y timeouts son configuración validada. |
| `aspnetcore-error-and-observability` | Traducción de fallas, métricas y trazas por dependencia. |
| `dotnet-async-and-concurrency` | Cancelación, timeouts y llamadas en paralelo. |
| `dotnet-adversarial-testing` | Upstream lento, caído o con respuesta malformada. |

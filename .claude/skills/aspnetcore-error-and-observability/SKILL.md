---
name: aspnetcore-error-and-observability
description: |
  Activa cuando se maneja un error, se escribe un log, se configuran métricas o
  trazas, o se define qué se devuelve al cliente ante un fallo en un servicio o BFF
  de este CRM. Triggers: "ILogger", "ILogger<T>", "log", "logging", "structured
  logging", "LogInformation", "LogWarning", "LogError", "PII", "datos sensibles",
  "ofuscar", "enmascarar", "IExceptionHandler", "ProblemDetails", "middleware de
  errores", "mapear excepción", "try/catch vacío", "tragar excepción", "catch
  vacío", "OpenTelemetry", "tracing distribuido", "traza", "span", "Activity",
  "correlationId", "AddCrmObservability", "UseCrmCorrelationId",
  "SanitizingLoggerProvider", "métricas", "health check", "readiness",
  "liveness", "AddCrmHealthChecks".
  Garantiza logging estructurado sin PII, errores mapeados a ProblemDetailsV1 en un
  mecanismo centralizado, trazas con OpenTelemetry, correlación de punta a punta y
  ningún error tragado en silencio. NO activar para: lógica de negocio sin
  dimensión de error ni evaluación de permisos.
---

# Errores y Observabilidad

## Objetivo

Cuando algo falla, la pregunta no es *"¿hubo un error?"* sino *"¿qué operación de
negocio se rompió y qué cadena de llamadas la produjo?"*.

Eso exige errores que sobreviven el viaje entre capas, logs estructurados que se
pueden filtrar, y trazas correlacionadas que atan una acción del usuario con todo
lo que disparó.

Y una prohibición: **nada de esto puede filtrar datos de las personas**. Un CRM
inmobiliario maneja documentos de identidad, teléfonos, domicilios e importes.

Fuentes: `PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md` (§0, §3),
`IMPLEMENTATION_REPORT-V2-FND-002.md`, `IMPLEMENTATION_REPORT-V2-FND-003.md`.

## Cuándo activar

- Se maneja o se lanza una excepción.
- Se escribe un log.
- Se configura tracing o health checks.
- Se define qué recibe el cliente ante un fallo.

## Cuándo NO activar

- Lógica de negocio sin dimensión de error.
- Forma de los endpoints (ver `aspnetcore-rest-layer`).
- Evaluación de permisos (ver `aspnetcore-security-owasp-baseline`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Contrato de error | `ProblemDetailsV1` (`contracts`) con `ErrorCode` estable |
| Mapeo | Mecanismo centralizado por servicio (ej. `IExceptionHandler`), no repetido por endpoint |
| Logging | `AddCrmObservability(serviceName)` registra `SanitizingLoggerProvider`: todo log pasa por redacción de `password`/`token`/`secret`/`documento`/`dni`/`cuit` antes de escribirse |
| Tracing | `AddCrmObservability` configura OpenTelemetry con instrumentación de ASP.NET Core y exporter de consola (la POC no requiere collector externo) |
| Correlación | `UseCrmCorrelationId()` (middleware `CorrelationIdMiddleware`) toma o genera un `correlationId` por request, lo agrega como tag de `Activity.Current` y como scope de `ILogger`, y lo devuelve en la respuesta |
| Health | `AddCrmHealthChecks()` + `MapCrmHealthEndpoints()`: `/health/live` no evalúa dependencias, `/health/ready` evalúa las que el servicio necesita (tag `ready`) |
| PII | **Nunca** en logs, trazas ni respuestas |

### Contexto obligatorio en todo log de negocio

```text
correlationId   hilo de negocio completo (propagado por CorrelationIdMiddleware)
actorId         usuario que provocó la acción (de ExecutionContextV1)
service         nombre del servicio (serviceName pasado a AddCrmObservability)
```

Sin `correlationId`, un log de este sistema es difícil de reconstruir: no se puede
atar a la cadena BFF → servicio → evento publicado.

## Estado actual vs target

- **Estado:** `AddCrmHealthChecks`/`MapCrmHealthEndpoints`/`AddCrmObservability`/
  `UseCrmCorrelationId`/`SensitiveDataRedactor` existen en
  `BuildingBlocks.Infrastructure` (V2-FND-003), probados incluso contra Mongo/
  RabbitMQ reales, pero **sin wire-up** en ningún `Program.cs` de `services/`/`bffs/`.
- **Target:** `V2-ACL-001` hace el primer wire-up real y agrega el test end-to-end de
  `correlationId` BFF → access-service → evento publicado (follow-up explícito de
  `V2-FND-003`).

## Reglas obligatorias

### MUST

- **MUST** centralizar el mapeo error → `ProblemDetailsV1` en un único mecanismo
  por servicio.
- **MUST** incluir `correlationId` y `actorId` en el contexto de todo log de
  negocio.
- **MUST** propagar el `correlationId` a través de HTTP, eventos (`EventEnvelopeV1`)
  y trazas, sin cambiarlo.
- **MUST** loguear con parámetros estructurados, no concatenando strings.
- **MUST** loguear en `Error` toda excepción no esperada, con su stack, **del lado
  del servidor**.
- **MUST** registrar `AddCrmObservability(serviceName)` y `UseCrmCorrelationId()`
  en todo `Program.cs` de servicio y BFF que agregue un endpoint HTTP real.
- **MUST** exponer `/health/live` y `/health/ready` vía `AddCrmHealthChecks()`, y
  que readiness verifique solo las dependencias que el servicio realmente necesita.
- **MUST** usar `SanitizingLoggerProvider` (vía `AddCrmObservability`) en vez de un
  logger sin redacción.

### MUST NOT

- **MUST NOT** tragar excepciones: un `catch` vacío o que solo loguea en `Debug`
  está prohibido.
- **MUST NOT** loguear PII: nombre completo, documento, CUIT, teléfono, email,
  domicilio, importes de una operación concreta.
- **MUST NOT** loguear secretos, tokens, connection strings ni headers de
  autorización.
- **MUST NOT** devolver al cliente stack traces, nombres de índice, mensajes del
  driver ni rutas internas.
- **MUST NOT** usar `Console.WriteLine` para logging: pasa por fuera de la
  redacción de `SanitizingLoggerProvider`.
- **MUST NOT** hacer que un health check ejecute lógica de negocio o consultas
  costosas.
- **MUST NOT** perder el `correlationId` al reaccionar a un evento: se propaga
  como `CorrelationId` del nuevo `EventEnvelopeV1`, con `CausationId` apuntando al
  evento que lo causó.

## Recomendaciones

### SHOULD

- **SHOULD** loguear el `errorCode` junto con la excepción: permite contar fallos
  por tipo sin parsear mensajes.
- **SHOULD** loguear IDs y no contenidos: `partyId` sí, nombre y documento no.
- **SHOULD** usar `LogWarning` para lo que puede resolverse solo y `LogError` para
  lo que requiere intervención.
- **SHOULD** incluir el `correlationId` en la respuesta de error para que el
  usuario pueda reportarlo.

### SHOULD NOT

- **SHOULD NOT** loguear en `Information` dentro de un bucle por elemento.
- **SHOULD NOT** crear una métrica por cada cosa medible sin que alguien la mire.

## Anti-patrones prohibidos

### 1. Excepción tragada

```csharp
// ❌ El error desaparece. Nadie se entera hasta que un dato falta.
try
{
    await _publisher.PublishAsync(message, ct);
}
catch (Exception)
{
    // ignorado
}
```

```csharp
// ✅ Se loguea con contexto y se decide qué hacer.
try
{
    await _publisher.PublishAsync(message, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Fallo al publicar {EventName} {EventId}",
        message.Name, message.EventId);
    throw;
}
```

### 2. PII en el log

```csharp
// ❌ El nombre, el DNI y el teléfono quedan en el sistema de logs para siempre.
_logger.LogInformation("Party creada: {@Party}", party);
```

```csharp
// ✅ Identificadores y metadatos, no contenidos.
_logger.LogInformation(
    "Party creada {PartyId} corr={CorrelationId}", party.Id, correlationId);
```

### 3. Detalle interno devuelto al cliente

```csharp
// ❌ Revela infraestructura y da pistas a un atacante.
return StatusCode(500, ex.ToString());
```

```csharp
// ✅ El cliente recibe un ProblemDetailsV1 opaco; el detalle queda en el servidor.
_logger.LogError(ex, "Error no manejado corr={CorrelationId}", correlationId);
```

### 4. Log sin contexto

```csharp
// ❌ Con varios servicios y consumidores, esto no sirve para nada.
_logger.LogError("Error al guardar");
```

```csharp
// ✅ Filtrable y reconstruible.
_logger.LogError(ex,
    "Error al guardar {Aggregate} {AggregateId} corr={CorrelationId}",
    nameof(Party), party.Id, correlationId);
```

### 5. Health check que hace trabajo real

```csharp
// ❌ El orquestador reinicia el servicio porque una query pesada tardó.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true   // incluye un check que cuenta documentos
});
```

```csharp
// ✅ Readiness verifica que las dependencias respondan, nada más.
builder.Services.AddCrmHealthChecks()
    .AddMongoReadinessCheck()
    .AddRabbitMqReadinessCheck();

app.MapCrmHealthEndpoints();
```

### 6. Correlación perdida al reaccionar a un evento

```csharp
// ❌ La cadena se corta: el evento derivado nace sin hilo.
await _publisher.PublishAsync(new PartyRegistered(partyId, occurredAt), ct);
```

```csharp
// ✅ correlationId se propaga; causationId apunta al evento que lo causó.
var envelope = new EventEnvelopeV1<PartyRegisteredPayload>(
    EventId: Guid.NewGuid(),
    Name: "PartyRegistered",
    Version: 1,
    OccurredAt: DateTimeOffset.UtcNow,
    ActorId: actorId,
    CorrelationId: incoming.CorrelationId,
    CausationId: incoming.EventId,
    AggregateId: partyId,
    Payload: payload);
```

## Checklist antes de devolver código

- [ ] Ningún `catch` vacío ni que solo silencie.
- [ ] Mapeo de errores centralizado a `ProblemDetailsV1`.
- [ ] Todo log de negocio lleva `correlationId` y `actorId`.
- [ ] Logging estructurado, sin concatenación.
- [ ] Ninguna PII, secreto ni token en logs o trazas.
- [ ] Ningún detalle interno en la respuesta al cliente.
- [ ] `AddCrmObservability`/`UseCrmCorrelationId`/`AddCrmHealthChecks` registrados
      en el `Program.cs` del servicio/BFF.
- [ ] `/health/live` y `/health/ready` expuestos, readiness sin trabajo pesado.
- [ ] `correlationId` propagado a eventos derivados, con `causationId`.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `aspnetcore-rest-layer` | Define el contrato de error; esta skill, cómo se produce y se registra. |
| `ddd-hexagonal-architecture` | Las excepciones de dominio nacen en el aggregate y viajan tipadas. |
| `event-driven-outbox-inbox` | Propagación de `correlationId`/`causationId` en eventos. |
| `mongodb-dotnet-driver` | Errores del driver traducidos a errores de dominio. |
| `aspnetcore-security-owasp-baseline` | Logging de eventos de seguridad sin PII. |

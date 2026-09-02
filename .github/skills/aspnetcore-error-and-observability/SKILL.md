---
name: aspnetcore-error-and-observability
description: |
  Activa cuando se maneja un error, se escribe un log, se configuran métricas o
  trazas, o se define qué se devuelve al cliente ante un fallo en un servicio de
  este CRM. Triggers: "ILogger", "ILogger<T>", "log", "logging", "structured
  logging", "Serilog", "Console.WriteLine en producción", "LogInformation",
  "LogWarning", "LogError", "PII", "datos sensibles", "ofuscar", "enmascarar",
  "excepción tipada", "ValidationException", "NotFoundException",
  "ConflictException", "BusinessRuleViolationException",
  "DependencyUnavailableException", "IExceptionHandler", "AddProblemDetails",
  "ProblemDetails", "middleware de errores", "mapear excepción", "try/catch vacío",
  "tragar excepción", "catch vacío", "OpenTelemetry", "OTLP", "tracing distribuido",
  "traza", "span", "Activity", "correlationId", "TraceId", "métricas", "RED",
  "health check", "readiness", "liveness", "alerta".
  Garantiza logging estructurado sin PII, excepciones tipadas mapeadas a Problem
  Details en un handler global, trazas con OpenTelemetry, correlación de punta a
  punta y ningún error tragado en silencio. NO activar para: lógica de negocio sin
  dimensión de error ni decisiones de autorización.
---

# Errores y Observabilidad

## Objetivo

Cuando algo falla en un sistema de veinte servicios, la pregunta no es *"¿hubo un
error?"* sino *"¿qué operación de negocio se rompió, para qué tenant, y qué cadena
de llamadas la produjo?"*.

Eso exige tres cosas que se diseñan juntas: **errores tipados** que sobreviven el
viaje entre capas, **logs estructurados** que se pueden filtrar, y **trazas
correlacionadas** que atan una acción del usuario con todo lo que disparó.

Y una prohibición: **nada de esto puede filtrar datos de las personas**. Un CRM
inmobiliario maneja documentos de identidad, teléfonos, domicilios e importes.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §8 y §19,
[`ADR-006`](../../../docs/adr/006-contrato-de-error-publico.md).

## Cuándo activar

- Se maneja o se lanza una excepción.
- Se escribe un log.
- Se configura tracing, métricas o health checks.
- Se define qué recibe el cliente ante un fallo.
- Se diagnostica un incidente.

## Cuándo NO activar

- Lógica de negocio sin dimensión de error.
- Forma de los endpoints (ver `aspnetcore-rest-layer`).
- Decisión de si el actor puede hacer algo (ver `multitenancy-authorization`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Contrato de error | `problem+json` RFC 9457 con `code` estable. Ver [ADR-006](../../../docs/adr/006-contrato-de-error-publico.md) |
| Mapeo | Un `IExceptionHandler` global registrado con `AddProblemDetails()` |
| Excepciones | Tipadas en `building-blocks`, con `code` propio |
| Logging | Estructurado en JSON, vía `Microsoft.Extensions.Logging` |
| Tracing | **OpenTelemetry**. Los drivers de MongoDB y RabbitMQ ya exponen instrumentación |
| Correlación | `correlationId` en logs, trazas, eventos y respuestas de error |
| Métricas | RED en endpoints y consumidores; retraso de relay y de projectors |
| Health | `/health/live` y `/health/ready` en todo servicio |
| PII | **Nunca** en logs, trazas ni respuestas |

### Contexto obligatorio en todo log

```text
correlationId   hilo de negocio completo
tenantId        tenant afectado
actor           usuario o sistema que provocó la acción
service         servicio que emite
```

Sin `tenantId` y `correlationId`, un log de este sistema es inútil: no se puede
filtrar ni reconstruir qué pasó.

## Estado actual vs target

- **Estado:** no implementado. `FND-003` crea las excepciones tipadas; `FND-007`
  observabilidad y resiliencia.
- **Target:** un building block compartido registra handler, logging y OpenTelemetry;
  ningún servicio los configura por su cuenta.

## Reglas obligatorias

### MUST

- **MUST** usar excepciones **tipadas** para comunicar errores entre capas, cada una
  con su `code` estable.
- **MUST** centralizar el mapeo excepción → respuesta en un `IExceptionHandler`
  global.
- **MUST** incluir `correlationId`, `tenantId` y `actor` en el contexto de todo log
  de negocio.
- **MUST** propagar el `correlationId` a través de HTTP, eventos y trazas, sin
  cambiarlo.
- **MUST** loguear con parámetros estructurados, no concatenando strings.
- **MUST** loguear en `Error` toda excepción no esperada, con su stack, **del lado
  del servidor**.
- **MUST** exponer `/health/live` y `/health/ready`, y que readiness verifique las
  dependencias que el servicio realmente necesita.
- **MUST** instrumentar con OpenTelemetry y usar la instrumentación nativa de los
  drivers en vez de escribir spans a mano.
- **MUST** emitir métricas de retraso del relay de outbox y de los projectors: un
  outbox que crece es la señal temprana de que el broker está caído.

### MUST NOT

- **MUST NOT** tragar excepciones: un `catch` vacío o que solo loguea en `Debug`
  está prohibido.
- **MUST NOT** loguear PII: nombre completo, documento, CUIT, teléfono, email,
  domicilio, importes de una operación concreta.
- **MUST NOT** loguear secretos, tokens, connection strings ni headers de
  autorización.
- **MUST NOT** devolver al cliente stack traces, nombres de índice, mensajes del
  driver ni rutas internas.
- **MUST NOT** usar `Console.WriteLine` para logging.
- **MUST NOT** hacer que un health check ejecute lógica de negocio o consultas
  costosas.
- **MUST NOT** perder el `correlationId` al reaccionar a un evento: se propaga.

## Recomendaciones

### SHOULD

- **SHOULD** loguear el `code` del error junto con la excepción: permite contar
  fallos por tipo sin parsear mensajes.
- **SHOULD** loguear IDs y no contenidos: `partyId` sí, nombre y documento no.
- **SHOULD** usar `LogWarning` para lo que puede resolverse solo (un reintento que
  funcionó) y `LogError` para lo que requiere intervención.
- **SHOULD** alertar sobre tasa de DLQ, retraso del relay y proyecciones atrasadas,
  no solo sobre errores HTTP.
- **SHOULD** incluir el `traceId` en la respuesta de error para que el usuario pueda
  reportarlo.
- **SHOULD** medir los caminos de negocio críticos: creación de Party, matching,
  cierre de operación.

### SHOULD NOT

- **SHOULD NOT** loguear en `Information` dentro de un bucle por elemento.
- **SHOULD NOT** crear una métrica por cada cosa medible: cada una tiene costo y
  alguien tiene que mirarla.

## Anti-patrones prohibidos

### 1. Excepción tragada

```csharp
// ❌ El error desaparece. Nadie se entera hasta que un dato falta.
try
{
    await _publisher.PublishAsync(envelope, ct);
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
    await _publisher.PublishAsync(envelope, ct);
}
catch (Exception ex)
{
    _logger.LogError(ex,
        "Fallo al publicar {EventName} {EventId} del tenant {TenantId}",
        envelope.Name, envelope.EventId, envelope.TenantId);
    throw;   // el outbox lo reintenta
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
    "Party creada {PartyId} kind={Kind} tenant={TenantId} corr={CorrelationId}",
    party.Id, party.Kind, party.TenantId, correlationId);
```

### 3. Detalle interno devuelto al cliente

```csharp
// ❌ Revela infraestructura y da pistas a un atacante.
return StatusCode(500, ex.ToString());
```

```csharp
// ✅ El cliente recibe un código y una traza; el detalle queda en el servidor.
// { "status": 500, "code": "INTERNAL_ERROR", "correlationId": "0f9c…" }
_logger.LogError(ex, "Error no manejado corr={CorrelationId}", ctx.TraceIdentifier);
```

### 4. Log sin contexto

```csharp
// ❌ Con veinte servicios y varios tenants, esto no sirve para nada.
_logger.LogError("Error al guardar");
```

```csharp
// ✅ Filtrable y reconstruible.
_logger.LogError(ex,
    "Error al guardar {Aggregate} {AggregateId} tenant={TenantId} corr={CorrelationId}",
    nameof(Party), party.Id, party.TenantId, correlationId);
```

### 5. Concatenación en vez de parámetros estructurados

```csharp
// ❌ Se pierde la estructura: no se puede filtrar por partyId.
_logger.LogInformation("Procesando party " + partyId + " del tenant " + tenantId);
```

```csharp
// ✅ Campos consultables en el sistema de logs.
_logger.LogInformation("Procesando {PartyId} tenant={TenantId}", partyId, tenantId);
```

### 6. Health check que hace trabajo real

```csharp
// ❌ El orquestador reinicia el servicio porque una query pesada tardó.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true   // incluye un check que cuenta documentos
});
```

```csharp
// ✅ Readiness verifica que las dependencias respondan, nada más.
builder.Services.AddHealthChecks()
    .AddMongoDb(tags: ["ready"])
    .AddRabbitMQ(tags: ["ready"]);
```

### 7. Correlación perdida al reaccionar a un evento

```csharp
// ❌ La cadena se corta: el evento derivado nace sin hilo.
await _publisher.PublishAsync(new MatchGenerated(matchId), ct);
```

```csharp
// ✅ correlationId se propaga; causationId apunta al evento que lo causó.
await _publisher.PublishAsync(new MatchGenerated(matchId)
{
    CorrelationId = incoming.CorrelationId,
    CausationId = incoming.EventId
}, ct);
```

## Checklist antes de devolver código

- [ ] Ningún `catch` vacío ni que solo silencie.
- [ ] Excepciones tipadas con `code` estable.
- [ ] Mapeo de errores centralizado en el handler global.
- [ ] Todo log de negocio lleva `correlationId`, `tenantId` y `actor`.
- [ ] Logging estructurado, sin concatenación.
- [ ] Ninguna PII, secreto ni token en logs o trazas.
- [ ] Ningún detalle interno en la respuesta al cliente.
- [ ] `/health/live` y `/health/ready` expuestos, readiness sin trabajo pesado.
- [ ] OpenTelemetry configurado, usando la instrumentación de los drivers.
- [ ] `correlationId` propagado a eventos derivados, con `causationId`.
- [ ] Métricas de relay y projectors donde correspondan.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `aspnetcore-rest-layer` | Define el contrato de error; esta skill, cómo se produce y se registra. |
| `ddd-hexagonal-architecture` | Las excepciones de dominio nacen en el aggregate y viajan tipadas. |
| `multitenancy-authorization` | `tenantId` y `actor` en el contexto de log; recurso ajeno da 404. |
| `event-driven-outbox-inbox` | Propagación de `correlationId`/`causationId` y métricas del relay. |
| `mongodb-dotnet-driver` | Tracing nativo del driver; errores traducidos a excepciones de dominio. |
| `rabbitmq-dotnet` | Métricas de queue y DLQ. |
| `aspnetcore-config-and-secrets` | Endpoints de telemetría y niveles de log son configuración. |

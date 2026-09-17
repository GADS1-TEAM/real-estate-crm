---
name: event-driven-outbox-inbox
description: |
  Activa cuando un servicio de este CRM publica o consume eventos de integración:
  outbox transaccional sobre MongoDB, relay de publicación, inbox de deduplicación,
  idempotencia, correlación, DLQ y versionado de contratos de evento. Triggers:
  "outbox", "inbox", "publicar un evento", "publicar evento de integración",
  "integration event", "evento público", "EventEnvelopeV1", "eventId",
  "correlationId", "causationId", "occurredAt", "aggregateId", "idempotencia",
  "idempotente", "deduplicación", "mensaje duplicado", "at-least-once",
  "entrega al menos una vez", "IOutbox", "IInbox", "IEventPublisher",
  "IEventConsumer", "RabbitMqEventPublisher", "RabbitMqEventConsumerHost", "DLQ",
  "dead letter", "evento perdido", "se guardó pero no se publicó", "consumidor",
  "consumer", "handler de evento", "suscribirse a un evento", "versionado de
  eventos", "cambio incompatible de evento", "payload del evento", "PII en el
  evento".
  Garantiza que ningún cambio de negocio se publique sin haberse persistido, que
  ningún consumidor duplique efectos, que los eventos sean hechos ocurridos con
  payload mínimo en `EventEnvelopeV1<TPayload>`, y que los contratos evolucionen
  sin romper consumidores. NO activar para: la API de RabbitMQ en detalle (canales,
  prefetch), la forma de los documentos de negocio, reglas de dominio ni capa REST.
---

# Event-Driven: Outbox e Inbox

## Objetivo

Publicar un evento y guardar un cambio son dos escrituras a dos sistemas
distintos: MongoDB y RabbitMQ. Sin coordinación aparecen dos fallas caras:

- **Cambio sin evento.** Un servicio guarda el aggregate y se cae antes de
  publicar. El dato existe pero nadie más se enteró.
- **Evento sin cambio.** Se publica y después falla el guardado. Se anunció algo
  que no ocurrió, y ya no hay forma de retirarlo.

El **outbox** guarda el evento en la misma base y la misma transacción que el
cambio de negocio (`IUnitOfWork.ExecuteInTransactionAsync`). El **relay/consumer
host** lo publica o lo entrega después. El **inbox** es el espejo: cada consumidor
registra qué `eventId` ya procesó, para que una entrega repetida no produzca
efectos dos veces.

Este building block ya está implementado en `RealEstateCrm.BuildingBlocks.Infra
structure` (`V2-FND-002`/`V2-FND-003`): esta skill documenta cómo usarlo, no cómo
reinventarlo.

Fuentes: `IMPLEMENTATION_REPORT-V2-FND-002.md`,
`IMPLEMENTATION_REPORT-V2-FND-003.md`.

## Cuándo activar

- Un aggregate produce un evento que otro servicio necesita.
- Se implementa un consumidor de eventos (`IEventConsumer<TPayload>`).
- Cambia el contrato de un evento ya publicado.
- Hay que decidir qué va en el payload.
- Aparece un duplicado, un evento perdido o un mensaje en la DLQ.

## Cuándo NO activar

- Configuración de RabbitMQ fuera de lo que ya resuelve `RabbitMqEventConsumerHost`/
  `RabbitMqEventPublisher` (exchange, cola, DLQ, prefetch de bajo nivel).
- Forma de los documentos de negocio (ver `mongodb-document-modeling`).
- Qué evento corresponde emitir según el dominio (ver `ddd-hexagonal-architecture`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Broker | RabbitMQ (`rabbitmq:4.3.6-management` en Compose), `RabbitMQ.Client` 7.2.2 |
| Outbox | Colección `outbox_messages` en la base del servicio (D8), escrita junto al aggregate en una transacción (`IUnitOfWork.ExecuteInTransactionAsync` + `MongoUnitOfWork`, requiere Mongo con replica set) |
| Inbox | Índice único `(EventId, ConsumerName)` en `InboxConsumedMessage` — deduplica por consumidor, no globalmente |
| Publicación | `IOutbox.EnqueueAsync` encola dentro de la transacción; el proceso que drena llama `IEventPublisher.PublishAsync` y después `IOutbox.MarkPublishedAsync` |
| Consumo | `RabbitMqEventConsumerHost.StartAsync<TPayload>` declara exchange/cola/DLQ (topic, `x-dead-letter-exchange`), resuelve `IInbox.TryMarkConsumedAsync` **antes** de invocar `IEventConsumer<TPayload>.HandleAsync`, y hace ack/nack según el resultado |
| Topología | Un exchange topic por servicio productor (`RabbitMqOptions.ServiceExchangeName`), routing key = nombre del evento, una cola + una DLQ (`<queue>.dlq` por defecto) por consumidor (`RabbitMqConsumerRegistration`) |
| Reintentos | Sin backoff: un fallo en `HandleAsync` hace `nack(requeue:false)` y el mensaje va directo a la DLQ (limitación conocida, ver Follow-ups de `V2-FND-002`) |
| Envelope | `EventEnvelopeV1<TPayload>` (`contracts`); no lleva ningún identificador de organización |
| Entrega | At-least-once. Todo `IEventConsumer<TPayload>` debe ser idempotente por su cuenta si su efecto no es naturalmente idempotente |

### Envelope obligatorio (`EventEnvelopeV1<TPayload>`)

```text
eventId         identificador único del evento (dedupe del Inbox)
name            nombre del hecho, en PascalCase y pasado (ej. "PartyRegistered")
version         versión del contrato, empieza en 1
occurredAt      cuándo ocurrió el hecho, UTC
actorId         quién lo provocó
correlationId   hilo de negocio completo
causationId     evento o comando que lo causó directamente (nullable)
aggregateId     aggregate que lo emitió
payload         datos mínimos del hecho
```

`correlationId` se propaga sin cambios a lo largo de toda la cadena. `causationId`
apunta siempre al eslabón inmediatamente anterior.

## Estado actual vs target

- **Estado:** `IOutbox`/`IInbox`/`IEventPublisher`/`IEventConsumer<TPayload>`
  (puertos) y `MongoOutbox`/`MongoInbox`/`RabbitMqEventPublisher`/
  `RabbitMqEventConsumerHost` (adapters) existen y están probados contra Mongo/
  RabbitMQ reales (`V2-FND-002`). Ningún servicio de dominio los usa todavía: no
  hay aggregates reales.
- **Target:** cada servicio que publica encola en su outbox dentro de la misma
  transacción que el aggregate; cada servicio que consume registra su
  `RabbitMqConsumerRegistration` e implementa `IEventConsumer<TPayload>`.

## Reglas obligatorias

### MUST

- **MUST** escribir el aggregate y el `OutboxMessage` en la **misma transacción**
  (`IUnitOfWork.ExecuteInTransactionAsync`). Nunca publicar desde el command
  handler.
- **MUST** publicar únicamente desde el proceso que drena el outbox, nunca desde
  lógica de negocio.
- **MUST** marcar `PublishedAt` (`IOutbox.MarkPublishedAsync`) **después** de que
  `IEventPublisher.PublishAsync` confirmó la publicación, nunca antes.
- **MUST** hacer todo `IEventConsumer<TPayload>` idempotente si su efecto no es
  naturalmente idempotente: el host ya evita invocar `HandleAsync` dos veces para
  el mismo `(eventId, consumerName)`, pero eso no cubre lo que pase si el proceso
  se cae **dentro** de `HandleAsync`.
- **MUST** declarar un `ConsumerName` estable en cada `IEventConsumer<TPayload>`:
  es la clave de idempotencia en el Inbox junto con `eventId`.
- **MUST** modelar los eventos como **hechos ocurridos**, nombrados en pasado:
  `PartyRegistered`, `CatalogEntryDeactivated`, `RoleAssigned`.
- **MUST** incluir el envelope completo (`EventEnvelopeV1<TPayload>`) en todo
  evento público.
- **MUST** propagar `correlationId` y `causationId` en toda la cadena, incluidos
  los eventos que un consumidor genera al reaccionar.
- **MUST** publicar una **versión nueva** ante cualquier cambio incompatible:
  quitar un campo, renombrarlo, cambiar su tipo o su significado.
- **MUST** dejar que el mensaje que falla en `HandleAsync` vaya a su DLQ
  (`RabbitMqConsumerRegistration.ResolvedDeadLetterQueueName`), conservando el
  envelope.

### MUST NOT

- **MUST NOT** publicar directamente desde un command handler, un controller o un
  repositorio.
- **MUST NOT** publicar comandos disfrazados de evento. `SendWelcomeEmail` no es un
  evento; `PartyRegistered` sí. El emisor no decide qué hace el receptor.
- **MUST NOT** meter el aggregate completo en el payload: solo lo mínimo para que
  el consumidor reaccione o necesite consultar el resto por API.
- **MUST NOT** incluir PII innecesaria en el payload: documentos de identidad,
  teléfonos, emails, domicilios que el consumidor no necesita.
- **MUST NOT** cambiar de forma incompatible un evento ya publicado, ni reutilizar
  un nombre de evento con otro significado.
- **MUST NOT** asumir orden global entre aggregates ni entre servicios.
- **MUST NOT** consumir el propio evento para modificar el mismo aggregate que lo
  emitió: es un ciclo.
- **MUST NOT** borrar del outbox en el momento de publicar: se marca
  `PublishedAt`.

## Recomendaciones

### SHOULD

- **SHOULD** incluir en el payload los IDs que el consumidor necesita para
  consultar el resto por API, en vez de replicar datos.
- **SHOULD** hacer los cambios de contrato **aditivos** siempre que se pueda: un
  campo opcional nuevo no rompe a nadie y no sube versión.
- **SHOULD** exponer métricas del proceso que drena el outbox: pendientes,
  antigüedad del más viejo. Un outbox que crece es la señal temprana de que el
  broker está caído.

### SHOULD NOT

- **SHOULD NOT** poner lógica de negocio en el proceso que drena el outbox: solo
  lee, publica y marca.
- **SHOULD NOT** emitir un evento por cada cambio menor de un campo. El evento
  público representa un hecho de negocio, no un `UPDATE`.

## Anti-patrones prohibidos

### 1. Publicar desde el handler

```csharp
// ❌ Si el proceso se cae entre las dos líneas, se pierde el evento
//    o se anuncia algo que no se guardó.
await _repository.UpdateAsync(party, ct);
await _publisher.PublishAsync(envelope, ct);
```

```csharp
// ✅ El handler solo persiste; el aggregate y su outbox viajan juntos.
await _unitOfWork.ExecuteInTransactionAsync(async ct2 =>
{
    await _repository.UpdateAsync(party, ct2);
    await _outbox.EnqueueAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow), ct2);
}, ct);
```

### 2. Marcar publicado antes de publicar

```csharp
// ❌ Si el broker rechaza o el proceso muere, el evento queda marcado
//    como publicado y no se reintenta jamás.
await _outbox.MarkPublishedAsync(message.EventId, DateTimeOffset.UtcNow, ct);
await _eventPublisher.PublishAsync(message, ct);
```

```csharp
// ✅ Primero el broker confirma, después se marca.
await _eventPublisher.PublishAsync(message, ct);
await _outbox.MarkPublishedAsync(message.EventId, DateTimeOffset.UtcNow, ct);
```

### 3. `ConsumerName` inestable

```csharp
// ❌ Cambia entre despliegues: el Inbox pierde el historial de qué ya se procesó
//    y todo se vuelve a entregar como "primera vez".
public string ConsumerName => GetType().FullName!;
```

```csharp
// ✅ Nombre explícito y estable, documentado.
public string ConsumerName => "party-service.commercial-origin-projector";
```

### 4. Aggregate completo y PII en el payload

```csharp
// ❌ Replica el aggregate, filtra PII y obliga a versionar ante cualquier cambio.
new UserCreatedPayload(user); // nombre, email, teléfono, roles completos...
```

```csharp
// ✅ Lo mínimo para reaccionar. El resto se consulta por API si hace falta.
new UserCreatedPayload(UserId: user.Id, Status: user.Status);
```

### 5. Cambio incompatible en el lugar

```csharp
// ❌ Renombrar un campo rompe en silencio a todos los consumidores desplegados.
public sealed record CatalogEntryCreatedPayload(string Code, string Label);
// pasa a ser:
public sealed record CatalogEntryCreatedPayload(string Code, string DisplayLabel);
```

```csharp
// ✅ Versión nueva (Version: 2 en el envelope), y las dos conviven hasta que
//    todos migren.
```

### 6. Evento que en realidad es una orden

```csharp
// ❌ El emisor decide qué tiene que hacer el receptor.
new NotifyUserOfRoleChange(userId, roleId);
```

```csharp
// ✅ Hecho ocurrido. El consumidor decide si reacciona y cómo.
new RoleAssignedPayload(UserId: userId, RoleId: roleId, OccurredAt: occurredAt);
```

## Checklist antes de devolver código

- [ ] Ningún `PublishAsync` fuera del proceso que drena el outbox.
- [ ] Aggregate y outbox se escriben en la misma transacción
      (`IUnitOfWork.ExecuteInTransactionAsync`).
- [ ] `MarkPublishedAsync` se llama después de la confirmación del broker.
- [ ] Todo `IEventConsumer<TPayload>` declara un `ConsumerName` estable.
- [ ] Los eventos tienen el envelope completo (`EventEnvelopeV1<TPayload>`) y
      nombre en pasado.
- [ ] `correlationId` y `causationId` se propagan.
- [ ] El payload es mínimo y no lleva PII innecesaria.
- [ ] Ningún cambio incompatible sobre un evento ya publicado sin versión nueva.
- [ ] `RabbitMqConsumerRegistration` declara exchange, routing key, cola y DLQ.
- [ ] Nada del diseño depende del orden entre aggregates distintos.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | Decide **qué** es evento de dominio y qué de integración. Esta skill decide cómo se publica. |
| `mongodb-document-modeling` | El outbox es una colección más: nombre e índices siguen sus reglas. |
| `mongodb-dotnet-driver` | `IUnitOfWork`/sesiones y manejo del duplicate key en C#. |
| `aspnetcore-error-and-observability` | Trazas del relay, `correlationId`/`causationId` en logs, sin PII en los logs de evento. |

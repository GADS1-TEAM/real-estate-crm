---
name: event-driven-outbox-inbox
description: |
  Activa cuando un servicio de este CRM publica o consume eventos de integración:
  outbox transaccional sobre MongoDB, relay de publicación, inbox de deduplicación,
  idempotencia, correlación, reintentos, DLQ y versionado de contratos de evento.
  Triggers: "outbox", "inbox", "publicar un evento", "publicar evento de
  integración", "integration event", "evento público", "event envelope", "eventId",
  "correlationId", "causationId", "occurredAt", "aggregateId", "idempotencia",
  "idempotente", "deduplicación", "messageId", "mensaje duplicado", "at-least-once",
  "exactly-once", "entrega al menos una vez", "relay", "publisher",
  "message relay", "DLQ", "dead letter", "reintento", "retry", "poison message",
  "evento perdido", "se guardó pero no se publicó", "consumidor", "consumer",
  "handler de evento", "suscribirse a un evento", "versionado de eventos",
  "cambio incompatible de evento", "payload del evento", "PII en el evento",
  "replay de eventos", "reconstruir proyección", "orden de los eventos".
  Garantiza que ningún cambio de negocio se publique sin haberse persistido, que
  ningún consumidor duplique efectos, que los eventos sean hechos ocurridos con
  payload mínimo y que los contratos evolucionen sin romper consumidores.
  NO activar para: la API de RabbitMQ (exchanges, canales, prefetch), la forma de
  los documentos de negocio, reglas de dominio ni capa REST.
---

# Event-Driven: Outbox e Inbox

## Objetivo

Publicar un evento y guardar un cambio son dos escrituras a dos sistemas
distintos: MongoDB y RabbitMQ. Sin coordinación aparecen dos fallas que en este
dominio son caras:

- **Cambio sin evento.** `party-service` guarda la Party y se cae antes de
  publicar. `identity-resolution-service` nunca busca duplicados y
  `analytics-copilot-service` nunca actualiza su proyección. El dato existe pero el
  sistema no se enteró.
- **Evento sin cambio.** Se publica y después falla el guardado. Se anunció algo
  que no ocurrió, y ya no hay forma de retirarlo.

El **outbox** guarda el evento en la misma base y la misma transacción que el
cambio de negocio. Un **relay** lo publica después, reintentando hasta lograrlo.
El **inbox** es el espejo: cada consumidor registra qué mensajes ya procesó, para
que una entrega repetida no produzca efectos dos veces.

Fuentes: [`ARCHITECTURE.md`](../../../ARCHITECTURE.md) §8,
[`AGENTS.md`](../../../AGENTS.md).

## Cuándo activar

- Un aggregate produce un evento que otro servicio necesita.
- Se implementa un consumidor de eventos.
- Se escribe o modifica el relay de publicación.
- Cambia el contrato de un evento ya publicado.
- Hay que decidir qué va en el payload.
- Aparece un duplicado, un evento perdido o un mensaje en DLQ.

## Cuándo NO activar

- Configuración de RabbitMQ: exchanges, bindings, prefetch, canales, ack (ver
  `rabbitmq-dotnet` y `aspnetcore-messaging`).
- Forma de los documentos de negocio (ver `mongodb-document-modeling`).
- Qué evento corresponde emitir según el dominio (ver `ddd-hexagonal-architecture`).
- Read models y proyecciones (ver `cqrs-read-models-projections`).

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Broker | RabbitMQ Community |
| Outbox | Colección propia por servicio: `<servicio>_outbox` |
| Atomicidad | Aggregate + outbox en **una transacción**. Es la única excepción sancionada a la regla de una escritura por operación |
| Infra | MongoDB como **replica set de un nodo**: las transacciones no funcionan en standalone |
| Relay | `BackgroundService` por servicio, publica en orden de `occurredAt` |
| Entrega | **At-least-once.** Todo consumidor debe ser idempotente |
| Inbox | Colección propia: `<servicio>_inbox`, índice único por `(tenantId, messageId)` |
| Retención | No se borra al publicar: se marca `publishedAt` y un índice **TTL de 7 días** limpia. Igual para el inbox |
| Versionado | Cambios compatibles no suben versión; los incompatibles publican una versión nueva |
| Schema registry | Ninguno. Contratos versionados como código, validados en CI |

### Envelope obligatorio

Todo evento público declara estos campos:

```text
eventId         identificador único del evento (dedupe del consumidor)
name            nombre del hecho, en pasado
version         versión del contrato
occurredAt      cuándo ocurrió el hecho, UTC
tenantId        tenant dueño del dato
actor           quién lo provocó (usuario, sistema, automatización)
correlationId   hilo de negocio completo
causationId     evento o comando que lo causó directamente
aggregateId     aggregate que lo emitió
payload         datos mínimos del hecho
```

`correlationId` se propaga sin cambios a lo largo de toda la cadena.
`causationId` apunta siempre al eslabón inmediatamente anterior.

## Estado actual vs target

- **Estado:** no hay eventos implementados. `FND-003` define el envelope como
  contrato compartido; `FND-004` levanta RabbitMQ y el replica set.
- **Target:** cada servicio que publica tiene su outbox, su relay y sus índices;
  cada servicio que consume tiene su inbox. El building block es compartido; los
  contratos de evento, versionados en `contracts`.

## Reglas obligatorias

### MUST

- **MUST** escribir el aggregate y sus eventos de integración en la **misma
  transacción**. Nunca publicar desde el command handler.
- **MUST** publicar únicamente desde el relay, leyendo del outbox.
- **MUST** marcar `publishedAt` **después** de que el broker confirmó la
  publicación, nunca antes.
- **MUST** hacer todo consumidor idempotente: la entrega es al menos una vez.
- **MUST** registrar el `messageId` procesado en el inbox **dentro de la misma
  transacción** que el cambio de negocio que provocó.
- **MUST** tratar el duplicate key del inbox como "ya procesado": se hace ack y se
  sigue, no es un error.
- **MUST** modelar los eventos como **hechos ocurridos**, nombrados en pasado:
  `PartyRegistered`, `ListingActivated`, `ReservationConfirmed`.
- **MUST** incluir el envelope completo en todo evento público.
- **MUST** propagar `correlationId` y `causationId` en toda la cadena, incluidos los
  eventos que un consumidor genera al reaccionar.
- **MUST** publicar una **versión nueva** ante cualquier cambio incompatible:
  quitar un campo, renombrarlo, cambiar su tipo o su significado.
- **MUST** enviar a DLQ el mensaje que agotó sus reintentos, conservando el
  envelope y el motivo.
- **MUST** derivar `tenantId` del envelope del evento, nunca del payload.

### MUST NOT

- **MUST NOT** publicar directamente desde un command handler, un controller o un
  repositorio.
- **MUST NOT** publicar comandos disfrazados de evento. `SendWelcomeEmail` no es un
  evento; `PartyRegistered` sí. El emisor no decide qué hace el receptor.
- **MUST NOT** meter el aggregate completo en el payload: solo lo mínimo para que el
  consumidor reaccione o sepa qué consultar.
- **MUST NOT** incluir PII innecesaria: documentos de identidad, teléfonos, emails,
  domicilios o importes que el consumidor no necesita.
- **MUST NOT** cambiar de forma incompatible un evento ya publicado, ni reutilizar
  un nombre de evento con otro significado.
- **MUST NOT** asumir orden global entre aggregates ni entre servicios.
- **MUST NOT** consumir el propio evento para modificar el mismo aggregate que lo
  emitió: es un ciclo.
- **MUST NOT** usar el outbox como cola de trabajo interna ni como scheduler.
- **MUST NOT** borrar del outbox en el momento de publicar: se marca y el TTL limpia.

## Recomendaciones

### SHOULD

- **SHOULD** implementar outbox, relay e inbox **una sola vez** en un building block
  compartido. Veinte servicios no deben tener veinte relays distintos.
- **SHOULD** ordenar la publicación por `occurredAt` dentro de cada aggregate;
  entre aggregates distintos no hay garantía y el diseño no debe depender de ella.
- **SHOULD** incluir en el payload los IDs que el consumidor necesita para consultar
  el resto por API, en vez de replicar datos.
- **SHOULD** hacer los cambios de contrato **aditivos** siempre que se pueda: un
  campo opcional nuevo no rompe a nadie y no sube versión.
- **SHOULD** publicar las dos versiones en paralelo durante la transición cuando un
  cambio incompatible es inevitable, hasta que todos los consumidores migren.
- **SHOULD** exponer métricas del relay: pendientes, antigüedad del más viejo,
  fallos de publicación. Un outbox que crece es la señal temprana de que el broker
  está caído.
- **SHOULD** usar change streams sobre el outbox para que el relay reaccione en vez
  de hacer polling, una vez que el replica set esté disponible.

### SHOULD NOT

- **SHOULD NOT** poner lógica de negocio en el relay: solo lee, publica y marca.
- **SHOULD NOT** reintentar indefinidamente: hay un tope y después DLQ.
- **SHOULD NOT** emitir un evento por cada cambio menor de un campo. El evento
  público representa un hecho de negocio, no un `UPDATE`.

## Anti-patrones prohibidos

### 1. Publicar desde el handler

```csharp
// ❌ Si el proceso se cae entre las dos líneas, se pierde el evento
//    o se anuncia algo que no se guardó.
await _repository.SaveAsync(party, ct);
await _publisher.PublishAsync(new PartyRegistered(party.Id), ct);
```

```csharp
// ✅ El handler solo persiste; el aggregate y su outbox viajan juntos.
await _repository.SaveWithOutboxAsync(party, party.DequeueEvents(), ct);
```

### 2. Escritura del outbox sin transacción

```csharp
// ❌ Dos escrituras independientes: la ventana de pérdida sigue existiendo.
await _parties.ReplaceOneAsync(filter, document, cancellationToken: ct);
await _outbox.InsertManyAsync(envelopes, cancellationToken: ct);
```

```csharp
// ✅ Excepción sancionada: aggregate + outbox en una transacción.
using var session = await _client.StartSessionAsync(cancellationToken: ct);

await session.WithTransactionAsync(async (s, t) =>
{
    var result = await _parties.ReplaceOneAsync(s, filter, document, cancellationToken: t);
    if (result.ModifiedCount == 0)
        throw new ConcurrencyConflictException(document.Id, expectedVersion);

    if (envelopes.Count > 0)
        await _outbox.InsertManyAsync(s, envelopes, cancellationToken: t);

    return true;
}, cancellationToken: ct);
```

### 3. Marcar publicado antes de publicar

```csharp
// ❌ Si el broker rechaza o el proceso muere, el evento queda marcado
//    como publicado y no se reintenta jamás.
await MarkPublishedAsync(envelope, ct);
await _bus.PublishAsync(envelope, ct);
```

```csharp
// ✅ Primero el broker confirma, después se marca.
await _bus.PublishAsync(envelope, ct);          // con publisher confirms
await MarkPublishedAsync(envelope.EventId, ct); // publishedAt = now
```

Peor marcar dos veces —que el consumidor deduplica— que perder un evento.

### 4. Consumidor sin inbox

```csharp
// ❌ At-least-once: una redelivery genera dos comisiones.
public async Task HandleAsync(ReservationConfirmed e, CancellationToken ct)
{
    await _commissions.CreateAsync(e.ReservationId, ct);
}
```

```csharp
// ✅ El registro de inbox y el efecto viajan juntos; el duplicado no hace nada.
public async Task HandleAsync(ReservationConfirmed e, CancellationToken ct)
{
    using var session = await _client.StartSessionAsync(cancellationToken: ct);
    try
    {
        await session.WithTransactionAsync(async (s, t) =>
        {
            await _inbox.InsertOneAsync(s,
                new InboxRecord(e.TenantId, e.EventId, DateTime.UtcNow),
                cancellationToken: t);

            await _commissions.CreateAsync(s, e.ReservationId, t);
            return true;
        }, cancellationToken: ct);
    }
    catch (MongoWriteException ex)
        when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
    {
        // Ya procesado. Ack y seguir: no es un error.
    }
}
```

### 5. Aggregate completo y PII en el payload

```csharp
// ❌ Replica el aggregate, filtra PII y obliga a versionar ante cualquier cambio.
new PartyRegistered(party); // nombre, DNI, CUIT, teléfonos, domicilio, contactos...
```

```csharp
// ✅ Lo mínimo para reaccionar. El resto se consulta por API si hace falta.
new PartyRegistered(
    PartyId: party.Id,
    TenantId: party.TenantId,
    Kind: party.Kind,
    OccurredAt: occurredAt);
```

### 6. Cambio incompatible en el lugar

```csharp
// ❌ Renombrar un campo rompe en silencio a todos los consumidores desplegados.
public sealed record ListingActivated(Guid ListingId, decimal Price);
// pasa a ser:
public sealed record ListingActivated(Guid ListingId, decimal AskingPrice);
```

```csharp
// ✅ Versión nueva, y las dos conviven hasta que todos migren.
public sealed record ListingActivatedV1(Guid ListingId, decimal Price);
public sealed record ListingActivatedV2(Guid ListingId, Money AskingPrice);
```

### 7. Evento que en realidad es una orden

```csharp
// ❌ El emisor decide qué tiene que hacer el receptor.
new NotifyAgentOfNewMatch(agentId, matchId);
```

```csharp
// ✅ Hecho ocurrido. notification-service decide si notifica y cómo.
new MatchGenerated(matchId, requirementId, listingId, score, tenantId, occurredAt);
```

## Índices requeridos

```csharp
// <servicio>_outbox
// pendientes: índice pequeño, solo lo no publicado
{ occurredAt: 1 }   partialFilterExpression: { publishedAt: { $exists: false } }
// limpieza automática a los 7 días
{ publishedAt: 1 }  expireAfterSeconds: 604800

// <servicio>_inbox
{ tenantId: 1, messageId: 1 }  unique: true
{ processedAt: 1 }             expireAfterSeconds: 604800
```

El TTL del inbox define la ventana real de deduplicación: un duplicado que llegue
después de 7 días vuelve a procesarse. Es un compromiso deliberado.

## Checklist antes de devolver código

- [ ] Ningún `Publish` fuera del relay.
- [ ] Aggregate y outbox se escriben en la misma transacción.
- [ ] `publishedAt` se marca después de la confirmación del broker.
- [ ] Todo consumidor registra el `messageId` en el inbox, en la misma transacción
      que su efecto.
- [ ] El duplicate key del inbox se trata como "ya procesado", no como error.
- [ ] Los eventos tienen el envelope completo y nombre en pasado.
- [ ] `correlationId` y `causationId` se propagan.
- [ ] El payload es mínimo y no lleva PII innecesaria.
- [ ] Ningún cambio incompatible sobre un evento ya publicado sin versión nueva.
- [ ] Índices de outbox e inbox creados, incluidos los TTL.
- [ ] Hay tope de reintentos y camino a DLQ.
- [ ] Nada del diseño depende del orden entre aggregates distintos.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `ddd-hexagonal-architecture` | Decide **qué** es evento de dominio y qué de integración. Esta skill decide cómo se publica. |
| `mongodb-document-modeling` | El outbox es una colección más: índices, TTL y ownership siguen sus reglas. |
| `mongodb-dotnet-driver` | Sesiones, transacciones y manejo del duplicate key en C#. |
| `rabbitmq-dotnet` | Exchanges, bindings, publisher confirms, ack manual, DLQ, prefetch. |
| `aspnetcore-messaging` | Reglas agnósticas de consumo: shutdown graceful, backpressure, `CancellationToken`. |
| `cqrs-read-models-projections` | Los projectors son consumidores: mismo inbox, mismas reglas. |
| `aspnetcore-error-and-observability` | Trazas del relay, métricas de pendientes, sin PII en los logs de evento. |

---
name: rabbitmq-dotnet
description: |
  Activa cuando se escribe código .NET que habla con RabbitMQ en este CRM:
  conexión, canales, declaración de topología, publicación con confirms, consumo con
  ack manual, prefetch, DLQ, reintentos y recuperación ante caídas de red.
  Triggers: "RabbitMQ", "RabbitMQ.Client", "ConnectionFactory", "IConnection",
  "IChannel", "IModel", "CreateConnectionAsync", "CreateChannelAsync",
  "BasicPublishAsync", "BasicConsumeAsync", "BasicAckAsync", "BasicNackAsync",
  "BasicRejectAsync", "BasicQos", "prefetch", "AsyncEventingBasicConsumer",
  "IAsyncBasicConsumer", "ReceivedAsync", "EventingBasicConsumer", "exchange",
  "ExchangeDeclareAsync", "QueueDeclareAsync", "QueueBindAsync", "routing key",
  "binding", "topic exchange", "publisher confirms", "ConfirmSelect",
  "BasicReturn", "mandatory", "unroutable", "DLQ", "dead letter",
  "x-dead-letter-exchange", "deliveryTag", "redelivered", "AutomaticRecoveryEnabled",
  "TopologyRecoveryEnabled", "BrokerUnreachableException", "ConsumerDispatchConcurrency",
  "canal compartido", "cliente 6.x", "cliente 7.x", "upgrade de RabbitMQ".
  Garantiza conexión larga, un canal por publisher, confirms activos, ack manual
  después de procesar, prefetch acotado, DLQ configurada y payload copiado antes de
  que el handler retorne. NO activar para: decidir qué evento publicar o cómo
  garantizar que no se pierda (eso es event-driven-outbox-inbox), modelado de
  documentos ni capa REST.
---

# RabbitMQ .NET Client

## Objetivo

`event-driven-outbox-inbox` define **qué** se publica y **cómo se garantiza** que un
cambio de negocio no quede sin evento. Esta skill cubre la mecánica del broker: la
API del cliente, la topología y las trampas operativas.

Igual que el driver de MongoDB, el cliente de RabbitMQ tuvo un cambio mayor
incompatible: **la versión 7.0 reescribió la API a async y renombró la interfaz
central**. Casi todo el material que circula por internet es de la 6.x y **no
compila**. La regla es la misma: no copiar snippets sin verificarlos.

## Cuándo activar

- Se configura la conexión al broker o su registro en DI.
- Se declara topología: exchanges, queues, bindings, DLQ.
- Se implementa el publisher del relay de outbox.
- Se implementa un consumidor.
- Se ajusta prefetch, concurrencia o reintentos.
- Aparece un error del broker: canal cerrado, mensaje no ruteable, mensaje en DLQ.

## Cuándo NO activar

- Qué evento corresponde emitir, envelope, versionado, idempotencia, inbox (ver
  `event-driven-outbox-inbox`).
- Reglas de dominio (ver `ddd-hexagonal-architecture`).
- Persistencia (ver las skills de MongoDB).
- Reglas agnósticas de consumo como shutdown graceful y backpressure, que ya cubre
  `aspnetcore-messaging`.

## Decisiones del proyecto

| Tema | Decisión |
|---|---|
| Paquete | `RabbitMQ.Client` **7.x**, versión exacta en `Directory.Packages.props` |
| Broker | RabbitMQ **4.x** Community + management UI, vía Docker Compose |
| Conexión | Una `IConnection` por proceso, larga. Nombre vía `ClientProvidedName` |
| Canales | Uno por publisher y uno por consumidor. **Nunca compartidos entre hilos** |
| Exchange | Uno por servicio publicador: `<servicio>.events`, tipo **topic**, durable |
| Routing key | `<aggregate>.<NombreEvento>.v<version>` — ej. `party.PartyRegistered.v1` |
| Queue | Una por consumidor y origen: `<consumidor>.<proposito>` — ej. `identity-resolution.party-events`. Durable |
| DLQ | Una por queue: `<queue>.dlq`, vía `x-dead-letter-exchange` |
| Confirms | **Publisher confirms obligatorios** en el relay |
| Ack | **Manual**, después de procesar. Nunca `autoAck` |
| Prefetch | Acotado y explícito. Default del proyecto: 20 |
| Concurrencia | `ConsumerDispatchConcurrency` en 1 salvo necesidad medida |
| Recovery | `AutomaticRecoveryEnabled` y `TopologyRecoveryEnabled` activos |
| Serialización | JSON UTF-8; el envelope lo define `FND-003` |

### Por qué exchange por servicio y no uno global

Un único exchange compartido acopla a todos: cualquiera puede publicar cualquier
routing key y nadie sabe quién produce qué. Un exchange por servicio publicador
hace explícito el ownership de los contratos y deja que cada consumidor se ate solo
a lo que necesita.

### Migración 6.x → 7.x (referencia)

| Tema | 6.x | 7.x |
|---|---|---|
| Canal | `IModel` | **`IChannel`** |
| Crear conexión | `CreateConnection()` | `await CreateConnectionAsync()` |
| Crear canal | `CreateModel()` | `await CreateChannelAsync()` |
| Publicar | `BasicPublish(...)` | `await BasicPublishAsync(...)` |
| Ack | `BasicAck(...)` | `await BasicAckAsync(...)` |
| Consumir | `BasicConsume(...)` | `await BasicConsumeAsync(...)` |
| Declarar | `QueueDeclare(...)` | `await QueueDeclareAsync(...)` |
| Consumidor | `EventingBasicConsumer` + `Received` | `AsyncEventingBasicConsumer` + **`ReceivedAsync`** |
| Propiedades | `channel.CreateBasicProperties()` | `new BasicProperties()` |
| Cerrar | `Close()` / `Dispose()` | `await CloseAsync()` / `await DisposeAsync()` |

Prácticamente **toda la superficie es async**. Un ejemplo de 6.x no compila en 7.x,
lo cual es preferible a fallar en runtime.

### Trampa de memoria del cliente 7.x

Desde la 7.0 el payload que llega al consumidor es `ReadOnlyMemory<byte>`. La
memoria **puede liberarse apenas el handler retorna**. Hay que deserializar o
copiar dentro del handler; guardar la referencia para usarla después lee basura o
falla de forma intermitente y muy difícil de diagnosticar.

## Estado actual vs target

- **Estado:** no hay publisher ni consumidores. `FND-004` levanta el broker,
  `FND-008` el harness de integración.
- **Target:** un building block compartido declara topología, publica con confirms
  y consume con ack manual y DLQ. Ningún servicio habla con `RabbitMQ.Client`
  directamente desde su capa de aplicación.

## Reglas obligatorias

### MUST

- **MUST** mantener una `IConnection` larga por proceso. Abrir conexión por mensaje
  es un anti-patrón explícito de la documentación oficial.
- **MUST** usar un `IChannel` por publisher, sin compartirlo entre hilos. Compartir
  un canal para publicar concurrentemente **corrompe el interleaving de frames** a
  nivel protocolo.
- **MUST** activar publisher confirms en el relay y marcar `publishedAt` solo tras
  la confirmación del broker.
- **MUST** deserializar o copiar el payload **dentro** del handler, antes de
  retornar.
- **MUST** usar ack manual: `autoAck: false`, y hacer `BasicAckAsync` después de
  procesar con éxito.
- **MUST** declarar exchanges y queues como **durables**, y publicar con
  `DeliveryMode = 2` (persistente).
- **MUST** configurar `x-dead-letter-exchange` en toda queue de negocio.
- **MUST** fijar prefetch explícito con `BasicQosAsync`. Sin prefetch, el broker
  empuja todo lo que puede y el consumidor se ahoga.
- **MUST** tratar los mensajes como **redelivery posible**: la recuperación
  automática resetea delivery tags y puede repetir entregas. La idempotencia la
  garantiza el inbox.
- **MUST** rechazar sin requeue (`BasicNackAsync(requeue: false)`) el mensaje que
  agotó reintentos, para que caiga en la DLQ.
- **MUST** reintentar la conexión inicial en el arranque: la recuperación automática
  **no cubre** el primer intento fallido.
- **MUST** setear `ClientProvidedName` con el nombre del servicio y su rol.
- **MUST** propagar `CancellationToken` y cerrar canales y conexión de forma
  ordenada al apagar.

### MUST NOT

- **MUST NOT** usar `IModel`, `CreateModel()`, `EventingBasicConsumer` ni ninguna
  API 6.x: no existen en 7.x.
- **MUST NOT** abrir una conexión o un canal por mensaje publicado.
- **MUST NOT** compartir un `IChannel` entre hilos para publicar.
- **MUST NOT** usar `autoAck: true` para mensajes de negocio: se pierde el mensaje
  si el consumidor falla.
- **MUST NOT** guardar la referencia al `ReadOnlyMemory<byte>` más allá del handler.
- **MUST NOT** hacer `BasicNackAsync(requeue: true)` sobre un mensaje que falla
  siempre: genera un loop caliente que satura al consumidor y al broker.
- **MUST NOT** usar `BasicGetAsync` (pull) para consumo continuo: es polling y la
  documentación oficial lo desaconseja explícitamente.
- **MUST NOT** poner lógica de negocio en el consumidor de transporte: deserializa,
  delega en el handler de aplicación, hace ack.
- **MUST NOT** asumir que un mensaje publicado mientras la conexión estaba caída
  llegó: el cliente **no lo encola**, se pierde. Por eso existe el outbox.
- **MUST NOT** usar `ulong` en headers: el cliente no lo soporta y lanza excepción.

## Recomendaciones

### SHOULD

- **SHOULD** declarar la topología una sola vez, al arranque, de forma idempotente:
  declarar algo que ya existe es un no-op.
- **SHOULD** distinguir fallo transitorio de mensaje envenenado. Un error de
  deserialización no mejora con reintentos: va directo a DLQ. Una base caída sí
  justifica reintentar con backoff.
- **SHOULD** acotar los reintentos en proceso, con backoff, y después rechazar a
  DLQ. Si hace falta más, la escalación es una topología de retry con TTL, no
  reintentos infinitos.
- **SHOULD** publicar con `mandatory: true` y suscribirse a `BasicReturn`: sin
  listener, un mensaje no ruteable **se descarta en silencio**.
- **SHOULD** dejar `ConsumerDispatchConcurrency` en 1. Con concurrencia mayor hay
  que acusar recibo de a un mensaje por vez para evitar el doble ack, que es un
  error de protocolo.
- **SHOULD** monitorear profundidad de queue, tasa de DLQ y edad del mensaje más
  viejo. Una DLQ que crece es un incidente, no una estadística.
- **SHOULD** incluir `correlationId` en las propiedades del mensaje además del
  envelope, para poder seguirlo desde la management UI.

### SHOULD NOT

- **SHOULD NOT** declarar topología desde el consumidor y desde el publisher con
  definiciones distintas: un mismatch cierra el canal.
- **SHOULD NOT** usar exchanges `fanout` para eventos de negocio: `topic` permite
  que cada consumidor filtre por routing key.
- **SHOULD NOT** subir el prefetch para "ir más rápido" sin medir: aumenta memoria y
  el tiempo de redelivery cuando algo falla.

## Anti-patrones prohibidos

### 1. API 6.x

```csharp
// ❌ No compila en 7.x: IModel, CreateModel y las versiones sync no existen.
IConnection conn = factory.CreateConnection();
IModel channel = conn.CreateModel();
channel.BasicPublish(exchange, routingKey, props, body);

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (s, ea) => { /* ... */ };
```

```csharp
// ✅ 7.x: todo async, IChannel, AsyncEventingBasicConsumer.
await using IConnection conn = await factory.CreateConnectionAsync(ct);
await using IChannel channel = await conn.CreateChannelAsync(cancellationToken: ct);

await channel.BasicPublishAsync(exchange, routingKey, mandatory: true,
    basicProperties: props, body: body, cancellationToken: ct);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (sender, ea) => { /* ... */ };
```

### 2. Conexión por mensaje

```csharp
// ❌ Handshake TCP y AMQP por cada evento publicado.
public async Task PublishAsync(Envelope e, CancellationToken ct)
{
    var conn = await _factory.CreateConnectionAsync(ct);
    var ch = await conn.CreateChannelAsync(cancellationToken: ct);
    await ch.BasicPublishAsync(/* ... */);
}
```

```csharp
// ✅ Conexión singleton, canal propio del publisher, ambos largos.
services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(cfg.AmqpUri),
        ClientProvidedName = $"{cfg.ServiceName}:outbox-relay",
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
```

### 3. Canal compartido entre hilos

```csharp
// ❌ Dos hilos publicando en el mismo canal: frames entrelazados,
//    excepción a nivel conexión y errores raros en el log del broker.
await Parallel.ForEachAsync(envelopes, ct, async (e, t) =>
    await _sharedChannel.BasicPublishAsync(/* ... */));
```

```csharp
// ✅ Un canal por publisher; si hay concurrencia, exclusión mutua explícita.
await _channelLock.WaitAsync(ct);
try
{
    await _channel.BasicPublishAsync(exchange, routingKey, true, props, body, ct);
}
finally
{
    _channelLock.Release();
}
```

### 4. Ack automático

```csharp
// ❌ El mensaje se da por procesado al entregarse. Si el handler falla, se perdió.
await channel.BasicConsumeAsync(queue, autoAck: true, consumer);
```

```csharp
// ✅ Ack manual después de procesar; nack sin requeue al agotar reintentos.
await channel.BasicQosAsync(0, prefetchCount: 20, global: false, ct);
await channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);

consumer.ReceivedAsync += async (sender, ea) =>
{
    try
    {
        var envelope = JsonSerializer.Deserialize<Envelope>(ea.Body.Span)!; // copia acá
        await _handler.HandleAsync(envelope, ct);
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
    }
    catch (JsonException)
    {
        // Mensaje envenenado: no mejora reintentando.
        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
    }
    catch (Exception)
    {
        // Transitorio: a DLQ tras agotar reintentos, nunca requeue infinito.
        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
    }
};
```

### 5. Retener el payload después del handler

```csharp
// ❌ La memoria puede estar liberada cuando el trabajo en background la lee.
consumer.ReceivedAsync += (sender, ea) =>
{
    _queue.Enqueue(ea.Body);   // referencia a memoria que ya no es tuya
    return Task.CompletedTask;
};
```

```csharp
// ✅ Deserializar (o copiar) dentro del handler.
consumer.ReceivedAsync += async (sender, ea) =>
{
    var envelope = JsonSerializer.Deserialize<Envelope>(ea.Body.Span)!;
    await _handler.HandleAsync(envelope, ct);
};
```

### 6. Queue sin DLQ

```csharp
// ❌ Un mensaje irrecuperable desaparece sin dejar rastro.
await channel.QueueDeclareAsync("identity-resolution.party-events",
    durable: true, exclusive: false, autoDelete: false, arguments: null,
    cancellationToken: ct);
```

```csharp
// ✅ Los rechazados caen en una DLQ inspeccionable.
await channel.ExchangeDeclareAsync("identity-resolution.dlx", ExchangeType.Fanout,
    durable: true, cancellationToken: ct);

await channel.QueueDeclareAsync("identity-resolution.party-events.dlq",
    durable: true, exclusive: false, autoDelete: false, arguments: null,
    cancellationToken: ct);

await channel.QueueBindAsync("identity-resolution.party-events.dlq",
    "identity-resolution.dlx", routingKey: "", cancellationToken: ct);

await channel.QueueDeclareAsync("identity-resolution.party-events",
    durable: true, exclusive: false, autoDelete: false,
    arguments: new Dictionary<string, object?>
    {
        ["x-dead-letter-exchange"] = "identity-resolution.dlx"
    },
    cancellationToken: ct);
```

### 7. Publicar sin confirms y dar por publicado

```csharp
// ❌ Publicar en AMQP es asincrónico: que no tire excepción no significa que llegó.
//    Si la conexión estaba cayéndose, el mensaje se perdió y nadie se entera.
await channel.BasicPublishAsync(exchange, routingKey, props, body, ct);
await MarkPublishedAsync(envelope.EventId, ct);
```

```csharp
// ✅ Confirms activos: el relay marca solo tras la confirmación del broker.
//    Si no confirma, el evento queda pendiente en el outbox y se reintenta.
await _publisher.PublishConfirmedAsync(envelope, ct);
await MarkPublishedAsync(envelope.EventId, ct);
```

## Checklist antes de devolver código

- [ ] Ninguna API 6.x: `IModel`, `CreateModel`, `EventingBasicConsumer`, métodos sync.
- [ ] Una conexión larga por proceso, con `ClientProvidedName`.
- [ ] Un canal por publisher; ninguno compartido entre hilos sin exclusión mutua.
- [ ] Publisher confirms activos; `publishedAt` se marca después de confirmar.
- [ ] `autoAck: false` y ack explícito tras procesar.
- [ ] Payload deserializado o copiado dentro del handler.
- [ ] Exchanges y queues durables; mensajes persistentes.
- [ ] `x-dead-letter-exchange` en toda queue de negocio.
- [ ] `BasicQosAsync` con prefetch explícito.
- [ ] Mensaje envenenado va a DLQ sin reintentar; transitorio reintenta acotado.
- [ ] Ningún `requeue: true` sobre un fallo permanente.
- [ ] Reintento de la conexión inicial en el arranque.
- [ ] `mandatory: true` con listener de `BasicReturn`.
- [ ] Cierre ordenado de canales y conexión al apagar.

## Conexiones con otros skills

| Skill | Relación |
|---|---|
| `event-driven-outbox-inbox` | Define qué se publica, el envelope, la idempotencia y el inbox. Esta skill es el transporte. |
| `aspnetcore-messaging` | Reglas agnósticas del broker: `BackgroundService`, shutdown graceful, backpressure. No duplicar. |
| `dotnet-async-and-concurrency` | Toda la API 7.x es async; canales no son thread-safe. |
| `aspnetcore-config-and-secrets` | La URI del broker es un secreto: viene de configuración. |
| `aspnetcore-error-and-observability` | Métricas de queue y DLQ, trazas de publicación y consumo. |
| `dotnet-adversarial-testing` | Broker caído, mensajes malformados, redeliveries, DLQ llena. |
| `mongodb-dotnet-driver` | El relay lee el outbox de MongoDB y publica acá: las dos skills se aplican juntas. |

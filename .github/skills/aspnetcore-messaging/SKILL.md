---
name: aspnetcore-messaging
description: |
  Activa cuando el microservicio ASP.NET Core 10 produce o consume mensajes de
  un broker de mensajería (Kafka con Confluent .NET client, Azure Service Bus,
  u otro). Triggers: "IHostedService", "BackgroundService", "consumer loop",
  "Kafka", "KafkaConsumer", "Confluent.Kafka", "IConsumer<TKey, TValue>",
  "IProducer<TKey, TValue>", "ConsumerBuilder", "ProducerBuilder",
  "Azure Service Bus", "ServiceBusClient", "ServiceBusProcessor",
  "ServiceBusSender", "topic", "partition", "offset", "commit", "idempotente",
  "idempotencia", "at-least-once", "deduplicación", "DLQ", "dead letter",
  "poison message", "mensaje malformado", "deserialization error",
  "CancellationToken consumer", "graceful shutdown", "backpressure",
  "Channel<T>", "serialización de eventos", "schema versionado",
  "consumer group", "rebalance", "offset commit", "procesamiento en background",
  "publicar evento", "enviar mensaje", "IServiceScopeFactory mensajería".
  Garantiza consumers idempotentes, commit de offset/ack post-proceso, DLQ para
  mensajes irrecuperables, shutdown graceful con CancellationToken y backpressure
  controlada. IMPORTANTE: esta skill cubre patrones agnósticos al broker. El broker
  de este proyecto es RabbitMQ (ver docs/implementation/POC_TECH_DECISIONS.md) y las
  reglas específicas viven en las skills rabbitmq-dotnet y event-driven-outbox-inbox,
  pendientes de escribir. NO activar para: llamadas HTTP salientes ni acceso a DB aislado.
---

# ASP.NET Core Messaging

## Objetivo

Un broker de mensajería como Kafka entrega mensajes **at-least-once** por defecto:
si el consumer procesa un mensaje pero falla antes de commitear el offset (o el
acknowledgment en Service Bus), el mensaje se reprocesa. Esto significa que
**todo consumer debe ser idempotente**: procesar el mismo mensaje N veces debe
tener el mismo efecto que procesarlo una vez. Los mensajes que nunca pueden
procesarse (poison messages) deben ir a una Dead Letter Queue (DLQ) en lugar de
bloquear el consumer indefinidamente. El shutdown del proceso debe ser graceful:
no perder mensajes en tránsito al apagar el pod.

> **⚠️ Nota de incertidumbre:** El README del arquetipo `epa-net-paas` (v1.1.8)
> **no documenta explícitamente** el stack de mensajería. Las reglas de este skill
> cubren patrones aplicables tanto a **Kafka (Confluent .NET)** como a
> **Azure Service Bus**. Si el proyecto confirma uno u otro broker, adaptar la
> configuración específica (builders, serializers, ack mode). **Consultar al
> equipo de plataforma antes de elegir el broker.**

## Cuándo activar

- Se crea un consumer de mensajes (`BackgroundService`, `IHostedService`).
- Se crea un producer de mensajes (Kafka `IProducer`, Service Bus `ServiceBusSender`).
- Se diseña el schema de un evento o mensaje.
- Se configura el DLQ o el manejo de mensajes irrecuperables.
- Se trabaja con idempotencia, commit de offset, o acknowledgment.
- Se diseña el graceful shutdown de un consumer.
- Se monitorea el consumer lag o se configuran alertas de DLQ.

## Cuándo NO activar

- Llamadas HTTP salientes (ver `aspnetcore-outgoing-http`).
- Acceso a BD sin mensajería (ver `aspnetcore-database-access-efcore`).

## Estado actual vs target

- **Stack no confirmado:** el arquetipo `epa-net-paas` no especifica el broker.
  Los patrones de este skill son agnósticos; los ejemplos de código usan Confluent
  Kafka (.NET) como referencia principal con notas equivalentes para Service Bus.
- **Target comportamental:** consumers implementados como `BackgroundService` con
  `CancellationToken` propagado, idempotencia garantizada por ID de mensaje o
  clave de negocio, offset/ack commiteado solo tras proceso exitoso, DLQ
  configurado para mensajes irrecuperables, shutdown graceful que espera a que
  el mensaje en proceso termine antes de cerrar el consumer.

## Decisiones del proyecto

- **Consumers como `BackgroundService`** en el proceso de ASP.NET Core.
  `IHostedService` + `CancellationToken` del host gestiona el ciclo de vida.
  El `BackgroundService` es un singleton; inyectar `IServiceScopeFactory` para
  crear un scope por mensaje y obtener `DbContext` y otros servicios Scoped.
- **Consumers idempotentes siempre.** Guardar el message id (o un campo de negocio
  único) y verificar si ya se procesó antes de ejecutar la lógica.
- **Commit de offset / ack después de procesar,** nunca antes. El at-least-once
  garantiza reprocesamiento; con el ack post-proceso, el reprocesamiento es
  seguro si el consumer es idempotente.
- **DLQ configurado para todos los consumers.** Un mensaje que supera el máximo de
  reintentos va al DLQ para revisión manual y nunca se descarta silenciosamente.
- **Serialización de eventos versionada.** Usar schemas con evolución documentada
  (JSON Schema, Avro/Schema Registry, Protocol Buffers). No bytes planos sin contrato.
- **Backpressure explícita.** El consumer no debe consumir más mensajes de los que
  puede procesar; usar `MaxConcurrentCalls` (Service Bus) o controlar el polling
  rate (Kafka) con un `Channel<T>` bounded.

## Reglas obligatorias

### MUST

1. **MUST hacer el consumer idempotente.** Antes de ejecutar la lógica de negocio,
   verificar si el mensaje ya fue procesado (por `MessageId`, clave de negocio, o
   campo único). Guardar el registro de procesamiento en DB o cache distribuida.

2. **MUST commitear el offset / hacer ack solo después de procesar exitosamente.**
   Nunca pre-ack antes de la lógica de negocio. Si el proceso falla, el mensaje
   se reprocesa (at-least-once semántica).

3. **MUST configurar DLQ para todo consumer.** Los mensajes que superan el máximo
   de reintentos deben moverse a un DLQ (dead-letter topic en Kafka, dead-letter
   sub-queue en Service Bus). Nunca descartar silenciosamente.

4. **MUST manejar mensajes malformados (poison messages) enviándolos al DLQ
   inmediatamente,** sin reintentos. Un mensaje con schema inválido nunca se va
   a poder procesar; reintentarlo solo bloquea el consumer.

5. **MUST propagar `CancellationToken`** en el loop de consumo y en toda llamada
   async dentro del procesamiento. El `CancellationToken` del `BackgroundService`
   viene del host y se cancela en el shutdown.

6. **MUST implementar graceful shutdown:** cuando se recibe la señal de stop,
   terminar de procesar el mensaje en curso antes de cerrar el consumer. No
   perder mensajes en tránsito al apagar el pod.

7. **MUST loggear el message key / ID y el offset o número de secuencia** al
   procesar un mensaje. Es necesario para rastrear mensajes problemáticos en el DLQ.

8. **MUST usar `IServiceScopeFactory.CreateScope()` por mensaje** en el
   `BackgroundService` para obtener `DbContext` y servicios Scoped. El
   `BackgroundService` es Singleton y no puede inyectar Scoped directamente.

### MUST NOT

9. **MUST NOT hacer auto-commit de offset antes de procesar.** En Kafka,
   deshabilitar `enable.auto.commit=true` si se usa commit manual. En Service Bus,
   no llamar a `CompleteMessageAsync` antes de la lógica de negocio.

10. **MUST NOT descartar mensajes del DLQ silenciosamente.** Los mensajes en el
    DLQ deben tener monitoring de lag y alertas operacionales.

11. **MUST NOT ejecutar lógica de negocio con side effects** en el consumer sin
    garantizar idempotencia. Si la lógica escribe en DB o llama a un servicio,
    debe ser idempotente.

12. **MUST NOT ignorar `OperationCanceledException`** en el loop de consumo; es
    la señal de shutdown del host y debe usarse para terminar el loop limpiamente.

## Recomendaciones

### SHOULD

- Usar `Channel<T>` con bounded capacity para desacoplar el loop de consumo del
  procesamiento: un thread lee mensajes del broker y los pone en el channel; uno
  o más threads los procesan. Provee backpressure natural y evita OOM en spikes.
- Instrumentar el consumer con métricas de lag, throughput, y tasa de mensajes
  al DLQ via OpenTelemetry.
- Definir el schema del evento en un contrato versionado (JSON Schema, Avro,
  Protobuf) y versionarlo junto al código.
- Usar `IOptions<T>` para configurar los parámetros del broker (endpoints, topics,
  consumer groups, timeouts) vía env vars; nunca hardcodear.

### SHOULD NOT

- No bloquear el thread del loop de consumo con trabajo CPU-intensivo; delegarlo
  a `Task.Run` con concurrencia acotada o a un `Channel<T>`.
- No compartir un `DbContext` (Scoped) entre el loop del consumer y el handler;
  crear un scope por mensaje con `IServiceScopeFactory`.

## Anti-patrones prohibidos

❌ Consumer sin idempotencia (puede procesar una transferencia dos veces):

```csharp
public class PagoConsumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var result = _consumer.Consume(ct);
            // ❌ sin chequeo de idempotencia; si falla el commit, se reprocesa y se ejecuta 2 veces
            await _pagoService.ProcesarAsync(result.Message.Value, ct);
            _consumer.Commit(result);
        }
    }
}
```

✅ Consumer idempotente con scope por mensaje:

```csharp
public class PagoConsumer : BackgroundService
{
    private readonly IConsumer<string, PagoEvent> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var result = _consumer.Consume(ct);
            var messageId = result.Message.Key;

            using var scope = _scopeFactory.CreateScope(); // ✅ scope por mensaje
            var pagoRepo = scope.ServiceProvider.GetRequiredService<IPagoRepository>();

            if (await pagoRepo.YaFueProcesadoAsync(messageId, ct))
            {
                _logger.LogInformation("Mensaje ya procesado, descartando. MessageId: {Id}", messageId);
                _consumer.Commit(result); // ✅ commit y continuar
                continue;
            }

            var pagoService = scope.ServiceProvider.GetRequiredService<IPagoService>();
            await pagoService.ProcesarAsync(result.Message.Value, ct);
            await pagoRepo.RegistrarProcesadoAsync(messageId, ct);
            _consumer.Commit(result); // ✅ commit solo después del proceso exitoso
        }
    }
}
```

❌ Shutdown que corta mensajes en proceso (pérdida de datos):

```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var msg = _consumer.Consume(ct);
        _ = Task.Run(() => ProcesarAsync(msg), ct); // ❌ fire-and-forget; al cancelar ct, el Task se pierde
    }
}
```

✅ Graceful shutdown esperando el mensaje en curso:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        ConsumeResult<string, PagoEvent> result;
        try
        {
            result = _consumer.Consume(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            break; // ✅ shutdown limpio al cancelar el token
        }

        try
        {
            // ✅ CancellationToken.None: el mensaje en curso se completa aunque se pida stop
            await ProcesarAsync(result, CancellationToken.None);
            _consumer.Commit(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando mensaje. Key: {Key}, Offset: {Offset}",
                result.Message.Key, result.Offset);
            await EnviarADlqAsync(result, ex, CancellationToken.None); // ✅ DLQ en lugar de descartar
        }
    }
    _consumer.Close(); // ✅ libera el consumer group limpiamente
}
```

❌ `DbContext` inyectado directamente en `BackgroundService` (Singleton):

```csharp
public class EventConsumer : BackgroundService
{
    private readonly AppDbContext _context; // ❌ Scoped inyectado en Singleton → excepción en runtime o state corruption

    public EventConsumer(AppDbContext context) => _context = context;
}
```

✅ `IServiceScopeFactory` para crear scope por mensaje:

```csharp
public class EventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; // ✅ Singleton-safe

    public EventConsumer(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    private async Task ProcesarAsync(MiEvento evento, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // ✅ DbContext Scoped creado fresh por mensaje
    }
}
```

## Checklist antes de devolver código

- [ ] El consumer es idempotente (chequea si el mensaje ya fue procesado antes de actuar).
- [ ] El offset/ack se commitea solo después de procesar exitosamente.
- [ ] Hay DLQ configurado (o lógica de envío al DLQ) para mensajes irrecuperables y poison messages.
- [ ] `CancellationToken` propagado en el loop de consumo y en toda llamada async.
- [ ] El `BackgroundService` usa `IServiceScopeFactory.CreateScope()` por mensaje (no `DbContext` Singleton).
- [ ] El graceful shutdown espera a que el mensaje en curso termine antes de cerrar el consumer.
- [ ] Se loggea el message key/ID y offset al procesar.
- [ ] Los parámetros del broker vienen de env vars (no hardcodeados).
- [ ] Se confirmó con el equipo de plataforma qué broker usa el arquetipo `epa-net-paas`.

## Conexiones con otros skills

- `aspnetcore-database-access-efcore` — guardar el registro de idempotencia en DB; crear scope por mensaje con `IServiceScopeFactory`.
- `aspnetcore-error-and-observability` — logging del consumer lag, tasa de mensajes al DLQ; tracing de Jaeger por mensaje con `Activity`.
- `dotnet-async-and-concurrency` — backpressure con `Channel<T>` bounded; `Parallel.ForEachAsync` para procesamiento paralelo acotado.
- `dotnet-parsing-and-validation` — validar el payload del mensaje antes de procesar; manejar errores de deserialización como poison messages.
- `aspnetcore-di-and-middleware-pipeline` — `BackgroundService` registrado como hosted service; ciclo de vida y scope factories.

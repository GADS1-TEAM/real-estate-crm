# ADR-004 — Topología RabbitMQ: exchange por servicio publicador

| Campo | Valor |
|---|---|
| Estado | `ACCEPTED` |
| Fecha | 2026-09-02 |
| Autor | Fabri |
| Task / PRP relacionado | `FND-003`, `FND-004` |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

- [x] semántica de evento público
- [x] contrato público incompatible

## Contexto

`ARCHITECTURE.md` §8 fija RabbitMQ como broker y define el envelope, pero no la
topología: quién declara qué exchange, cómo se nombran las routing keys ni cómo se
atan los consumidores.

Sin esa convención, cada servicio inventa la suya y no hay forma de saber quién
produce qué.

Además, el cliente `RabbitMQ.Client` 7.0 reescribió la API a async y renombró
`IModel` como `IChannel`: el material de 6.x que circula no compila.

## Decisión

**Cliente:** `RabbitMQ.Client` **7.x**, versión exacta y centralizada. Broker
RabbitMQ 4.x Community.

**Topología:**

```text
exchange     <servicio>.events            tipo topic, durable
routing key  <aggregate>.<Evento>.v<N>    ej. party.PartyRegistered.v1
queue        <consumidor>.<proposito>     ej. identity-resolution.party-events
DLQ          <queue>.dlq                  vía x-dead-letter-exchange
```

**Garantías:** publisher confirms obligatorios en el relay; ack manual siempre;
prefetch explícito, default 20; `ConsumerDispatchConcurrency` en 1 salvo necesidad
medida.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| Un exchange global compartido | Acopla a todos: cualquiera puede publicar cualquier routing key y nadie sabe quién produce qué. El ownership de contratos se vuelve invisible. |
| Exchange `fanout` | Obliga a cada consumidor a recibir todo y filtrar en código. `topic` deja que el binding haga el filtrado. |
| Un exchange por evento | Explota en cantidad de objetos y no aporta sobre las routing keys de un topic. |
| Cliente 6.x | API síncrona y sin soporte futuro. El proyecto nace nuevo: no hay motivo para empezar en la versión anterior. |

## Consecuencias

**Aceptamos:**

- el ownership de los contratos de evento es explícito por exchange;
- cada consumidor se ata solo a lo que necesita, por routing key;
- la versión del contrato viaja en la routing key: un consumidor puede quedarse en
  `v1` mientras otro migra a `v2`;
- toda queue tiene DLQ inspeccionable.

**Perdemos:**

- más objetos de topología que con un exchange único;
- hay que declarar la topología de forma consistente desde publisher y consumer: un
  mismatch cierra el canal.

**Deuda que queda abierta:**

- los reintentos del consumidor son en proceso y acotados, y después DLQ. Una
  topología de retry con TTL queda como escalación si se mide que hace falta.

## Impacto en el repositorio

- Documentos actualizados: ninguno fuera de las skills.
- Skills afectadas: `rabbitmq-dotnet`, `event-driven-outbox-inbox`,
  `aspnetcore-messaging`.
- Tasks afectadas: `FND-003`, `FND-004`, `FND-008` y toda task con eventos.
- Contratos que cambian de versión: define el formato de routing key, que es
  contrato público.
- Servicios que deben migrar datos: ninguno.

## Revisión

Se revisa si el volumen de eventos o la cantidad de consumidores hace que la
topología por servicio resulte incómoda de operar.

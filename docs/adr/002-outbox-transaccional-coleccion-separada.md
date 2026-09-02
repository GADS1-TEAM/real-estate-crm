# ADR-002 — Outbox transaccional en colección separada

| Campo | Valor |
|---|---|
| Estado | `ACCEPTED` |
| Fecha | 2026-09-02 |
| Autor | Fabri |
| Task / PRP relacionado | `FND-003`, `FND-004` |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

- [x] estrategia de consistencia cross-service
- [x] invariante core

## Contexto

Guardar un cambio de negocio y publicar su evento son dos escrituras a dos sistemas
distintos: MongoDB y RabbitMQ. Sin coordinación se pierde un evento o se anuncia un
hecho que no ocurrió.

`ARCHITECTURE.md` §8 exige Outbox, pero no define cómo se implementa sobre MongoDB.
La tensión es con la regla de modelado del proyecto:

> `mongodb-document-modeling` — "resolver una operación de negocio con una sola
> escritura de documento. La atomicidad de MongoDB es por documento."

## Decisión

Cada servicio tiene una colección **`<servicio>_outbox`** propia. El aggregate y sus
eventos se escriben en una **transacción multi-documento**.

Esta es la **única excepción sancionada** a la regla de una escritura por operación.
Cualquier otro uso de transacciones requiere ADR propio.

Un `BackgroundService` (relay) lee lo no publicado, publica con publisher confirms y
recién entonces marca `publishedAt`. Un índice TTL de 7 días limpia lo publicado.
El inbox es simétrico: índice único `(tenantId, messageId)` y mismo TTL.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| Eventos embebidos en el documento del aggregate | Da atomicidad real sin transacciones ni replica set, pero mete contabilidad de infraestructura dentro del aggregate — lo que contradice la regla hexagonal más de lo que lo hace la transacción. Además exige un poller e índice parcial por colección (decenas de piezas casi iguales), y vuelve incómoda la retención: o se borra el evento y se pierde el rastro, o el documento crece. |
| Colección separada sin transacción | No resuelve nada: deja la misma ventana de pérdida que el patrón viene a cerrar. |
| Change streams derivando eventos de los diffs | Requiere replica set igual y hace frágil el contrato: derivar eventos de negocio de diferencias de documento pierde intención. |

## Consecuencias

**Aceptamos:**

- ningún cambio de negocio queda sin su evento;
- el patrón es el clásico de bibliografía, con mucho material correcto disponible;
- un solo relay por servicio en un building block compartido;
- retención y limpieza triviales con un índice TTL;
- los aggregates quedan libres de estado de infraestructura.

**Perdemos:**

- MongoDB debe ser replica set (ver [ADR-001](001-mongodb-replica-set-nodo-unico.md));
- costo de performance de la transacción frente a una escritura simple;
- una excepción a una regla, que hay que mantener explícita para que no se generalice.

**Deuda que queda abierta:**

- el relay arranca con polling; migrar a change streams cuando se mida que hace falta;
- la ventana de deduplicación del inbox es de 7 días: un duplicado posterior se
  reprocesa.

## Impacto en el repositorio

- Documentos actualizados: `docs/tasks/wave-0-foundation/FND-004-infra-local.md`.
- Skills afectadas: `event-driven-outbox-inbox`, `mongodb-document-modeling`,
  `mongodb-dotnet-driver`, `ddd-hexagonal-architecture`.
- Tasks afectadas: `FND-003`, `FND-004`, `FND-008` y toda task que publique eventos.
- Contratos que cambian de versión: define el envelope, versionado en `FND-003`.
- Servicios que deben migrar datos: ninguno.

## Revisión

Se revisa si el costo de las transacciones resulta medible y molesto, o si aparece
un patrón mejor soportado por el driver.

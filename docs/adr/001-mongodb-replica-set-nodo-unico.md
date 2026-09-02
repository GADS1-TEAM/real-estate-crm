# ADR-001 — Levantar MongoDB como replica set de un nodo

| Campo | Valor |
|---|---|
| Estado | `ACCEPTED` |
| Fecha | 2026-09-02 |
| Autor | Fabri |
| Task / PRP relacionado | `FND-004` |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

- [x] estrategia de consistencia cross-service
- [x] datastore principal

## Contexto

`POC_TECH_DECISIONS.md` define MongoDB Community como persistencia y busca costo de
infraestructura cero con operación mínima. La lectura natural de esa restricción es
un `mongod` standalone.

Pero las **transacciones multi-documento y los change streams de MongoDB solo
funcionan sobre replica set o sharded cluster**. Un standalone no los soporta.
Verificado contra la documentación oficial de MongoDB.

Eso choca con [ADR-002](002-outbox-transaccional-coleccion-separada.md), que escribe
el aggregate y su outbox en una misma transacción.

> `ARCHITECTURE.md` §8 — "Outbox en el mismo boundary de persistencia que el cambio
> de negocio"

## Decisión

MongoDB se levanta como **replica set de un solo nodo** (`--replSet rs0`), iniciado
automáticamente al arrancar el compose.

Sigue siendo un contenedor, una imagen y cero costo. Solo cambia la configuración de
arranque.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| Standalone + outbox embebido en el documento del aggregate | Evita el replica set, pero mete contabilidad de infraestructura dentro del estado persistente del aggregate, obliga a un poller por colección y complica la retención. Ver ADR-002. |
| Standalone + outbox sin transacción | Deja abierta la ventana de "cambio guardado sin evento publicado", que es exactamente el problema que el patrón outbox existe para resolver. |
| Replica set de 3 nodos | Realista para producción, innecesario para la POC: triplica recursos sin agregar nada que la POC necesite validar. |

## Consecuencias

**Aceptamos:**

- transacciones multi-documento disponibles para el outbox;
- change streams disponibles: el relay puede reaccionar en vez de hacer polling;
- la POC se parece más a un despliegue real.

**Perdemos:**

- dos líneas más de configuración en el compose y un `rs.initiate()` de arranque;
- un nodo único en replica set no da alta disponibilidad: es configuración, no
  redundancia.

**Deuda que queda abierta:**

- pasar a 3 nodos cuando haya un entorno que requiera disponibilidad real.

## Impacto en el repositorio

- Documentos actualizados: `docs/tasks/wave-0-foundation/FND-004-infra-local.md`.
- Skills afectadas: `mongodb-document-modeling`, `mongodb-dotnet-driver`,
  `event-driven-outbox-inbox`.
- Tasks afectadas: `FND-004`, `FND-008`.
- Contratos que cambian de versión: ninguno.
- Servicios que deben migrar datos: ninguno.

## Revisión

Se revisa si aparece un entorno que exija alta disponibilidad, o si el costo
operativo del replica set resulta mayor al previsto en la POC.

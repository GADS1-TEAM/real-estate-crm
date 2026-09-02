# Architecture Decision Records

Registro de decisiones que cambian una regla global del proyecto.

`README.md`, `ARCHITECTURE.md` y `AGENTS.md` describen el estado vigente. Los ADR
explican **por qué** ese estado es el que es y qué se descartó para llegar ahí.

## Cuándo abrir un ADR

Cuando el cambio toca algo de `ARCHITECTURE.md` §18:

- ownership de aggregate/colección;
- límites de microservicio;
- datastore principal;
- estrategia de consistencia cross-service;
- contrato público incompatible;
- semántica de evento público;
- invariante core;
- política global de tenancy/autorización.

También cuando un work package necesita agregar infraestructura que
`docs/implementation/POC_TECH_DECISIONS.md` excluye de la POC (Redis, OpenSearch,
Kafka, MinIO, Kubernetes u otra base de datos).

## Cuándo NO abrir un ADR

Para detalles locales y reversibles dentro de una task. `AGENTS.md` es explícito:
elegir la opción más simple compatible y documentarla en el PR. Un ADR por cada
decisión menor vacía el mecanismo de significado.

Regla práctica: si revertirlo mañana no obliga a tocar otro servicio, no es un ADR.

## Cómo se escribe

1. Copiar [`ADR_TEMPLATE.md`](ADR_TEMPLATE.md) a `docs/adr/NNN-titulo-corto.md`.
2. Numerar correlativo, sin reutilizar números.
3. Abrirlo en estado `PROPOSED` dentro del PR que lo necesita.
4. El PR **no se mergea** hasta que el ADR queda `ACCEPTED` o `REJECTED`.
5. Un ADR aceptado no se edita: si la decisión cambia, se escribe uno nuevo que lo
   marque `SUPERSEDED`.

## Regla para agentes

Un agente **no aprueba un ADR**. Si mientras ejecuta una task encuentra una
contradicción que cae en la lista de arriba, se detiene, deja el ADR en `PROPOSED`
con el contexto que reunió y escala. `AGENTS.md` §Escalamiento.

## Índice

| ADR | Título | Estado | Fecha |
|---|---|---|---|
| [001](001-mongodb-replica-set-nodo-unico.md) | Levantar MongoDB como replica set de un nodo | `ACCEPTED` | 2026-09-02 |
| [002](002-outbox-transaccional-coleccion-separada.md) | Outbox transaccional en colección separada | `ACCEPTED` | 2026-09-02 |
| [003](003-driver-mongodb-y-representacion-de-ids.md) | Driver MongoDB 3.11.1 exacta e IDs como GUID binario | `ACCEPTED` | 2026-09-02 |
| [004](004-topologia-rabbitmq.md) | Topología RabbitMQ: exchange por servicio publicador | `ACCEPTED` | 2026-09-02 |
| [005](005-keycloak-realm-unico-y-patron-bff.md) | Un realm de Keycloak y tokens retenidos en el BFF | `ACCEPTED` | 2026-09-02 |
| [006](006-contrato-de-error-publico.md) | Contrato de error público: Problem Details + código estable | `ACCEPTED` | 2026-09-02 |

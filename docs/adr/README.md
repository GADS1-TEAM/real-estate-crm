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
| — | Todavía no hay ADRs registrados. | — | — |

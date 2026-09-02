# ADR-NNN — Título corto en imperativo

| Campo | Valor |
|---|---|
| Estado | `PROPOSED` \| `ACCEPTED` \| `REJECTED` \| `SUPERSEDED` |
| Fecha | AAAA-MM-DD |
| Autor | |
| Task / PRP relacionado | |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

Cuál de los cambios de `ARCHITECTURE.md` §18 obliga a este ADR:

- [ ] ownership de aggregate/colección
- [ ] límites de microservicio
- [ ] datastore principal
- [ ] estrategia de consistencia cross-service
- [ ] contrato público incompatible
- [ ] semántica de evento público
- [ ] invariante core
- [ ] política global de tenancy/autorización

## Contexto

Qué situación concreta obliga a decidir. Hechos, no opiniones. Si el disparador
apareció mientras se ejecutaba una task, indicar cuál y qué la bloqueó.

Citar la regla vigente que se está por cambiar, con archivo y sección:

> `ARCHITECTURE.md` §N — "texto de la regla actual"

## Decisión

Qué se decide, en una frase. Después el detalle.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| | |
| | |

Descartar una alternativa sin motivo escrito equivale a no haberla considerado.

## Consecuencias

**Aceptamos:**

-

**Perdemos:**

-

**Deuda que queda abierta:**

-

## Impacto en el repositorio

- Documentos a actualizar: `README.md` / `ARCHITECTURE.md` / `AGENTS.md` / `docs/implementation/POC_TECH_DECISIONS.md` / skills.
- Tasks afectadas del `TASK_BOARD.md`:
- Contratos o eventos que cambian de versión:
- Servicios que deben migrar datos:

## Revisión

Bajo qué condición observable este ADR se revisa o se revierte. Si no hay
ninguna, decirlo explícitamente.

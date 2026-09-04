# Documentación del CRM Inmobiliarias

Este directorio complementa las dos fuentes principales del repositorio:

- [`../README.md`](../README.md) — modelo de dominio y lenguaje ubicuo.
- [`../ARCHITECTURE.md`](../ARCHITECTURE.md) — arquitectura de software y reglas de trazabilidad.

## Implementación

- [`implementation/IMPLEMENTATION_MASTER_PLAN.md`](implementation/IMPLEMENTATION_MASTER_PLAN.md) — plan maestro vigente para el recorte del TP, el orden de implementación y sus entregas.
- [`implementation/POC_TECH_DECISIONS.md`](implementation/POC_TECH_DECISIONS.md) — stack físico de la POC y decisiones diferidas.
- [`implementation/EXECUTION_STRATEGY.md`](implementation/EXECUTION_STRATEGY.md) — cómo paralelizar agentes sin pisarse.
- [`implementation/FIRST_BATCH.md`](implementation/FIRST_BATCH.md) — orden concreto para las primeras tandas.

La decisión de alcance y la representación de oportunidades están documentadas en [`adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md`](adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md).

## Backlog técnico

- [`tasks/v2/README.md`](tasks/v2/README.md) — backlog V2 vigente para este TP.
- [`tasks/v2/TASK_BOARD.md`](tasks/v2/TASK_BOARD.md) — 17 tasks priorizadas del alcance recortado.
- [`tasks/README.md`](tasks/README.md) — cómo consumir y ejecutar el backlog.
- [`tasks/TASK_BOARD.md`](tasks/TASK_BOARD.md) — 92 tasks / 274 casos de uso.
- [`tasks/TASK_TEMPLATE.md`](tasks/TASK_TEMPLATE.md) — contrato mínimo de una task.
- [`tasks/AGENT_TASK_PROMPT.md`](tasks/AGENT_TASK_PROMPT.md) — prompt base para ejecutar una task con un agente.
- [`tasks/VALIDATION_REPORT.md`](tasks/VALIDATION_REPORT.md) — cobertura por ola y checks estructurales.

`docs/tasks/v2/` es el **backlog vigente del TP**. `docs/tasks/` conserva el backlog amplio anterior como referencia histórica y no debe ejecutarse para este alcance. `PRPs/` es el mecanismo de ejecución cuando una task es tomada y necesita discovery/implementación detallada.

## Skills

- [`skills/SKILL_GAPS.md`](skills/SKILL_GAPS.md) — capacidades ya cubiertas y skills específicas que faltan para este proyecto.

Las skills ejecutables viven en [`.github/skills/`](../.github/skills/).

## Agentes

Las instrucciones operativas globales están en [`../AGENTS.md`](../AGENTS.md). Los agentes especializados viven en [`.github/agents/`](../.github/agents/).

## Orden de lectura recomendado

### Arquitectura / producto

1. `README.md`
2. `ARCHITECTURE.md`
3. `docs/implementation/POC_TECH_DECISIONS.md`

### Agente de implementación

1. `AGENTS.md`
2. task asignada
3. secciones relevantes de `README.md`
4. `ARCHITECTURE.md`
5. skills aplicables

No es necesario cargar todo el repositorio documental en contexto para implementar una task acotada.

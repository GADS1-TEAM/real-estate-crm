# Backlog ejecutable

`docs/tasks/` es la fuente de verdad para el backlog técnico del CRM. Contiene **92 work packages** que cubren **274 casos de uso de negocio**.

## Cómo usarlo

1. Elegir una task `READY` de [`TASK_BOARD.md`](TASK_BOARD.md).
2. Leer `AGENTS.md`, la task, las secciones relevantes de `README.md` y `ARCHITECTURE.md`, y `docs/implementation/POC_TECH_DECISIONS.md`.
3. Cargar las skills aplicables según [`../skills/SKILL_ROUTING.md`](../skills/SKILL_ROUTING.md). Sus MUST y MUST NOT son condición de aceptación.
4. Si el cambio no es trivial, usar el flujo de PRP existente: discovery → aprobación → implementación → tests → evaluación.
5. No ejecutar una task bloqueada por dependencias sin cerrar antes el contrato que necesita.
6. No ampliar scope silenciosamente: nuevos bounded contexts, contratos públicos o invariantes requieren decisión explícita.

## Estructura

- `wave-0-foundation/` — repositorio, CI, contratos, infraestructura, seguridad y observabilidad.
- `wave-1-core/` — organización, platform defaults, Party, Property y Demand mínimos.
- `wave-2-commercial-loop/` — supply, matching, interactions y analytics operativos.
- `wave-3-integrity-compliance/` — identity resolution, documents/compliance, visits y supply avanzado.
- `wave-4-negotiation-closing/` — negociación, reservas, transacciones, comisiones y compliance de cierre.
- `wave-5-operations-integrations/` — alquileres, canales, syndication e integraciones.
- `wave-6-automation-ai/` — automatización, IA y copilot/analytics.
- `wave-7-optional-expansion/` — maintenance, portals y platform backoffice avanzado.

## Contrato común

[`TASK_TEMPLATE.md`](TASK_TEMPLATE.md) define las reglas que toda task debe cumplir. Las tasks pueden ser más compactas o más detalladas según la ola, pero siempre deben conservar como mínimo: ID, dependencias, UCs, owner, resultado esperado, overrides POC y Definition of Done.

## Control

- [`VALIDATION_REPORT.md`](VALIDATION_REPORT.md) — cobertura y checks estructurales.
- [`AGENT_TASK_PROMPT.md`](AGENT_TASK_PROMPT.md) — prompt base para un agente de implementación.
- [`../skills/SKILL_ROUTING.md`](../skills/SKILL_ROUTING.md) — qué skills carga cada task.
- [`../implementation/EXECUTION_STRATEGY.md`](../implementation/EXECUTION_STRATEGY.md) — paralelización y write zones.
- [`../implementation/FIRST_BATCH.md`](../implementation/FIRST_BATCH.md) — primera secuencia concreta.

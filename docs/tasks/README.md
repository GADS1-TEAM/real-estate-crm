# Backlog ejecutable

> **Vigente para este TP:** usar [`v2/README.md`](v2/README.md) y [`v2/TASK_BOARD.md`](v2/TASK_BOARD.md). Este directorio raíz conserva el backlog amplio original como referencia histórica; sus 92 work packages no forman parte del alcance V2 y no deben ejecutarse para esta entrega.

`docs/tasks/` conserva la versión amplia anterior del backlog técnico del CRM:
contiene **92 work packages** que cubren **274 casos de uso de negocio**.

## Cómo usar el backlog histórico

1. Elegir una task `READY` de [`TASK_BOARD.md`](TASK_BOARD.md).
2. Leer `AGENTS.md`, la task, las secciones relevantes de `README.md` y `ARCHITECTURE.md`, y `docs/implementation/POC_TECH_DECISIONS.md`.
3. Si el cambio no es trivial, usar el flujo de PRP existente: discovery → aprobación → implementación → tests → evaluación.
4. No ejecutar una task bloqueada por dependencias sin cerrar antes el contrato que necesita.
5. No ampliar scope silenciosamente: nuevos bounded contexts, contratos públicos o invariantes requieren decisión explícita.

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
- [`../implementation/EXECUTION_STRATEGY.md`](../implementation/EXECUTION_STRATEGY.md) — paralelización y write zones.
- [`../implementation/FIRST_BATCH.md`](../implementation/FIRST_BATCH.md) — primera secuencia concreta.

# W2-SUP-01 — Abrir, asignar, priorizar y registrar actividad de una captación

**Dependencias:** `W1-PTY-01`, `W1-PRP-01`  
**Casos de uso:** `SUP-001`, `SUP-002`, `SUP-003`, `SUP-004`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `SUP-001` Abrir `CaptationCase` con datos mínimos; evento `CaptationCaseOpened`.
- `SUP-002` Asignar responsable según zona/especialidad/carga/relación/policy; evento `CaptationAssigned`.
- `SUP-003` Priorizar con score explicable usando interés, demanda compatible, valor potencial, timing y otros factores; evento `CaptationPriorityChanged`.
- `SUP-004` Vincular `Interaction` existente al CaptationCase sin duplicarla; evento `InteractionLinked`.

## Reglas

CaptationCase representa el proceso de conseguir oferta; no requiere que Property esté completa. Scoring debe explicar factores y datos faltantes.

## DoD
- [ ] Captación mínima usable.
- [ ] Asignación autorizada.
- [ ] Prioridad reproducible/explicable.
- [ ] Interaction se referencia, no se copia.

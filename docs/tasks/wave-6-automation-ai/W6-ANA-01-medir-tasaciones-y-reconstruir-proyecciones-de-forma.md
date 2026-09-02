# W6-ANA-01 — Medir tasaciones y reconstruir proyecciones de forma segura

- **Ola:** 6 — Automatización e inteligencia
- **Estado:** `TODO`
- **Dependencias:** `W3-SUP-01`, `W2-ANA-01`
- **UCs:** `ANA-006`, `ANA-014`
- **Owner:** `analytics-copilot-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `cqrs-read-models-projections`, `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Medir tasaciones y reconstruir proyecciones de forma segura.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

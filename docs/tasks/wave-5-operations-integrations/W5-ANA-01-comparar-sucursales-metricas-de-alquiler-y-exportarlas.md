# W5-ANA-01 — Comparar sucursales, métricas de alquiler y exportarlas

- **Ola:** 5 — Operación e integraciones
- **Estado:** `TODO`
- **Dependencias:** `W5-RNT-05`, `W2-ANA-01`
- **UCs:** `ANA-004`, `ANA-009`, `ANA-015`
- **Owner:** `analytics-copilot-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `cqrs-read-models-projections`, `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Comparar sucursales, métricas de alquiler y exportarlas.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

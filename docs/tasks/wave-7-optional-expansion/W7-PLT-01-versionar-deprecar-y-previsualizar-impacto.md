# W7-PLT-01 — Versionar/deprecar y previsualizar impacto

- **Ola:** 7 — Expansión opcional y backoffice avanzado
- **Estado:** `TODO`
- **Dependencias:** `W1-PLT-01`
- **UCs:** `PLT-002`, `PLT-003`, `PLT-019`
- **Owner:** `platform-config-service`, `analytics-copilot-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Versionar/deprecar y previsualizar impacto.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

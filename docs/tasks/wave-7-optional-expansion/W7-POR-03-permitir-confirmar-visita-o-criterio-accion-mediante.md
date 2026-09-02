# W7-POR-03 — Permitir confirmar visita o criterio/acción mediante link/portal

- **Ola:** 7 — Expansión opcional y backoffice avanzado
- **Estado:** `TODO`
- **Dependencias:** `W3-VIS-03`, `W2-DEM-02`
- **UCs:** `POR-009`, `POR-010`
- **Owner:** `scheduling-service`, `commercial-service`, `demand-service/commercial-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `aspnetcore-rest-layer`, `nextjs-frontend-architecture`, `crm-ux-quick-capture`, `mongodb-document-modeling`, `mongodb-dotnet-driver`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Permitir confirmar visita o criterio/acción mediante link/portal.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

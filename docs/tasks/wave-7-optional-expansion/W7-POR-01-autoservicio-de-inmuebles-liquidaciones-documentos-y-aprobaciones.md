# W7-POR-01 — Autoservicio de inmuebles, liquidaciones, documentos y aprobaciones

- **Ola:** 7 — Expansión opcional y backoffice avanzado
- **Estado:** `TODO`
- **Dependencias:** `W5-RNT-05`, `W7-MNT-02`
- **UCs:** `POR-001`, `POR-002`, `POR-003`, `POR-004`
- **Owner:** `analytics-copilot-service`, `rental-service`, `documents-compliance-service`, `maintenance-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `aspnetcore-rest-layer`, `nextjs-frontend-architecture`, `crm-ux-quick-capture`, `mongodb-document-modeling`, `mongodb-dotnet-driver`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Autoservicio de inmuebles, liquidaciones, documentos y aprobaciones.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

# W7-PLT-03 — Correcciones auditadas, auditoría y promoción entre ambientes

- **Ola:** 7 — Expansión opcional y backoffice avanzado
- **Estado:** `TODO`
- **Dependencias:** `W7-PLT-01`, `FND-006`
- **UCs:** `PLT-017`, `PLT-018`, `PLT-020`
- **Owner:** `service owner`, `audit-service`, `platform-config-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Correcciones auditadas, auditoría y promoción entre ambientes.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

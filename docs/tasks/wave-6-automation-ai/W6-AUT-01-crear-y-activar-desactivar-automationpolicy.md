# W6-AUT-01 — Crear y activar/desactivar AutomationPolicy

- **Ola:** 6 — Automatización e inteligencia
- **Estado:** `TODO`
- **Dependencias:** `W3-PLT-02`
- **UCs:** `AUT-001`, `AUT-002`
- **Owner:** `automation-ai-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`, `rabbitmq-dotnet`, `aspnetcore-messaging`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Crear y activar/desactivar AutomationPolicy.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

# W5-INT-01 — WhatsApp/email/Meta y envío desde CRM mediante adapters

- **Ola:** 5 — Operación e integraciones
- **Estado:** `TODO`
- **Dependencias:** `W2-INT-01`
- **UCs:** `INT-001`, `INT-002`, `INT-003`, `INT-004`
- **Owner:** `interaction-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `rabbitmq-dotnet`, `aspnetcore-messaging`, `aspnetcore-outgoing-http`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
WhatsApp/email/Meta y envío desde CRM mediante adapters.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## Override POC
- **Mobile está diferido en la POC.** Si el caso también admite CRM Web, implementar primero la variante web; la interacción exclusivamente móvil queda como adapter/UI futura.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

# W5-INT-03 — Vincular interacciones a Party/casos, reasignar y adjuntar archivos

- **Ola:** 5 — Operación e integraciones
- **Estado:** `TODO`
- **Dependencias:** `W5-INT-01`, `W1-PTY-01`
- **UCs:** `INT-010`, `INT-011`, `INT-012`, `INT-015`
- **Owner:** `interaction-service`, `access-service`, `asset-service`, `documents-compliance-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `rabbitmq-dotnet`, `aspnetcore-messaging`, `aspnetcore-outgoing-http`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Vincular interacciones a Party/casos, reasignar y adjuntar archivos.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## Override POC
- **Mobile está diferido en la POC.** Si el caso también admite CRM Web, implementar primero la variante web; la interacción exclusivamente móvil queda como adapter/UI futura.
- `asset-service` representa la capacidad técnica futura de procesamiento de archivos. **No se requiere desplegar un servicio independiente en la POC.**

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

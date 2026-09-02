# W6-AUT-03 — Sugerir, pedir aprobación o autoejecutar con guardrails

- **Ola:** 6 — Automatización e inteligencia
- **Estado:** `TODO`
- **Dependencias:** `W6-AUT-01`
- **UCs:** `AUT-005`, `AUT-006`, `AUT-007`
- **Owner:** `automation-ai-service`, `notification-service`, `target service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`, `rabbitmq-dotnet`, `aspnetcore-messaging`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Sugerir, pedir aprobación o autoejecutar con guardrails.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## Override POC
- **Mobile está diferido en la POC.** Si el caso también admite CRM Web, implementar primero la variante web; la interacción exclusivamente móvil queda como adapter/UI futura.

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

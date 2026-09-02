# W5-INT-02 — Iniciar/registrar llamada, grabar con consentimiento y transcribir

- **Ola:** 5 — Operación e integraciones
- **Estado:** `TODO`
- **Dependencias:** `W3-DOC-02`, `W2-INT-01`
- **UCs:** `INT-005`, `INT-006`, `INT-007`, `INT-008`
- **Owner:** `interaction-service`, `documents-compliance-service`, `asset-service`, `automation-ai-service`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `rabbitmq-dotnet`, `aspnetcore-messaging`, `aspnetcore-outgoing-http`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado
Iniciar/registrar llamada, grabar con consentimiento y transcribir.

## Ejecución
Aplicar `ARCHITECTURE.md`, `AGENTS.md`, `docs/tasks/TASK_TEMPLATE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. Implementar todos los UCs listados respetando ownership, tenant isolation, contratos versionados, Outbox/Inbox/idempotencia cuando aplique, observabilidad, seguridad, UX de captura mínima y tests.

## Override POC
- **Mobile está diferido en la POC.** Si el caso también admite CRM Web, implementar primero la variante web; la interacción exclusivamente móvil queda como adapter/UI futura.
- **Telefonía está diferida.** En la POC solo se conserva el contrato/puerto y mocks; no integrar proveedor VoIP ni APIs nativas.
- `asset-service` representa la capacidad técnica futura de procesamiento de archivos. **No se requiere desplegar un servicio independiente en la POC.**

## DoD
- [ ] UCs trazables y criterios del dominio cumplidos.
- [ ] Build/tests/contratos/documentación en verde y PR acotado a esta task.

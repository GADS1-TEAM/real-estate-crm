# W4-CLS-06 — Cerrar compraventa/locación, cancelar y consultar expediente integral

**Ola:** 4 — Negociación y cierre  
**Estado inicial:** `TODO`  
**Dependencias:** `W4-CLS-05`  
**Casos de uso:** `CLS-021`, `CLS-022`, `CLS-023`, `CLS-024`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado esperado

Cerrar compraventa/locación, cancelar y consultar expediente integral.

## Casos de uso incluidos

| ID | Caso | Actor | Qué debe resolver | Superficie | Servicio(s) | Entidades/objetos | Evento(s) |
|---|---|---|---|---|---|---|---|
| `CLS-021` | **Cerrar compraventa** | Usuario autorizado | Marcar Transaction cerrada con resultado, fecha y términos finales; dispara comisiones, analytics e historial. | CRM Web | closing-service | Transaction | TransactionClosed |
| `CLS-022` | **Cerrar locación** | Usuario autorizado | Finalizar Transaction de locación y, si se administra, crear ManagedAgreement. | CRM Web | closing-service, rental-service | Transaction, ManagedAgreement | TransactionClosed, ManagedAgreementCreated |
| `CLS-023` | **Cancelar operación** | Usuario autorizado | Cerrar Transaction sin éxito con motivo y estado final, preservando toda la historia previa. | CRM Web | closing-service | Transaction | TransactionCancelled |
| `CLS-024` | **Consultar expediente de operación** | Agente/Manager | Ver Parties, Property, negociación, reserva, tareas, documentos, compliance, comisiones y timeline en una vista compuesta. | CRM Web | analytics-copilot-service | Transaction read model | — |

## Ownership

- **Servicios propietarios:** `closing-service`, `rental-service`, `analytics-copilot-service`.
- El owner del aggregate/colección es el único que puede mutar sus datos.
- Integraciones entre bounded contexts mediante contratos, APIs, eventos o read models; nunca lectura directa de colecciones ajenas.

## Contexto obligatorio antes de implementar

1. Esta task.
2. `README.md` para lenguaje ubicuo e invariantes de los objetos involucrados.
3. `ARCHITECTURE.md` para boundaries, servicios y decisiones vigentes.
4. `docs/implementation/POC_TECH_DECISIONS.md`.
5. Contratos producidos por las dependencias directas.
6. `docs/tasks/TASK_TEMPLATE.md` y `AGENTS.md` para checklist, DoD y evidencia de PR.

## Reglas de implementación

- Captura mínima y progressive disclosure: no convertir enriquecimiento en campos obligatorios sin invariante real.
- `tenantId` obligatorio en datos tenant-scoped e aislamiento probado explícitamente.
- MongoDB por aggregate root; referencias por ID entre aggregates independientes.
- Outbox para eventos ligados a cambios de estado; consumers idempotentes/Inbox cuando haya side effects.
- Propagar actor, `tenantId`, `correlationId` y `causationId`.
- BFF compone experiencia; no contiene reglas de dominio.
- No introducir Redis, OpenSearch, Kubernetes, warehouse, mobile o telefonía salvo que esta task lo permita expresamente.
- La PR debe limitarse a esta task o justificar cualquier desviación.

## Definition of Done

Aplicar íntegramente el DoD, estrategia de pruebas, observabilidad, seguridad, documentación y evidencia de PR definidos en `docs/tasks/TASK_TEMPLATE.md`. Cada UC listado arriba debe quedar verificablemente cubierto.

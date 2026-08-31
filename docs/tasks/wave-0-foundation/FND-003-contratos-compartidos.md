# FND-003 — Definir contratos versionados, IDs, errores, paginación, metadata de actor/tenant y envelope de eventos

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-001`

## Resultado esperado

Definir la gramática contractual común para que los agentes puedan implementar servicios en paralelo sin compartir modelos de dominio.

## Entregables

- [ ] IDs tipados: `TenantId`, `ActorId`, aggregate IDs y `CorrelationId`.
- [ ] `ExecutionContext` y metadata de tenant/actor.
- [ ] Problem Details/errores públicos estables.
- [ ] Paginación y versionado de APIs.
- [ ] Envelope de eventos con `eventId`, `name`, `version`, `occurredAt`, `tenantId`, `actor`, `correlationId`, `causationId`, `aggregateId` y `payload`.
- [ ] Idempotency key y convenciones de contratos.
- [ ] OpenAPI/event schemas como código + ejemplos.

## Criterios de aceptación

- [ ] Existe una única convención de errores y metadata.
- [ ] Contratos serializables y versionables.
- [ ] Servicios no comparten clases de dominio; solo contratos.
- [ ] Contratos no transportan PII innecesaria.

## DoD

- [ ] contract tests base;
- [ ] ejemplos de compatibilidad/versionado;
- [ ] documentación de breaking vs non-breaking changes.

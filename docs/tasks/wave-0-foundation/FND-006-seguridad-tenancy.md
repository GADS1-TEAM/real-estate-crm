# FND-006 — Seguridad, tenancy y contexto de ejecución

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-003`, `FND-004`

## Resultado esperado

Fijar `tenantId`, actor context, OIDC adapter, authorization port, audit envelope y propagación de `correlationId`.

## Entregables

- [ ] Derivar `tenantId`, actor y correlationId desde token/request confiable.
- [ ] Crear authorization port hacia `access-service`.
- [ ] Agregar mecanismo obligatorio para filtros tenant-scoped en repositorios Mongo.
- [ ] Definir audit envelope para comandos sensibles.
- [ ] Definir propagación service-to-service del contexto mínimo.

## Criterios de aceptación

- [ ] No existe query tenant-scoped sin tenant.
- [ ] Un tenant no puede leer/escribir datos de otro en tests de integración.
- [ ] Actor/correlationId llegan a eventos y auditoría.
- [ ] `tenantId` enviado por body/query no puede elevar acceso.

## DoD

- [ ] tests de aislamiento;
- [ ] tests de BOLA/IDOR básicos;
- [ ] documentación de claims vs autorización de negocio.

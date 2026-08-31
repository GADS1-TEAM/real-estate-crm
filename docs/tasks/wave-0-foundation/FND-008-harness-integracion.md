# FND-008 — Harness de integración y contract testing

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-002`, `FND-003`, `FND-004`

## Resultado esperado

Crear Testcontainers/fixtures, consumer-driven contracts y pruebas de eventos/APIs para que los vertical slices puedan integrarse sin infraestructura externa.

## Entregables

- [ ] Testcontainers para MongoDB y RabbitMQ.
- [ ] Fixture de Keycloak o auth fake contractual para tests aislados.
- [ ] Harness de APIs/eventos.
- [ ] Consumer contract tests.
- [ ] Helpers para seed de tenant/actor y cleanup aislado.

## Criterios de aceptación

- [ ] Tests de integración corren local/CI sin infraestructura externa preexistente.
- [ ] Un contrato incompatible rompe CI.
- [ ] Los tests pueden ejecutarse por slice de manera independiente.
- [ ] Existe una forma reproducible de probar aislamiento de tenants.

## DoD

- [ ] ejemplos mínimos de API + evento contract test;
- [ ] cleanup determinístico;
- [ ] documentación de cómo ejecutar el harness.

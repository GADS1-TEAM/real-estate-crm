# FND-007 — Observabilidad y resiliencia base

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-004`

## Resultado esperado

OpenTelemetry, logs estructurados, métricas RED/USE, health/readiness, retry policy e idempotency helpers sin montar un stack operativo pesado.

## Entregables

- [ ] OpenTelemetry en .NET y BFFs.
- [ ] Logs estructurados con correlationId/tenantId sin PII sensible.
- [ ] Health/readiness endpoints.
- [ ] Helpers de retry con criterios de idempotencia.
- [ ] Métricas mínimas RED/USE exportables.

## Criterios de aceptación

- [ ] Una request puede seguirse por correlationId entre BFF y servicio.
- [ ] Healthchecks distinguen alive/ready.
- [ ] Retries no duplican side effects en tests.
- [ ] PII/secrets no aparecen en logs de escenarios testeados.

## DoD

- [ ] tests de logging/correlation cuando sean razonables;
- [ ] healthchecks documentados;
- [ ] sin dependencia de un SaaS de observabilidad.

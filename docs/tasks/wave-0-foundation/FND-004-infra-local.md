# FND-004 — Levantar la plataforma POC mínima con Docker Compose

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-001`

## Resultado esperado

Levantar la infraestructura gratuita y mínima necesaria para desarrollar y probar los slices sin cuentas cloud ni configuración pesada.

## Entregables

- [ ] `docker-compose.yml` con MongoDB Community.
- [ ] RabbitMQ Community + management UI.
- [ ] Keycloak Community con configuración mínima reproducible.
- [ ] Healthchecks y volúmenes locales.
- [ ] `IObjectStorage` con adapter filesystem local.
- [ ] Configuración por variables de entorno sin secretos commiteados.

## Criterios de aceptación

- [ ] `docker compose up` levanta todo desde máquina limpia.
- [ ] MongoDB, RabbitMQ y Keycloak tienen healthcheck funcional.
- [ ] Bajar y recrear containers no requiere setup manual relevante.
- [ ] No se agrega Redis, OpenSearch, Kafka, Kubernetes ni servicio pago.

## DoD

- [ ] instrucciones start/stop/reset;
- [ ] configuración development reproducible;
- [ ] secretos de desarrollo claramente ficticios/locales.

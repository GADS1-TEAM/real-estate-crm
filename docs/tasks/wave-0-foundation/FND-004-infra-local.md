# FND-004 — Levantar la plataforma POC mínima con Docker Compose

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-001`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `aspnetcore-config-and-secrets`, `rabbitmq-dotnet`, `oidc-keycloak-aspnetcore`, `mongodb-dotnet-driver`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado esperado

Levantar la infraestructura gratuita y mínima necesaria para desarrollar y probar los slices sin cuentas cloud ni configuración pesada.

## Entregables

- [ ] `docker-compose.yml` con MongoDB Community **8.x, como replica set de un nodo** (ver restricción abajo).
- [ ] RabbitMQ Community + management UI.
- [ ] Keycloak Community con configuración mínima reproducible.
- [ ] Healthchecks y volúmenes locales.
- [ ] `IObjectStorage` con adapter filesystem local.
- [ ] Configuración por variables de entorno sin secretos commiteados.

## Restricción: MongoDB debe ser replica set

Las **transacciones multi-documento y los change streams de MongoDB solo funcionan sobre replica set o sharded cluster**; un `mongod` standalone no los soporta.

El patrón de outbox transaccional del proyecto escribe el aggregate y sus eventos en una misma transacción, así que un standalone no alcanza. Se levanta como replica set de un solo nodo: es la configuración mínima que habilita ambas cosas y no agrega servicios ni costo.

```yaml
mongo:
  image: mongo:8
  command: ["--replSet", "rs0", "--bind_ip_all"]
```

El nodo debe quedar iniciado (`rs.initiate()`) de forma automática y reproducible, sin pasos manuales tras `docker compose up`.

Además, fijar el tag de la imagen: el driver .NET 3.10+ dejó de soportar MongoDB Server 4.2 y anteriores. No usar `mongo:latest`.

Ver [`event-driven-outbox-inbox`](../../../.github/skills/event-driven-outbox-inbox/SKILL.md) y [`mongodb-dotnet-driver`](../../../.github/skills/mongodb-dotnet-driver/SKILL.md).

## Criterios de aceptación

- [ ] `docker compose up` levanta todo desde máquina limpia.
- [ ] MongoDB, RabbitMQ y Keycloak tienen healthcheck funcional.
- [ ] MongoDB queda como replica set iniciado sin intervención manual, y una transacción multi-documento de prueba commitea correctamente.
- [ ] Bajar y recrear containers no requiere setup manual relevante.
- [ ] No se agrega Redis, OpenSearch, Kafka, Kubernetes ni servicio pago.

## DoD

- [ ] instrucciones start/stop/reset;
- [ ] configuración development reproducible;
- [ ] secretos de desarrollo claramente ficticios/locales.

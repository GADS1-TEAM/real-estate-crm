# Implementation report — V2-FND-003 — Infraestructura local, CI y observabilidad

## UCs cubiertos

- **OPS-001** — `docker-compose.yml` levanta `mongo` (replica set de un nodo `rs0`),
  `rabbitmq` (management) y `keycloak` (realm `crm-dev` importado como código) con un solo
  `docker compose up -d`. Sin volúmenes persistentes: infra POC efímera, se recrea limpia en
  cada `up`.
- **OPS-002** — `.github/workflows/ci.yml`: job `fast` (build + tests sin infra + build de
  `crm-web`/`platform-admin-web`) y job `integration` (compose up + los 3 `Category` reales).
  Ambos jobs corren `scripts/test-fast.sh`/`scripts/test-integration.sh`, los mismos scripts
  que se corren a mano — es la forma en que "CI local = CI remoto".
- **OPS-003** — `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/HealthChecks/`:
  extensión reutilizable `AddCrmHealthChecks()` + `AddMongoReadinessCheck()`/
  `AddRabbitMqReadinessCheck()` + `MapCrmHealthEndpoints()` (`/health/live` no evalúa ninguna
  dependencia — vivo = el proceso responde —, `/health/ready` evalúa los checks con el tag
  `ready`). Probado con TestServer, incluyendo contra Mongo/RabbitMQ reales.
- **OPS-004** — `Observability/CorrelationIdMiddleware` toma o genera un correlationId por
  request, lo agrega como tag de `Activity.Current` (traza OpenTelemetry) y como scope de
  `ILogger`, y lo devuelve en la respuesta. `CorrelationIdEventPropagationTests` (contra
  RabbitMQ/Mongo reales) prueba que el mismo correlationId aparece logueado tanto al publicar
  como al consumir un evento. **No es la demostración real "BFF → consumer"** que pide la
  task: ver "Alcance pendiente" abajo.
- **OPS-005** — `Observability/SensitiveDataRedactor` + `SanitizingLoggerProvider`: todo log
  estructurado pasa por redacción de `password`/`token`/`secret`/`documento`/`dni`/`cuit`/
  texto de actividad antes de escribirse, sin tocar correlationId/actorId. Cubierto con tests
  adversariales (mayúsculas mezcladas, JSON embebido, valores vacíos/nulos/larguísimos,
  unicode) y con un test de `ILogger` real capturando output.

## Archivos

Creados (write zone declarada: `docker-compose*`, `infra/`, `.github/workflows/`, config de
observabilidad, scripts de test):

- `docker-compose.yml`: servicios `mongo` (7.0.43), `rabbitmq` (4.3.6-management), `keycloak`
  (26.7.4).
- `.env.example`: variables documentadas, sin secretos reales.
- `infra/keycloak/realm-export/crm-dev-realm.json`: realm `crm-dev`, client `operations-bff`,
  roles Administrador/Vendedor/Responsable Comercial, usuario `dev.vendedor`.
- `scripts/test-fast.{sh,ps1}`, `scripts/test-integration.{sh,ps1}`.
- `.github/workflows/ci.yml`: jobs `fast` e `integration`.
- `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/HealthChecks/` (5 archivos):
  `MongoHealthCheck`, `RabbitMqHealthCheck`, `CrmHealthCheckTags`,
  `CrmHealthChecksServiceCollectionExtensions`, `CrmHealthCheckEndpointRouteBuilderExtensions`.
- `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/Observability/` (4 archivos):
  `SensitiveDataRedactor`, `CorrelationIdMiddleware`, `SanitizingLoggerProvider`,
  `CrmObservabilityServiceCollectionExtensions`.
- `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests/HealthChecks/` (3 archivos):
  `CrmHealthEndpointsTests` (sin infra), `MongoReadinessCheckTests` (`RequiresMongo`),
  `RabbitMqReadinessCheckTests` (`RequiresRabbitMq`).
- `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests/Observability/` (4 archivos):
  `SensitiveDataRedactorTests`, `SanitizingLoggerProviderTests`, `CorrelationIdMiddlewareTests`
  (sin infra), `CorrelationIdEventPropagationTests` (`RequiresRabbitMq`).

Modificados:

- `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/RealEstateCrm.BuildingBlocks.Infrastructure.csproj`:
  3 `PackageReference` nuevas (OpenTelemetry, ver "Decisiones locales").
- `DEVELOPMENT.md`: sección de infraestructura local, comandos de los scripts nuevos,
  estructura de solución actualizada. No se tocó nada fuera de eso.
- `.gitignore`: agregado `.env` (nunca commitear un `.env` real, aunque sea todo ficticio).

No se modificó ningún `Program.cs` de `services/`/`bffs/`, ni `.github/skills`, ni
`.github/agents`, ni código de `apps/crm-web`/`apps/platform-admin-web`, ni ningún archivo de
la lista prohibida (`README.md`, `ARCHITECTURE.md`, `AGENTS.md`, `docs/adr/`,
`docs/implementation/`, `docs/tasks/wave-*`, `design_handoff_*`).

## Comandos y resultados

Todo lo de abajo se corrió contra infraestructura real (Docker), no mockeada.

### Verificación de tags exactos contra los registries reales

```text
$ docker manifest inspect mongo:7.0.43                       → OK (Docker Hub, library/mongo)
$ docker manifest inspect rabbitmq:4.3.6-management           → OK (Docker Hub, library/rabbitmq)
$ docker manifest inspect quay.io/keycloak/keycloak:26.7.4    → OK (quay.io, fuente oficial)
```

`mongo:7.0.43` y `rabbitmq:4.3.6-management` son los últimos patch de sus líneas (7.0.x y
4.3.x respectivamente) publicados en Docker Hub al 2026-09-16, confirmados con la API de
Docker Hub (`hub.docker.com/v2/repositories/library/{mongo,rabbitmq}/tags`).
`quay.io/keycloak/keycloak:26.7.4` es el último patch de la línea 26.x (26.7.0 publicado
2026-07-09), confirmado directo contra `quay.io`.

### Mongo: replica set + acceso desde host (probado antes de decidir la connection string)

```text
$ docker run mongo:7.0.43 --replSet rs0 --bind_ip_all (vía compose, healthcheck idempotente)
$ MongoClient con distintas connection strings, mismo contenedor:
  mongodb://localhost:27017                                    → FAILED (timeout: SDAM descubre
                                                                    el miembro como "mongo:27017",
                                                                    "mongo" no resuelve en el host)
  mongodb://localhost:27017/?replicaSet=rs0                     → FAILED (mismo motivo)
  mongodb://localhost:27017/?directConnection=true               → OK (insert simple + transacción
                                                                    multi-documento)
  mongodb://localhost:27017/?replicaSet=rs0&directConnection=true → OK (insert simple + transacción
                                                                    multi-documento)
```

Confirmado con un proyecto de consola descartable referenciando `MongoDB.Driver 3.11.2` (la
misma versión fijada en `V2-FND-002`) contra el contenedor real. Por eso `.env.example` fija
`directConnection=true` en ambas connection strings, con el miembro del replica set anunciado
como `mongo:27017` (DNS de Compose) para que un futuro servicio *dentro* de la red de Compose
no necesite ese parámetro.

### Build y tests sin infraestructura (job `fast`)

```text
$ bash scripts/test-fast.sh
==> dotnet build         → Compilación correcta. 0 Advertencias, 0 Errores.
==> dotnet test --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
    RealEstateCrm.ArchitectureTests.dll                   → Superado: 3,  Total: 3
    RealEstateCrm.ContractTests.dll                       → Superado: 21, Total: 21
    RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 48, Total: 48
==> apps/scripts/build-webs.sh
    crm-web: next build              → ✓ Compiled successfully, 6/6 páginas generadas
    platform-admin-web: next build   → ✓ Compiled successfully, 6/6 páginas generadas
```

### Job de integración, contra Docker real (`scripts/test-integration.sh`)

```text
$ bash scripts/test-integration.sh
==> docker compose up -d
==> esperando healthchecks
  crm-mongo    healthy (intento 3, ~15s)
  crm-rabbitmq healthy (intento 1, ~5s)
  crm-keycloak healthy (intento 6, ~30s)
==> dotnet test --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak"
    RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 11, Total: 11
==> docker compose down -v
```

Los 11: `MongoInboxIdempotencyTests` (2) + `MongoOutboxTransactionTests` (2, de V2-FND-002) +
`MongoReadinessCheckTests` (2) + `RabbitMqPublishConsumeTests` (1, de V2-FND-002) +
`RabbitMqReadinessCheckTests` (2) + `CorrelationIdEventPropagationTests` (1) +
`KeycloakDevRealmTests` (1, de V2-FND-002 — corre por primera vez contra Keycloak real: el
realm local todavía no existía cuando se escribió).

### Keycloak: password grant real contra el realm importado

```text
$ curl -X POST http://localhost:8080/realms/crm-dev/protocol/openid-connect/token \
    -d grant_type=password -d client_id=operations-bff \
    -d username=dev.vendedor -d password=dev.vendedor
→ HTTP 200, access_token presente (mismo flujo que KeycloakDevRealmTests)
```

## Decisiones locales

1. **Versiones exactas de imágenes** (decisión del equipo — sección 5 del plan, ver
   respuestas del humano): `mongo:7.0.43`, `rabbitmq:4.3.6-management`,
   `quay.io/keycloak/keycloak:26.7.4`. Verificadas contra los registries reales (ver arriba),
   no adivinadas.
2. **Connection string de Mongo con `directConnection=true`** (decisión del equipo, ajuste
   pedido explícitamente): el miembro del replica set se anuncia como `mongo:27017` (DNS de
   Compose, para que un futuro consumidor dentro de la red de Compose no necesite el
   parámetro); un cliente que corre en el host (dotnet test local o el runner de CI) necesita
   `directConnection=true` para no intentar la resolución de topología completa contra ese
   hostname. Probado en rojo (sin el parámetro) y en verde (con él) antes de fijarlo.
3. **Job rápido de CI no levanta Compose** (decisión del equipo, ajuste pedido
   explícitamente): `scripts/test-fast.sh` nunca invoca `docker compose`; solo
   `scripts/test-integration.sh` lo hace. Los dos scripts (bash + PowerShell) son la única
   fuente de verdad de ambos jobs, corribles a mano.
4. **Client `operations-bff` público, con Direct Access Grants habilitado solo para dev/test**
   (decisión del equipo, ajuste pedido explícitamente): es el mismo `clientId` que ya usa
   `KeycloakDevRealmTests` (escrito en V2-FND-002, no modificado acá), así que el realm lo
   configura como cliente público con `standardFlowEnabled=true` (login real futuro del BFF
   vía cookie + PKCE, sin client secret — coincide con
   `AddKeycloakOpenIdConnectCookieAuthentication`, que tampoco configura uno) y
   `directAccessGrantsEnabled=true` (password grant). El password grant es exclusivamente
   para pruebas de desarrollo: el login real de las webs sigue yendo por el BFF con cookie de
   sesión (AUTH-001, V2-FND-002), nunca por password grant directo desde un front. Documentado
   en `.env.example` y en `infra/keycloak/realm-export/crm-dev-realm.json`.
5. **Healthcheck de Mongo idempotente**: `rs.status().ok`; solo si tira excepción (replica set
   sin inicializar) corre `rs.initiate`. Evita el error "already initialized" en reinicios del
   healthcheck.
6. **Healthcheck de Keycloak sin curl/wget**: la imagen oficial no los trae. Se usa el truco de
   `/dev/tcp` de bash contra el puerto de management (9000, habilitado con
   `KC_HEALTH_ENABLED=true`) para pegarle a `/health/ready`.
7. **`KC_BOOTSTRAP_ADMIN_USERNAME`/`KC_BOOTSTRAP_ADMIN_PASSWORD`** en vez de las variables
   `KEYCLOAK_ADMIN`/`KEYCLOAK_ADMIN_PASSWORD` (deprecadas desde Keycloak 26). Credenciales
   ficticias (`admin`/`admin-dev-only`), solo para la consola de administración en desarrollo.
8. **Versiones de OpenTelemetry** (no está en la lista de decisiones vedadas de la sección 5,
   que solo cubre Mongo/RabbitMQ/Keycloak): `OpenTelemetry.Extensions.Hosting`,
   `OpenTelemetry.Instrumentation.AspNetCore` y `OpenTelemetry.Exporter.Console`, todas
   `1.18.0` (últimas estables en NuGet al 2026-09-16). Exporter de consola: la POC no requiere
   collector ni dashboard externo (override explícito de la task).
9. **Redacción por nombre de clave, no por contenido libre**: `SensitiveDataRedactor` busca
   pares `clave: valor`/`clave=valor` cuya clave sea una de una lista conocida
   (password/token/secret/documento/dni/cuit/texto de actividad). Un valor sensible sin esa
   clave al lado no se detecta. Es una limitación deliberada (evitar falsos positivos de
   escaneo de contenido libre) documentada como supuesto/follow-up.

## Supuestos

- No hay ningún `Program.cs` de `services/`/`bffs/` wireado (siguen en `Hello World!` desde
  `V2-FND-001`): registrar `AddCrmHealthChecks`/`MapCrmHealthEndpoints`/`AddCrmObservability`/
  `UseCrmCorrelationId` ahí está fuera de la write zone de esta task (instrucción explícita del
  humano antes de tocar esos archivos). Todo lo de `HealthChecks/` y `Observability/` está
  escrito, compilado y testeado (incluso contra Mongo/RabbitMQ reales) pero nadie lo invoca
  todavía en un proceso real.
- Ningún aggregate real existe aún (son de wave 2+), así que `SensitiveDataRedactor` no tiene
  ningún campo de dominio real (`Interaction.text`, etc.) contra el cual probarse: los tests
  usan nombres de campo genéricos (`activityText`, `documento`) como aproximación de lo que
  va a loguearse eventualmente.
- Node de la PC es v24.16.0 (el plan pide "Node 22+"); mismo supuesto ya documentado en
  V2-FND-001/002. No se tocó nada de `apps/`.

## Alcance pendiente (aceptado como follow-up, no bloquea esta task)

El criterio de aceptación "un request de prueba conserva correlationId desde BFF hasta
consumer" (OPS-004) **no se demuestra end-to-end real** en esta task: no hay ningún slice ni
`Program.cs` en la write zone de V2-FND-003 que reciba un HTTP request real del BFF y dispare
un comando → evento → consumer. Lo que sí queda probado, contra infraestructura real:

- `CorrelationIdMiddleware` genera/propaga el correlationId por request HTTP y lo deja en
  scope de `ILogger` + tag de `Activity` (`CorrelationIdMiddlewareTests`, TestServer, sin
  infra externa).
- El correlationId de un `EventEnvelopeV1` sobrevive publish (outbox) → RabbitMQ → consume, y
  aparece logueado (redactado de datos sensibles) en ambos extremos
  (`CorrelationIdEventPropagationTests`, contra Mongo/RabbitMQ reales).

Lo que falta es unir ambas puntas con un HTTP request real disparando el publish. **Cierra
V2-ACL-001**, la primera task que va a tener un `Program.cs` real con un endpoint HTTP que
dispare un comando y publique un evento — ahí corresponde invocar
`AddCrmHealthChecks`/`AddCrmObservability`/`UseCrmCorrelationId` por primera vez y agregar el
test end-to-end real BFF → consumer.

## Follow-ups

- V2-ACL-001 (o la primera task que agregue un endpoint HTTP real): wirear
  `AddCrmHealthChecks().AddMongoReadinessCheck().AddRabbitMqReadinessCheck()`,
  `MapCrmHealthEndpoints()`, `AddCrmObservability(serviceName)` y `UseCrmCorrelationId()` en su
  `Program.cs`, y agregar el test end-to-end real de correlationId BFF → consumer que esta
  task no pudo escribir (ver "Alcance pendiente").
- Cuando exista un aggregate/servicio real logueando datos de dominio, revisar si
  `SensitiveDataRedactor` necesita nombres de clave adicionales específicos del dominio (más
  allá de los genéricos agregados acá).
- `RabbitMqHealthCheck`/`MongoHealthCheck` no cachean resultado: cada llamada a `/health/ready`
  abre una conexión/canal nuevo. Si en una wave futura esto genera carga medible, agregar
  cache corto (no es necesario en la POC).

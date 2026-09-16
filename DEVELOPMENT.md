# Desarrollo — CRM Inmobiliario V2

Comandos locales para el esqueleto creado en `V2-FND-001`. No reemplaza a
`README.md` ni a `ARCHITECTURE.md`: es la referencia operativa mínima para
compilar y testear lo que existe hoy en el repo.

## Requisitos

- .NET 10 SDK (`dotnet --version` → 10.x)
- Node 22+ y npm (`node -v`)
- Docker Desktop (para `docker compose up`, infraestructura de `V2-FND-003`)

## Infraestructura local (V2-FND-003)

```bash
docker compose up -d      # Mongo (replica set rs0), RabbitMQ, Keycloak (realm crm-dev)
docker compose ps         # confirmar healthy
docker compose down -v    # bajar todo (no hay volúmenes persistentes: es infra POC efímera)
```

- Mongo: un solo contenedor `mongo:7.0.43` como replica set de un nodo (`rs0`), requerido por
  el outbox transaccional de `V2-FND-002`. Healthcheck idempotente (`rs.status()`, y solo si
  falla corre `rs.initiate`).
- RabbitMQ: `rabbitmq:4.3.6-management`, UI en http://localhost:15672 (guest/guest).
- Keycloak: `quay.io/keycloak/keycloak:26.7.4`, realm `crm-dev` importado desde
  `infra/keycloak/realm-export/crm-dev-realm.json` (client `operations-bff`, usuario
  `dev.vendedor`/`dev.vendedor`, roles Administrador/Vendedor/Responsable Comercial como
  identidad — los permisos los resuelve `access-service`, V2-ACL-001).
- Variables y el porqué de `directConnection=true` en las connection strings de Mongo:
  ver `.env.example`.

## Backend (.NET)

```bash
# Compilar toda la solución
dotnet build RealEstateCrm.slnx

# Correr los tests SIN infraestructura externa (comando por defecto: no necesita
# Docker/Mongo/RabbitMQ/Keycloak corriendo, y debe dar 100% verde)
dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
```

Algunos tests de `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests` (agregados en
`V2-FND-002`, ampliados en `V2-FND-003`) corren contra Mongo/RabbitMQ/Keycloak reales en vez
de mockearlos, y están marcados con exactamente uno de estos `[Trait("Category", "...")]`:
`RequiresMongo`, `RequiresRabbitMq`, `RequiresKeycloak`. Si no se excluyen con el filtro de
arriba, van a fallar (timeout de conexión) en cualquier PC sin esa infraestructura levantada.

Dos scripts (`scripts/test-fast.{sh,ps1}` y `scripts/test-integration.{sh,ps1}`) son la fuente
única de verdad: los mismos que corre `.github/workflows/ci.yml` se pueden correr a mano.

```bash
# Job rápido: build + tests sin infra + build de ambas webs (no levanta Compose)
bash scripts/test-fast.sh

# Job de integración: docker compose up + espera healthchecks + los 3 Category reales + compose down
bash scripts/test-integration.sh
```

En PowerShell, usar `scripts/test-fast.ps1` y `scripts/test-integration.ps1`.

## Estructura de la solución

- `services/<nombre>-service/src/` — 11 servicios de dominio, cada uno con
  capas `Domain` → `Application` → `Infrastructure` → `Api` (hexagonal;
  `Domain` no depende de ningún otro proyecto ni paquete de infraestructura).
- `bffs/<nombre>-bff/src/` — `operations-bff` y `platform-admin-bff`, cada uno
  con `Application` + `Api`.
- `contracts/RealEstateCrm.Contracts/` — contratos compartidos entre servicios
  (`ProblemDetailsV1`, `PageV1<T>`, `ExecutionContextV1`, `EventEnvelopeV1<T>`; `V2-FND-002`).
- `building-blocks/RealEstateCrm.BuildingBlocks/` — puertos reutilizables
  (`IAuthenticationPort`, `IRepository<T,TId>`, `IUnitOfWork`, `IOutbox`, `IInbox`,
  `IEventPublisher`, `IEventConsumer<T>`), sin ningún paquete de infraestructura.
- `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/` — adapters reales de esos
  puertos (Mongo, RabbitMQ, Keycloak/JwtBearer; `V2-FND-002`) más `HealthChecks/`
  (`/health/live`, `/health/ready`) y `Observability/` (correlationId/actorId, logs con
  redacción de datos sensibles, OpenTelemetry; `V2-FND-003`). Solo la capa `Infrastructure`
  de cada servicio puede referenciarlo: `Application` no debe, y el test de arquitectura lo
  verifica. Ningún `Program.cs` de `services/`/`bffs/` invoca todavía `AddCrmHealthChecks` ni
  `AddCrmObservability` (fuera de la write zone de V2-FND-003, ver
  IMPLEMENTATION_REPORT-V2-FND-003.md).
- `infra/keycloak/realm-export/` — realm `crm-dev` versionado como JSON (`V2-FND-003`).
- `scripts/` — `test-fast`/`test-integration` en bash y PowerShell (`V2-FND-003`), usados
  tanto por `.github/workflows/ci.yml` como a mano.
- `tests/RealEstateCrm.ArchitectureTests/` — reglas de dependencia: falla si
  un proyecto `Domain` referencia MongoDB/RabbitMQ/Keycloak/ASP.NET Core, si
  un servicio referencia proyectos de otro servicio, o si un `Application`
  referencia `RealEstateCrm.BuildingBlocks.Infrastructure` directamente.
- `tests/RealEstateCrm.TestSupport/` — test doubles (`FakeAuthenticationPort`), solo para uso
  de otros proyectos de test.

Ningún servicio de dominio (`services/*`) se conecta todavía a MongoDB, RabbitMQ ni Keycloak:
`V2-FND-002` deja los ports/adapters listos en `building-blocks/`, pero ningún aggregate real
los usa aún (son de wave 2+). `V2-FND-003` agrega el Compose con la infraestructura real y los
building blocks de healthchecks/observabilidad, también sin wire-up en ningún `Program.cs`.

## Frontend (apps/)

`apps/crm-web` y `apps/platform-admin-web` ya existen (`V2-UX-001`). Ver
`apps/README.md` para el comando único que instala y construye ambas, o
`apps/crm-web/README.md` / `apps/platform-admin-web/README.md` para comandos
específicos de cada app.

```bash
bash apps/scripts/build-webs.sh
```

## Convenciones

- Target framework: `net10.0` (fijado en `Directory.Build.props`).
- Un servicio no referencia proyectos de otro servicio (verificado por el
  test de arquitectura).
- Sin `tenantId` ni `organizationId` en ningún proyecto de este esqueleto.

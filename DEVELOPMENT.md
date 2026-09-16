# Desarrollo — CRM Inmobiliario V2

Comandos locales para el esqueleto creado en `V2-FND-001`. No reemplaza a
`README.md` ni a `ARCHITECTURE.md`: es la referencia operativa mínima para
compilar y testear lo que existe hoy en el repo.

## Requisitos

- .NET 10 SDK (`dotnet --version` → 10.x)
- Node 22+ y npm (`node -v`)
- Docker Desktop (para infraestructura de `V2-FND-003`, todavía no agregada)

## Backend (.NET)

```bash
# Compilar toda la solución
dotnet build RealEstateCrm.slnx

# Correr los tests SIN infraestructura externa (comando por defecto: no necesita
# Docker/Mongo/RabbitMQ/Keycloak corriendo, y debe dar 100% verde)
dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
```

Algunos tests de `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests` (agregados en
`V2-FND-002`) corren contra Mongo/RabbitMQ/Keycloak reales en vez de mockearlos, y están
marcados con exactamente uno de estos `[Trait("Category", "...")]`: `RequiresMongo`,
`RequiresRabbitMq`, `RequiresKeycloak`. Si no se excluyen con el filtro de arriba, van a
fallar (timeout de conexión) en cualquier PC sin esa infraestructura levantada. Para correrlos
con infraestructura real (contenedores efímeros, no requiere el Compose de `V2-FND-003`):

```bash
docker run -d --rm --name mongo-standalone -p 27017:27017 mongo:7
docker run -d --rm --name mongo-replset -p 27018:27018 mongo:7 mongod --replSet rs0 --port 27018 --bind_ip_all
docker exec mongo-replset mongosh --port 27018 --eval 'rs.initiate({_id:"rs0", members:[{_id:0, host:"localhost:27018"}]})'
docker run -d --rm --name rabbitmq -p 5672:5672 rabbitmq:4-management

MONGO_CONNECTION_STRING=mongodb://localhost:27017 \
MONGO_REPLICA_SET_CONNECTION_STRING="mongodb://localhost:27018/?replicaSet=rs0" \
RABBITMQ_HOST=localhost \
dotnet test RealEstateCrm.slnx --filter "Category!=RequiresKeycloak"

docker rm -f mongo-standalone mongo-replset rabbitmq
```

`RequiresKeycloak` necesita además el realm local de desarrollo de `V2-FND-003`, que todavía
no existe (variables de entorno documentadas en `KeycloakDevRealmTests`).

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
  puertos (Mongo, RabbitMQ, Keycloak/JwtBearer; `V2-FND-002`). Solo la capa `Infrastructure`
  de cada servicio puede referenciarlo: `Application` no debe, y el test de arquitectura lo
  verifica.
- `tests/RealEstateCrm.ArchitectureTests/` — reglas de dependencia: falla si
  un proyecto `Domain` referencia MongoDB/RabbitMQ/Keycloak/ASP.NET Core, si
  un servicio referencia proyectos de otro servicio, o si un `Application`
  referencia `RealEstateCrm.BuildingBlocks.Infrastructure` directamente.
- `tests/RealEstateCrm.TestSupport/` — test doubles (`FakeAuthenticationPort`), solo para uso
  de otros proyectos de test.

Ningún servicio de dominio (`services/*`) se conecta todavía a MongoDB, RabbitMQ ni Keycloak:
`V2-FND-002` deja los ports/adapters listos en `building-blocks/`, pero ningún aggregate real
los usa aún (son de wave 2+). `V2-FND-003` agrega el Compose con la infraestructura real.

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

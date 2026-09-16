# Implementation report — V2-FND-002 — Contratos, autenticación OIDC y persistencia por ports

## UCs cubiertos

- **CONT-001** — `ProblemDetailsV1`, `PageV1<T>` y `ErrorCodes` (5 códigos genéricos) en `contracts/RealEstateCrm.Contracts`. Serialización camelCase fija (`RealEstateCrmJsonDefaults`). Contract tests verifican forma y versionado.
- **CONT-002** — `ExecutionContextV1` (actorId, displayName, email, roles, permissions, correlationId, causationId — **sin tenant**) y `EventEnvelopeV1<TPayload>` (eventId, name, version, occurredAt, actorId, correlationId, causationId, aggregateId, payload). Contract test de ejemplo validado con JSON congelado.
- **AUTH-001** — `AddKeycloakOpenIdConnectCookieAuthentication` (cookie HttpOnly + OIDC code flow contra Keycloak) y `AddKeycloakJwtBearerAuthentication` (valida el JWT emitido por Keycloak). Happy/error path probado con `TestServer` + JWT firmado localmente (sin Keycloak real). El caso con Keycloak real corriendo queda como test separado y marcado (ver Decisiones del equipo).
- **AUTH-002** — `ClaimsPrincipalAuthenticationPort` resuelve `AuthenticatedUser` (userId, displayName, email, roles) desde los claims del JWT ya validado, sin ningún concepto de tenant. `FakeAuthenticationPort` (en `tests/RealEstateCrm.TestSupport`, no en `building-blocks`: es un test double, no un puerto de producción) para unit tests de los servicios.
- **PERSIST-001** — `IRepository<TAggregate, TId>` + `IUnitOfWork` (puertos) y `MongoRepository<TAggregate, TId>` + `MongoUnitOfWork` (adapters). Repository contract test ejecutado contra Mongo real (transacción multi-documento).
- **MSG-001** — `IOutbox`/`IInbox` (puertos) y `MongoOutbox`/`MongoInbox` (adapters); `IEventPublisher`/`IEventConsumer<T>` (puertos) y `RabbitMqEventPublisher`/`RabbitMqEventConsumerHost` (adapters). Idempotencia probada contra Mongo real (índice único `EventId+ConsumerName`) y publish→consume probado contra RabbitMQ real.

## Archivos

Creados (write zone declarada: `contracts/`, `building-blocks/`, adapters de autenticación/persistencia/mensajería, tests contractuales):

- `contracts/RealEstateCrm.Contracts/{Context,Errors,Events,Paging,Serialization}/*.cs` (6 archivos): `ExecutionContextV1`, `ProblemDetailsV1`, `ErrorCodes`, `EventEnvelopeV1<TPayload>`, `PageV1<TItem>`, `RealEstateCrmJsonDefaults`.
- `building-blocks/RealEstateCrm.BuildingBlocks/{Authentication,Messaging,Persistence}/*.cs` (8 archivos): puertos `IAuthenticationPort`, `IRepository<TAggregate,TId>`, `IUnitOfWork`, `IOutbox`, `IInbox`, `IEventPublisher`, `IEventConsumer<TPayload>`; `AuthenticatedUser`, `OutboxMessage`. Sin ningún test double: `FakeAuthenticationPort` vive en `tests/RealEstateCrm.TestSupport`.
- `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/` (**proyecto nuevo**, decisión del equipo — ver abajo): `RealEstateCrm.BuildingBlocks.Infrastructure.csproj` + 17 archivos `.cs` en `Authentication/`, `Persistence/Mongo/`, `Messaging/{Mongo,RabbitMq}/`.
- `tests/RealEstateCrm.TestSupport/` (**proyecto nuevo**, class library sin dependencias de test framework): `RealEstateCrm.TestSupport.csproj` + `Authentication/FakeAuthenticationPort.cs`. Solo lo referencian proyectos de test (`RealEstateCrm.ContractTests` hoy); ningún `*.Application`/`*.Infrastructure` de `services/` ni `bffs/` puede llegar a él.
- `tests/RealEstateCrm.ContractTests/` (**proyecto nuevo**): 8 archivos de test (contratos de `ProblemDetailsV1`, `PageV1<T>`, `ExecutionContextV1`, `EventEnvelopeV1<T>`, `OutboxMessage`, `FakeAuthenticationPort`, guardrail anti-tenant).
- `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests/` (**proyecto nuevo**): 6 archivos de test (pipeline JWT, extracción de roles, idempotencia de Inbox, transacción de Outbox, publish/consume RabbitMQ, test de Keycloak real marcado).
- `RealEstateCrm.slnx`: entradas para los 4 proyectos nuevos.
- `building-blocks/RealEstateCrm.BuildingBlocks/RealEstateCrm.BuildingBlocks.csproj`: agregada `ProjectReference` a `contracts`.
- `tests/RealEstateCrm.ArchitectureTests/DependencyRulesTests.cs`: nueva regla `Application_projects_do_not_reference_BuildingBlocks_Infrastructure`.

No se modificó ningún archivo fuera de la write zone ni de la lista prohibida (`README.md`, `ARCHITECTURE.md`, `AGENTS.md`, `docs/adr/`, `docs/implementation/`, `docs/tasks/wave-*`, `design_handoff_*`, board). No se tocó ningún `Program.cs` de servicios/BFF ni `apps/`.

## Comandos y resultados

```text
$ dotnet build RealEstateCrm.slnx
Compilación correcta.
    0 Advertencia(s)
    0 Errores
```

**Comando sin infraestructura** (ningún contenedor corriendo — este es el comando a usar en
CI o en una PC sin Docker levantado; los 3 proyectos de test quedan 100% en verde porque los
únicos tests que tocan Mongo/RabbitMQ/Keycloak reales están marcados con
`[Trait("Category", "RequiresMongo"|"RequiresRabbitMq"|"RequiresKeycloak")]`):

```text
$ dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
RealEstateCrm.ArchitectureTests.dll                   → Superado: 3,  Total: 3
RealEstateCrm.ContractTests.dll                       → Superado: 21, Total: 21
RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 10, Total: 10
```

Con infraestructura real levantada, el comando `--filter "Category!=RequiresKeycloak"` deja
15/15 en `RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll` (10 sin infra + 5 contra
Mongo/RabbitMQ reales; ver "Evidencia contra infraestructura real" abajo).

Demostración en rojo del nuevo test de arquitectura (revertida inmediatamente después):

```text
$ dotnet add services/party-service/src/PartyService.Application reference \
    building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure

$ dotnet test tests/RealEstateCrm.ArchitectureTests
[FAIL] Application_projects_do_not_reference_BuildingBlocks_Infrastructure
  Application no debe referenciar RealEstateCrm.BuildingBlocks.Infrastructure
  (solo puertos): PartyService.Application.csproj
Con error! - Con error: 1, Superado: 2, Total: 3

# dotnet remove reference revirtió el cambio; re-ejecución: 3/3 en verde.
```

### Evidencia contra infraestructura real (no simulada)

Se levantaron contenedores efímeros para no depender de `V2-FND-003` (que todavía no
existe) y ejecutar los tests marcados contra infraestructura real en vez de mockearla:

```text
$ docker run -d --rm --name fnd002-mongo-standalone -p 27017:27017 mongo:7
$ docker run -d --rm --name fnd002-mongo-replset -p 27018:27018 \
    mongo:7 mongod --replSet rs0 --port 27018 --bind_ip_all
$ docker exec fnd002-mongo-replset mongosh --port 27018 --eval \
    'rs.initiate({_id:"rs0", members:[{_id:0, host:"localhost:27018"}]})'
$ docker run -d --rm --name fnd002-rabbitmq -p 5672:5672 rabbitmq:4-management

$ MONGO_CONNECTION_STRING=mongodb://localhost:27017 \
  MONGO_REPLICA_SET_CONNECTION_STRING="mongodb://localhost:27018/?replicaSet=rs0" \
  RABBITMQ_HOST=localhost \
  dotnet test tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests \
    --filter "Category=RequiresMongo|Category=RequiresRabbitMq"

Correctas! - Con error: 0, Superado: 5, Total: 5
  - MongoInboxIdempotencyTests (2): segundo consumo del mismo eventId no repite el
    efecto; consumidores distintos sí procesan el mismo eventId por separado.
  - MongoOutboxTransactionTests (2): aggregate + outbox commitean atómicos; una
    excepción dentro de la transacción no deja ni aggregate ni mensaje de outbox.
  - RabbitMqPublishConsumeTests (1): publish → exchange topic → cola con routing
    key = nombre del evento → consumer recibe exactamente 1 mensaje.

$ docker rm -f fnd002-mongo-standalone fnd002-mongo-replset fnd002-rabbitmq
```

Durante esta corrida contra Mongo real aparecieron y se corrigieron dos bugs reales
que ningún test en memoria hubiera detectado (ver "Decisiones del equipo").

## Ejemplos JSON

`EventEnvelopeV1<TPayload>` (test congelado en `EventEnvelopeV1ContractTests`):

```json
{
  "eventId": "8f14e45f-ceea-4a1f-8f5b-6c2c3b6a7b00",
  "name": "PartyRegistered",
  "version": 1,
  "occurredAt": "2026-09-16T12:00:00+00:00",
  "actorId": "3c2f2e10-9b1a-4a3e-8b2a-2a6f1e6a5b11",
  "correlationId": "1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22",
  "causationId": "5e6f7a8b-9c0d-4e1f-a2b3-c4d5e6f7a833",
  "aggregateId": "9c0d1e2f-3a4b-4c5d-8e9f-0a1b2c3d4e44",
  "payload": { "partyName": "Empresa Demo SA" }
}
```

`ProblemDetailsV1` (token inválido, generado por `ProblemDetailsChallengeWriter` vía
`JwtBearerEvents.OnChallenge`):

```json
{
  "type": "about:blank",
  "title": "No se pudo validar el token.",
  "status": 401,
  "detail": "The token expired ...",
  "instance": "/secure",
  "errorCode": "unauthorized",
  "correlationId": "1a2b3c4d-5e6f-4a1b-9c2d-3e4f5a6b7c22"
}
```

## Decisiones del equipo

Estas seis decisiones estaban explícitamente vedadas para el agente (sección 5 del
plan de ejecución) y se resolvieron con el humano antes de escribir código:

1. **IDs públicos:** `Guid` nativo, serializado como string en JSON. Sin dependencias nuevas.
2. **Outbox:** colección Mongo separada (`outbox_messages`) **con transacción multi-documento**
   junto con el aggregate (`MongoUnitOfWork` + `MongoSessionAccessor` ambiente).
   ⚠️ **Requiere Mongo con replica set** en el Compose de `V2-FND-003` — en un Mongo
   standalone `session.StartTransaction()` falla. Coordinar antes de cerrar esa task.
3. **RabbitMQ:** un exchange **topic** por servicio productor (`RabbitMqOptions.ServiceExchangeName`),
   routing key = nombre del evento (`OutboxMessage.Name`), una cola + una DLQ por
   consumidor (`RabbitMqConsumerRegistration`, dead-lettering vía `x-dead-letter-exchange`/`x-dead-letter-routing-key`).
4. **Tokens OIDC:** cookie de sesión HttpOnly en el BFF (`AddKeycloakOpenIdConnectCookieAuthentication`).
   El front nunca ve el token; ya es compatible con `apps/crm-web` y `apps/platform-admin-web`
   (ambos usan `fetch(..., {credentials:"include"})` y `BFF_CONTRACT.md` documenta que el
   BFF resuelve sesión/permisos server-side) sin tocar su código.
5. **Catálogo de errores:** `ErrorCodes` con 5 códigos genéricos transversales
   (`validation_error`, `unauthorized`, `forbidden`, `not_found`, `conflict`). El
   catálogo específico de cada dominio se agrega en su propia wave.
6. **Versiones de paquete:** fijadas exactas (sin rango) en
   `RealEstateCrm.BuildingBlocks.Infrastructure.csproj`:
   `MongoDB.Driver 3.11.2`, `RabbitMQ.Client 7.2.2`,
   `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.12`,
   `Microsoft.AspNetCore.Authentication.OpenIdConnect 10.0.12`
   (10.0.12 = versión del runtime ASP.NET Core instalado en esta PC). **Sin**
   `Keycloak.AuthServices.Authentication`: la autenticación usa solo paquetes
   oficiales de Microsoft; el mapeo de roles de Keycloak (`realm_access.roles`,
   un claim JSON, no claims individuales) se resuelve a mano en
   `ClaimsPrincipalAuthenticationPort.ExtractRealmRoles`.

Además, ajustes pedidos explícitamente sobre la primera propuesta:

- **Separación puertos/adapters:** `RealEstateCrm.BuildingBlocks` (proyecto ya
  existente de `V2-FND-001`) quedó **solo** con interfaces/DTOs, sin ningún
  `PackageReference` de Mongo/RabbitMQ/ASP.NET Core — sigue teniendo cero
  dependencias externas más allá de `contracts`. Todos los adapters concretos
  (Mongo, RabbitMQ, Keycloak/JwtBearer) se movieron a un proyecto nuevo,
  `RealEstateCrm.BuildingBlocks.Infrastructure`. Se agregó la regla de
  arquitectura `Application_projects_do_not_reference_BuildingBlocks_Infrastructure`
  para que ningún `*.Application.csproj` pueda referenciarlo directamente
  (demostrada en rojo, ver arriba).
- **Test de Keycloak real como opt-in:** `KeycloakDevRealmTests` está marcado
  con `[Trait("Category", "RequiresKeycloak")]` y se excluye con
  `dotnet test --filter "Category!=RequiresKeycloak"` (ya aplicado en los
  comandos de este reporte). El resto de los tests de autenticación usan un
  JWT firmado con una clave simétrica local o `FakeAuthenticationPort`, no
  Keycloak real.
- **`FakeAuthenticationPort` fuera de `building-blocks`:** movido a
  `tests/RealEstateCrm.TestSupport` (proyecto nuevo). Es un test double, no un
  puerto ni una abstracción de producción; dejarlo en `RealEstateCrm.BuildingBlocks`
  hubiera hecho que cualquier servicio real pudiera referenciarlo por
  accidente. Solo `RealEstateCrm.ContractTests` lo referencia hoy.
- **0 warnings:** al revisar antes del commit aparecieron 4 warnings (2
  `CA2255` por los `[ModuleInitializer]` de Mongo, justificados y silenciados
  con `#pragma` + comentario; 2 `ASPDEPR004`/`ASPDEPR008` por `WebHostBuilder`/
  `TestServer(IWebHostBuilder)` obsoletos en .NET 10). Se modernizó
  `JwtBearerAuthenticationPipelineTests` a `HostBuilder.ConfigureWebHost(...).UseTestServer()`
  + `IHost.GetTestClient()`. `dotnet build` queda en 0 advertencias, 0 errores.
- **Trait de `MongoOutboxTransactionTests` corregido:** tenía
  `[Trait("Category", "RequiresMongoReplicaSet")]`, un cuarto nombre que no
  coincidía con ninguna de las tres categorías canónicas (`RequiresMongo`,
  `RequiresRabbitMq`, `RequiresKeycloak`). Con
  `dotnet test --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"`
  ese test no quedaba excluido y fallaba por timeout contra `localhost:27018`
  en una PC sin Mongo corriendo. Corregido a `RequiresMongo` (mismo Trait que
  `MongoInboxIdempotencyTests`, aunque apunten a instancias Mongo distintas:
  una standalone, la otra con replica set — ver comentario en el archivo).
  Verificado el comando de arriba sin ningún contenedor levantado: 34/34 en
  verde.

### Bugs reales encontrados al testear contra Mongo real (no simulado)

Ningún test en memoria los hubiera detectado; aparecieron recién al correr contra
un `mongo:7` real:

1. **`GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified`**:
   desde MongoDB.Driver 3.x, `Guid` ya no tiene una representación BSON por
   defecto. Se agregó `MongoGuidSerializationBootstrap` (`[ModuleInitializer]`)
   que registra `GuidRepresentation.Standard` una única vez por proceso para
   todo el building block, así ningún servicio futuro tiene que acordarse de
   configurarlo.
2. **`Element '_id' does not match any field or property of class OutboxMessage`**:
   al no tener ningún miembro mapeado como id, Mongo autogenera un `_id`
   (`ObjectId`) que el deserializador no reconocía. Como `OutboxMessage` vive en
   el proyecto de puertos (sin MongoDB.Bson, por la decisión de separación de
   arriba), no puede llevar atributos `[BsonId]`; se resolvió con un
   `BsonClassMap` registrado por código en `MongoClassMapBootstrap`
   (`Infrastructure`) que mapea `EventId` como `_id` para `OutboxMessage` e
   `InboxConsumedMessage`.

## Supuestos

- El realm local de Keycloak con usuario de desarrollo (`KEYCLOAK_BASE_URL`,
  `KEYCLOAK_REALM`, etc.) todavía no existe: lo crea `V2-FND-003`. Hasta
  entonces, `KeycloakDevRealmTests` queda escrito y compilando, pero sin correr.
- `AddKeycloakOpenIdConnectCookieAuthentication`/`AddKeycloakJwtBearerAuthentication`
  son extensiones de `IServiceCollection` listas para usar, pero **no se
  invocan desde ningún `Program.cs`** de servicio o BFF: `bffs/` y los `Api` de
  cada servicio no están en la write zone de esta task. Wire-up real (login
  interactivo end-to-end desde `operations-bff`) queda para la wave que
  implemente el primer journey autenticado.
- Ningún servicio de dominio (`services/*`) registra todavía un
  `MongoRepository<TAggregate, TId>` concreto ni un `IEventConsumer<TPayload>`:
  no hay aggregates aún (esperado, son de wave 2+). `MongoOutboxTransactionTests`
  y `RabbitMqPublishConsumeTests` usan un aggregate/payload de ejemplo definido
  dentro del propio test para probar el building block de forma aislada.
- Node de la PC es v24.16.0 (el plan pide "Node 22+"); no se tocó nada de
  `apps/` en esta task, mismo supuesto documentado ya en el reporte de FND-001.

## Follow-ups

- `V2-FND-003` debe configurar Mongo como **replica set** (aunque sea de un
  solo nodo) en el Compose: el Outbox transaccional decidido en esta task no
  funciona contra un Mongo standalone. Coordinar antes de cerrar esa task.
- `V2-FND-003` debe levantar el realm local de Keycloak con un usuario de
  desarrollo para poder correr `KeycloakDevRealmTests`
  (`dotnet test --filter "Category=RequiresKeycloak"`, variables de entorno
  documentadas en el test).
- Cuando la primera wave con aggregates reales (`V2-ACL-001`/`V2-PTY-001`)
  arranque, van a necesitar: (a) su propio `MongoRepository<TAggregate, TId>`
  concreto, (b) decidir si el aggregate lleva o no atributos `MongoDB.Bson`
  directamente (hoy el building block no fuerza ninguna de las dos opciones),
  y (c) wirear `AddMongoPersistence`/`AddRabbitMqMessaging`/
  `AddKeycloak*Authentication` en su propio `Program.cs`.
- El catálogo de errores específico de dominio (más allá de los 5 genéricos)
  se define en cada wave que lo necesite, no acá.
- `RabbitMqEventConsumerHost` no implementa reintentos con backoff antes de
  mandar a la DLQ (nack directo, `requeue:false`, primer fallo va a la DLQ).
  Si una wave futura necesita reintentos, extenderlo ahí.

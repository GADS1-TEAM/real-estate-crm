# V2-FND-002 — Contratos, autenticación OIDC y persistencia por ports

- **Ola:** 1 — Fundación ejecutable
- **Estado:** TODO
- **Dependencias:** V2-FND-001
- **UCs:** CONT-001, CONT-002, AUTH-001, AUTH-002, PERSIST-001, MSG-001
- **Owner:** building-blocks, contracts y access adapter
- **Write zone:** contracts, building-blocks, adapters de autenticación/persistencia/mensajería y tests contractuales

## Resultado esperado

Existe una gramática común para APIs y eventos, login OIDC contra Keycloak,
contexto de actor sin tenant, repositories Mongo detrás de ports y Outbox/Inbox
idempotentes para los eventos que alimentan proyecciones.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| CONT-001 | Servicios | Serializar Problem Details, paginación, errores estables y versionado. | contracts | Contract tests. |
| CONT-002 | Servicios | Propagar actorId, roles, permissions, correlationId y causationId. | building-blocks | Evento de ejemplo validado. |
| AUTH-001 | Usuario | Iniciar sesión y recuperar sesión OIDC. | auth adapter | Happy/error path. |
| AUTH-002 | Sistema | Resolver usuario y roles sin tenant context. | access adapter | Claims/context tests. |
| PERSIST-001 | Servicio owner | Leer/escribir Mongo mediante un port propio. | building-blocks | Repository contract test. |
| MSG-001 | Servicio owner | Guardar outbox y procesar inbox sin duplicar side effects. | messaging | Idempotency test. |

## Interfaces

- API: ProblemDetails v1, Page<T> v1, ExecutionContext v1 sin tenantId.
- Evento: EventEnvelope v1 con eventId, name, version, occurredAt, actorId,
  correlationId, causationId, aggregateId y payload.
- Ports mínimos: IAuthenticationPort, IRepository<T>, IUnitOfWork,
  IOutbox, IInbox, IEventPublisher y IEventConsumer.
- Keycloak adapter: devuelve userId, displayName, email y claims de roles; la
  autorización de negocio se resuelve en access-service.

## Reglas

- No añadir tenantId, organizationId ni filtros de organización a los
  contratos V2.
- No compartir tipos de dominio entre servicios; contracts contienen DTOs y
  envelopes, no aggregates.
- Los repositorios reciben el actor context cuando una query requiere
  autorización, pero la base no inventa un tenant.
- Outbox e Inbox deben aceptar reintentos y conservar eventId.
- Un token válido no equivale automáticamente a permiso para toda operación.

## Criterios de aceptación

- [ ] Un contrato incompatible rompe el contract test.
- [ ] Un evento de ejemplo puede serializarse/deserializarse con metadata de
  actor y correlación.
- [ ] No existe un filtro tenant obligatorio en los repositorios V2.
- [ ] Un segundo consumo del mismo eventId no repite la proyección.
- [ ] Keycloak autentica un usuario de desarrollo y un token inválido produce
  Problem Details estable.

## Overrides POC

- Se permite fake auth para unit tests y un realm local de Keycloak para
  integración.
- MongoDB Community es la persistencia; no se agrega otra base.
- RabbitMQ Community se conecta en la infraestructura local.

## Definition of Done

- [ ] Contracts versionados y ejemplos.
- [ ] Ports y adapters compilables.
- [ ] Tests de auth, serialización e idempotencia en verde.
- [ ] Documentación de breaking/non-breaking changes.

## Evidencia requerida

1. Ejemplos JSON de API y evento.
2. Resultado de tests contractuales.
3. Flujo de login local.
4. Test que demuestre idempotencia del Inbox.

## Handoff

**Qué quedó:** `contracts/RealEstateCrm.Contracts` tiene `ProblemDetailsV1`, `PageV1<T>`,
`ExecutionContextV1` (sin tenant) y `EventEnvelopeV1<TPayload>`. `building-blocks/RealEstateCrm.BuildingBlocks`
tiene los puertos (`IAuthenticationPort`, `IRepository<T,TId>`, `IUnitOfWork`, `IOutbox`,
`IInbox`, `IEventPublisher`, `IEventConsumer<T>`) sin ningún paquete de infraestructura.
`building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure` (proyecto nuevo, decisión del
equipo) tiene los adapters reales: Mongo (repository genérico, unit of work transaccional,
outbox, inbox), RabbitMQ (publisher, consumer host con DLQ) y Keycloak (JwtBearer para
servicios backend, cookie+OIDC para BFF). `dotnet build`/`dotnet test` en verde (39 tests,
incluye 5 corridos contra Mongo/RabbitMQ reales con contenedores efímeros; el test de
Keycloak real queda escrito pero sin correr, ver abajo). Detalle completo, comandos,
decisiones del equipo y los dos bugs reales encontrados contra Mongo en
`IMPLEMENTATION_REPORT-V2-FND-002.md` (esta misma carpeta).

**Qué falta:**
- Ningún servicio (`services/*`) usa todavía estos building blocks: no hay aggregates
  reales, ni `MongoRepository<T,TId>` concreto, ni `IEventConsumer<T>` registrado por
  ningún servicio (esperado, son de wave 2+).
- Ningún `Program.cs` de servicio/BFF invoca `AddMongoPersistence`/`AddRabbitMqMessaging`/
  `AddKeycloak*Authentication`: `bffs/` y los `Api` no estaban en la write zone de esta task.
- El realm local de Keycloak (`V2-FND-003`) no existe todavía, así que
  `KeycloakDevRealmTests` (`[Trait("Category","RequiresKeycloak")]`) no se pudo correr.
- El Outbox transaccional decidido acá **requiere Mongo con replica set** en el Compose de
  `V2-FND-003` (que todavía no definió standalone vs. replica set) — coordinar antes de
  cerrar esa task, o el Outbox de todos los servicios futuros va a fallar en runtime.

**Cómo verificar (sin infraestructura — este es el comando a usar por defecto,
también documentado en `DEVELOPMENT.md`):**

```bash
dotnet build RealEstateCrm.slnx
dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
```

Sin ningún contenedor corriendo, este comando debe dar 100% verde (34 tests): los únicos
tests que tocan Mongo/RabbitMQ/Keycloak reales están marcados con exactamente uno de estos
tres Traits (`RequiresMongo`, `RequiresRabbitMq`, `RequiresKeycloak`) y quedan excluidos.

**Con infraestructura real** (para correr también esos tests contra Mongo/RabbitMQ reales):

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

`KeycloakDevRealmTests` necesita además el realm local de `V2-FND-003`
(`dotnet test --filter "Category=RequiresKeycloak"`, variables de entorno documentadas en el
test).

El test de arquitectura extendido (`tests/RealEstateCrm.ArchitectureTests`) debe seguir en
3/3 verde; si un `*.Application.csproj` referencia `RealEstateCrm.BuildingBlocks.Infrastructure`
directamente, debe fallar (se verificó manualmente en rojo durante esta task, ver reporte).

**Para continuar (V2-ACL-001, V2-FND-003, y cualquier wave 2+):**
- Los namespaces/nombres de `contracts` y `building-blocks` ya son estables. No renombrar
  sin abrir ADR.
- Un servicio nuevo con aggregates reales: su `Infrastructure` referencia
  `RealEstateCrm.BuildingBlocks.Infrastructure` (permitido) y su `Application` referencia
  solo `RealEstateCrm.BuildingBlocks` (el test de arquitectura lo hace cumplir).
- `V2-FND-003`: definir Mongo replica set en el Compose (obligatorio por la decisión de
  Outbox de esta task) y el realm local de Keycloak con un usuario de desarrollo
  (variables documentadas en `KeycloakDevRealmTests`).

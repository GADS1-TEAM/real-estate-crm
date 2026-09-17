# Implementation report — V2-ACL-001 — Usuarios, roles, permisos y responsables

## UCs cubiertos

- **USR-001** — `POST /api/v1/users`, `PUT /api/v1/users/{id}` en `AccessService.Api`. Aggregate
  `UserAccount` (Domain, inmutable/record). Tests de `UserAccountService` (alta, edición,
  duplicado de `keycloakSubject` → 409).
- **USR-002** — `POST /api/v1/users/{id}/deactivate`. `UserAccount.Deactivate()` solo cambia
  `Status`; no toca ninguna otra colección (no hay historial de actividades/oportunidades que
  borrar en V2 — Wave 1 excluye esos agregados — así que "no borra la historia" se demuestra acá
  como "no muta ni elimina el propio documento del usuario", más los eventos ya publicados con su
  `UserId`). Doble desactivación → 409 `user_already_inactive`.
- **AUTHZ-001** — `POST /api/v1/users/{id}/role-assignment`. Aggregate `RoleAssignment`: su
  `_id` de Mongo **es** `UserId` (invariante "un solo rol activo por usuario" enforzada por
  construcción, no por validación de negocio). Reasignar reemplaza el rol anterior; el evento
  `RoleAssigned` lleva `previousRoleCode`.
- **AUTHZ-002** — Matriz rol→permiso en backend (`PermissionMatrix`, confirma sin cambios la
  propuesta de `IMPLEMENTATION_REPORT-V2-ACL-001a.md`) + `AccessServiceAuthorizationEvaluator`
  (implementación local de `IAuthorizationPort`, sin loop HTTP a sí mismo) +
  `POST /api/v1/authorization/evaluate` (endpoint HTTP interno que consumen los demás servicios)
  + `GET /api/v1/authorization/role-permission-matrix` (evidencia de la matriz completa, consumida
  por la screen ADM-03). Orden de precedencia pedido explícitamente: PENDING → `user_pending`,
  INACTIVE → `user_inactive`, sin permiso → `permission_not_granted`.
- **ASSIGN-001 / ASSIGN-002** (alcance acotado, ver "Decisiones locales" #1) —
  `POST /api/v1/assignments/validate`: valida que el actor tenga el permiso de negocio para
  asignar responsable sobre un `resourceType` y que el `responsibleUserId` propuesto exista y
  esté ACTIVE. **No persiste la asignación**: el dato `responsibleUserId` es del servicio owner
  del recurso (`party-service`, V2-PTY-001, que no existe todavía). Contrato del evento
  `ResponsibleAssignedV1` publicado en `contracts` para que V2-PTY-001 lo use cuando implemente
  la persistencia real.
- **Primer wire-up real** — `AccessService.Api`: `AddMongoPersistence`, `AddRabbitMqMessaging`,
  `AddOutboxRelay`, `AddKeycloakJwtBearerAuthentication`, `AddCrmHealthChecks`,
  `AddCrmObservability`, `UseCrmCorrelationId`, manejador centralizado de excepciones
  (`IExceptionHandler` → `ProblemDetailsV1`). `OperationsBff.Api`: cookie OIDC
  (`AddKeycloakOpenIdConnectCookieAuthentication`), token relay hacia access-service (D6), mismo
  wire-up de health/observabilidad/correlationId.
- **Cierra follow-up de V2-FND-003** — Test end-to-end real contra Mongo/RabbitMQ/Keycloak:
  `CorrelationIdAndAuthorizationEndToEndTests.Correlation_id_survives_from_the_incoming_header_to_the_published_event`
  entra con el mismo header `X-Correlation-Id` que reenvía `operations-bff`, pasa por
  `CorrelationIdMiddleware` → `UserAccountService` → `IOutbox` → `OutboxRelayBackgroundService` →
  RabbitMQ real, y verifica que el evento `UserCreated` consumido trae el mismo `correlationId`.
- **BFF** — `GET /screens/{screenId}` (`ADM-01` Usuarios, `ADM-03` Roles y permisos, `ADM-04`
  Permisos efectivos — los 3 `screenId` reales de
  `apps/crm-web/src/lib/screen-registry.ts` con `task: "V2-ACL-001"` que corresponden a datos de
  acceso; `ADM-02` "Invitar usuario" excluido, overrides POC) y `POST /mutations/{name}`
  (`createUser`/`updateUser`/`deactivateUser`/`assignRole`), proxy delgado con token relay.
  `apps/crm-web` no se tocó.
- **Seed D9** — `AccessServiceDevSeedHostedService` (solo `Development`): 3 usuarios
  (Administrador/Vendedor/Responsable Comercial) vinculados por `id` fijo a los 3 usuarios nuevos
  del realm-export.

## Archivos

Write zone declarada (`access-service` y sus contratos, adapter HTTP de `IAuthorizationPort` en
`BuildingBlocks.Infrastructure`, wire-up inicial de `operations-bff`, realm-export, tests) **más**
el relay de outbox en `BuildingBlocks.Infrastructure` (pedido explícito del humano) y el soporte
de concurrencia optimista en el mismo proyecto (pedido explícito, ver "Decisiones locales" #2).

**`contracts/RealEstateCrm.Contracts/`** (nuevo, sin tocar nada de V2-ACL-001a):
- `Authorization/AuthorizationEvaluationRequestV1.cs`, `ResponsibleAssignmentValidationRequestV1.cs`,
  `ResponsibleAssignmentDenyReasons.cs`.
- `Events/Access/UserEventsV1.cs` (`UserCreatedV1`, `UserUpdatedV1`, `UserDeactivatedV1`,
  `RoleAssignedV1`), `ResponsibleAssignedV1.cs`.

**`building-blocks/RealEstateCrm.BuildingBlocks/`** (puertos, sin dependencias de infraestructura):
- `Authorization/IResponsibleAssignmentValidationPort.cs`.
- `Persistence/ConcurrencyConflictException.cs`.

**`building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/`** (nuevo):
- `Persistence/Mongo/VersionedMongoRepository.cs` — concurrencia optimista reutilizable (D8, sin
  romper `IRepository<TAggregate, TId>`).
- `Messaging/Outbox/OutboxRelayOptions.cs`, `OutboxRelayBackgroundService.cs`,
  `OutboxRelayServiceCollectionExtensions.cs` — relay reutilizable pedido explícitamente.
- `Authorization/AuthorizationClientOptions.cs`, `HttpAuthorizationPort.cs`,
  `HttpResponsibleAssignmentValidationPort.cs`, `AuthorizationClientServiceCollectionExtensions.cs`
  — adapters HTTP con `IMemoryCache` ≤60 s (D2) para que otros servicios consuman access-service.

**`services/access-service/src/`**:
- `AccessService.Domain/`: `UserAccount.cs`, `UserStatus.cs`, `RoleAssignment.cs`, `RoleCodes.cs`.
  Cero `ProjectReference` (verificado por `RealEstateCrm.ArchitectureTests`).
- `AccessService.Application/`: `AccessDomainException.cs` (+ `AccessErrorCodes`),
  `Ports/IUserAccountReadPort.cs`, `Authorization/{PermissionMatrix,ResourceTypePermissionMap,
  AccessServiceAuthorizationEvaluator,AccessServiceResponsibleAssignmentValidator}.cs`,
  `Users/{UserSummary,EffectivePermissionsResult,UserAccountService}.cs`. Ahora referencia
  `RealEstateCrm.BuildingBlocks` (puertos) y `RealEstateCrm.Contracts` — no
  `BuildingBlocks.Infrastructure` (test de arquitectura lo verifica).
- `AccessService.Infrastructure/`: `Persistence/Mongo/{AccessServiceMongoClassMapBootstrap,
  UserAccountRepository,RoleAssignmentRepository,UserAccountReadRepository,
  AccessServiceIndexInitializer}.cs`, `Seeding/{DevSeedUsers,AccessServiceDevSeedHostedService}.cs`,
  `AccessServiceInfrastructureServiceCollectionExtensions.cs`.
- `AccessService.Api/`: `Program.cs` (reescrito, wire-up real), `appsettings.json` (Mongo/RabbitMq/
  OutboxRelay/Keycloak), `Users/{UserRequests,UsersController}.cs`,
  `Authorization/{AuthorizationController,AssignmentsController}.cs`,
  `ExecutionContextResolution/CurrentExecutionContextProvider.cs` (también hace el
  auto-provisioning PENDING de D3), `ErrorHandling/{ProblemDetailsExceptionHandler,
  ProblemDetailsResults}.cs`.

**`bffs/operations-bff/src/OperationsBff.Api/`**: `Program.cs` (reescrito), `appsettings.json`,
`AccessService/{AccessServiceClient,ProxyResults}.cs`, `Screens/ScreensController.cs`,
`Mutations/MutationsController.cs`, `ErrorHandling/ProblemDetailsExceptionHandler.cs`.
`OperationsBff.Application` queda sin usar (mismo criterio que ya traía del esqueleto: un BFF
delgado no necesita capa de aplicación propia para un simple proxy).

**`infra/keycloak/realm-export/crm-dev-realm.json`**: agregados `dev.administrador`
(`id` fijo `a0000000-…-001`, rol Administrador) y `dev.responsable` (`id` fijo `…-003`, rol
Responsable Comercial); a `dev.vendedor` (ya existía) se le fijó `id` `…-002` para poder
vincularlo determinísticamente al seed (antes no tenía `id` explícito).

**Tests** (nuevos, ~66 tests):
- `services/access-service/tests/AccessService.Application.Tests/` (proyecto nuevo, 46 tests):
  `PermissionMatrix`, `AccessServiceAuthorizationEvaluator` (orden PENDING/INACTIVE/sin permiso),
  `AccessServiceResponsibleAssignmentValidator`, `UserAccountService` (alta/edición/baja/rol/
  paginado/permisos efectivos), `UserAccount`/`RoleAssignment` (invariantes de dominio).
- `services/access-service/tests/AccessService.Api.Tests/` (proyecto nuevo, 2 tests,
  `RequiresMongo|RequiresRabbitMq|RequiresKeycloak`): 403 real de Vendedor creando un usuario;
  correlationId end-to-end contra infraestructura real.
- `tests/RealEstateCrm.TestSupport/`: `Authorization/FakeResponsibleAssignmentValidationPort.cs`,
  `Persistence/{InMemoryRepository,PassthroughUnitOfWork}.cs`, `Messaging/InMemoryOutbox.cs`
  (reutilizables por V2-CAT-001/V2-PTY-001, mismo espíritu que los fakes de V2-ACL-001a).
- `tests/RealEstateCrm.ContractTests/`: `Authorization/ResponsibleAssignmentDenyReasonsContractTests.cs`,
  `Events/Access/{UserEventsV1ContractTests,ResponsibleAssignedV1ContractTests}.cs` (9 tests).
- `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests/`: `Persistence/Mongo/
  VersionedMongoRepositoryTests.cs` (`RequiresMongo`, 2 tests), `Messaging/Outbox/
  OutboxRelayBackgroundServiceTests.cs` (`RequiresMongo`+`RequiresRabbitMq`, 1 test),
  `Authorization/HttpAuthorizationPortTests.cs` (caché, 3 tests sin infra).

**Modificados fuera de `services/access-service`/`bffs/operations-bff`**: `RealEstateCrm.slnx`
(3 proyectos de test nuevos), `.env.example` y `DEVELOPMENT.md` (sección 8 del ciclo: usuarios de
dev y cómo correr el slice).

No se tocó `apps/crm-web`, ni ningún archivo de la lista prohibida (Wave 1 §3), ni código de
otro servicio de dominio.

## Comandos y resultados

```text
$ dotnet build RealEstateCrm.slnx
Compilación correcta.
    0 Advertencia(s)
    0 Errores

$ dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"
RealEstateCrm.ArchitectureTests.dll                   → Superado: 3,  Total: 3
AccessService.Application.Tests.dll                    → Superado: 46, Total: 46  (nuevo)
RealEstateCrm.ContractTests.dll                        → Superado: 60, Total: 60  (51 previos + 9 nuevos)
RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll  → Superado: 51, Total: 51  (48 previos + 3 nuevos, HttpAuthorizationPort)
AccessService.Api.Tests.dll                            → 0 tests (los 2 propios son RequiresMongo|RequiresRabbitMq|RequiresKeycloak)

$ bash scripts/test-fast.sh
==> dotnet build         → Compilación correcta.
==> dotnet test          → mismos números de arriba
==> apps/scripts/build-webs.sh
    crm-web: next build              → ✓ Compiled successfully
    platform-admin-web: next build   → ✓ Compiled successfully

$ bash scripts/test-integration.sh   (Docker real: mongo, rabbitmq, keycloak con el realm-export actualizado)
==> healthchecks: crm-mongo / crm-rabbitmq / crm-keycloak → healthy
==> dotnet test --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak"
RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 14, Total: 14
  (11 heredados de FND-002/FND-003 + VersionedMongoRepositoryTests (2) + OutboxRelayBackgroundServiceTests (1))
AccessService.Api.Tests.dll                            → Superado: 2,  Total: 2
  (403 real de Vendedor + correlationId end-to-end)
```

Gate del Paso 0 (plan Wave 2 sección 2) verificado antes de escribir código: Wave 1 DONE en el
board, las 11 decisiones de la sección 5 ya marcadas, Docker Desktop corriendo,
`bash scripts/test-integration.sh` en verde en `main` (11/11) antes de cualquier cambio.

### Bug real encontrado y corregido contra infraestructura real (no simulado)

1. **`AccessServiceDevSeedHostedService` no arrancaba bajo `WebApplicationFactory`**: `ValidateScopes`
   (que `WebApplicationFactory` activa por defecto, a diferencia de `dotnet run`) detectó que un
   `IHostedService` (siempre singleton) consumía puertos `Scoped` (`IUserAccountReadPort`,
   `IRepository<...>`) directo en el constructor. Corregido con el mismo patrón que
   `OutboxRelayBackgroundService`: resuelve un `IServiceScope` propio en `StartAsync` en vez de
   inyectar las dependencias scoped directamente.
2. **401 en vez de 403/200 en el primer login real**: el token que Keycloak emite para el client
   `operations-bff` no llevaba `aud=operations-bff` por defecto (sin un audience mapper dedicado
   en el realm), y `Keycloak:Audience=operations-bff` en `appsettings.json` de `access-service`
   activaba `ValidateAudience=true`, rechazando el token. **Resuelto agregando un
   `protocolMapper` de tipo `oidc-audience-mapper`** al client `operations-bff` en
   `infra/keycloak/realm-export/crm-dev-realm.json` (`included.client.audience:
   "operations-bff"`, `access.token.claim: true`), que hace que el token real incluya
   `aud=operations-bff`. `Keycloak:Audience` quedó en `"operations-bff"` con `ValidateAudience`
   activo (validación real, no deshabilitada). Verificado recreando el contenedor de Keycloak
   (reimporta el realm) y corriendo `test-fast`/`test-integration` completos: 176/176 en verde,
   incluyendo el 403 real y el e2e de correlationId con audiencia validada de punta a punta.

Ninguno de los dos se hubiera detectado sin correr contra `WebApplicationFactory` + Keycloak real:
son exactamente el tipo de bug que un mock no reproduce.

## Contratos publicados

**Endpoints REST v1** (`AccessService.Api`, D1 — controllers + `ProblemDetailsV1`):

| Método y ruta | Permiso requerido | Notas |
|---|---|---|
| `POST /api/v1/users` | `users.manage` | USR-001. Body: `keycloakSubject, displayName, email`. 201 + `UserSummary`. 409 `user_already_exists` si el `sub` ya tiene cuenta. |
| `GET /api/v1/users?page=&pageSize=` | `users.read` | GetUsers. `PageV1<UserSummary>`. |
| `PUT /api/v1/users/{id}` | `users.manage` | USR-001. Body: `displayName, email`. |
| `POST /api/v1/users/{id}/deactivate` | `users.manage` | USR-002. 409 `user_already_inactive` si ya estaba INACTIVE. |
| `POST /api/v1/users/{id}/role-assignment` | `users.manage` | AUTHZ-001. Body: `roleCode`. 422 `invalid_role_code` si no es uno de los 3. Activa al usuario si estaba PENDING. |
| `GET /api/v1/users/{id}/effective-permissions` | `users.read` | AUTHZ-002. `{userId, roleCode, permissions[]}`; vacío si PENDING/INACTIVE o sin rol. |
| `POST /api/v1/authorization/evaluate` | — (sin auth, D6) | Body `AuthorizationEvaluationRequestV1`. Respuesta `AuthorizationDecision`. Lo consumen los demás servicios vía `HttpAuthorizationPort`. |
| `GET /api/v1/authorization/role-permission-matrix` | — (sin auth, D6) | Matriz completa rol→permiso (evidencia requerida #1). |
| `POST /api/v1/assignments/validate` | — (sin auth, D6) | ASSIGN-001/002. Body `ResponsibleAssignmentValidationRequestV1`. Respuesta `AuthorizationDecision`. Ver `IResponsibleAssignmentValidationPort`. |

`403 forbidden` (D4) para cualquier permiso denegado, siempre `ProblemDetailsV1` con `errorCode`
igual al `ReasonCode` de `AuthorizationDecision` en `Detail` (`permission_not_granted`,
`user_inactive`, `user_pending`).

**DTO mínimo de usuario** (`UserSummary`): `userId, displayName, email, status, roleCodes[]`
(0 o 1 elementos — un solo rol activo por usuario).

**Matriz rol → permiso** (confirmada sin cambios respecto a la propuesta de V2-ACL-001a):

| Permiso | Administrador | Vendedor | Responsable Comercial |
|---|:---:|:---:|:---:|
| `users.read` | ✅ | ✅ | ✅ |
| `users.manage` | ✅ | ❌ | ❌ |
| `catalogs.read` | ✅ | ✅ | ✅ |
| `catalogs.manage` | ✅ | ❌ | ❌ |
| `parties.read` | ✅ | ✅ | ✅ |
| `parties.write` | ✅ | ✅ | ✅ |
| `parties.change_commercial_status` | ✅ | ❌ | ✅ |
| `parties.assign_responsible` | ✅ | ❌ | ✅ |

**Puertos nuevos** (`RealEstateCrm.BuildingBlocks`):
- `IResponsibleAssignmentValidationPort.ValidateAsync(actorId, resourceType, resourceId, responsibleUserId, ct)`
  → `AuthorizationDecision`. **`actorId` es el `sub` de Keycloak** (misma convención que
  `ExecutionContextV1.ActorId`/`EventEnvelopeV1.ActorId` en todo el sistema);
  **`responsibleUserId` es el `userId` propio de access-service** (el mismo que devuelve
  `UserSummary`/`GetUsers`), **no** el `sub`. Son dos esquemas de id distintos a propósito —
  documentado en el XML doc del puerto y en `AccessServiceResponsibleAssignmentValidator`.
- Adapters HTTP (`BuildingBlocks.Infrastructure/Authorization/`): `HttpAuthorizationPort`,
  `HttpResponsibleAssignmentValidationPort`, con `IMemoryCache` por `(actorId, permission/
  responsibleUserId, resourceType, resourceId)`, duración configurable (`AccessService:
  CacheDuration`, default 60 s, D2). **Implica que una desactivación de usuario o un cambio de
  rol puede tardar hasta ese tiempo en reflejarse en otros servicios que cacheen la decisión**
  (`party-service` en V2-PTY-001, `platform-config-service` en V2-CAT-001).
- Registro: `services.AddAuthorizationHttpClients(configuration)` (sección `AccessService:
  BaseUrl`/`CacheDuration`). **`access-service` no se registra a sí mismo con este adapter**:
  usa `AccessServiceAuthorizationEvaluator`/`AccessServiceResponsibleAssignmentValidator`
  (evaluación local, sin loop HTTP).

**Relay de outbox reutilizable** (`BuildingBlocks.Infrastructure/Messaging/Outbox/`):
`OutboxRelayBackgroundService` + `AddOutboxRelay(configuration)` (sección `OutboxRelay:
PollingInterval`/`BatchSize`). Drena `IOutbox.GetPendingAsync` → `IEventPublisher.PublishAsync` →
`IOutbox.MarkPublishedAsync`, con su propio `IServiceScope` por ciclo (`IOutbox` es Scoped).
**Supuestos documentados en el XML doc de la clase**: asume una sola instancia del servicio
corriendo (sin lock distribuido; más de una instancia podría publicar el mismo mensaje dos veces
en la misma ventana — el broker/consumidor ya tolera duplicados vía Inbox); marca un mensaje
publicado recién **después** de que el publish devuelve exitosamente (at-least-once: un
duplicado ocasional es preferible a perder el evento si el proceso muere entre publish y mark).

**Concurrencia optimista reutilizable** (`BuildingBlocks.Infrastructure/Persistence/Mongo/
VersionedMongoRepository.cs`): misma API pública que `IRepository<TAggregate, TId>` (no la
rompe). La versión se lee por delegado (`versionSelector`), no por una interfaz que el aggregate
deba implementar (los aggregates viven en `Domain`, que no puede referenciar `BuildingBlocks`).
Convención: cada método de dominio que muta el aggregate incrementa su propio campo `Version`
antes de llamar a `UpdateAsync`; el adapter filtra por `id + (Version - 1)` y lanza
`ConcurrencyConflictException` (nueva, en `BuildingBlocks.Persistence`) si cero documentos
matchean. El Api de cada servicio la mapea a `409 Conflict`/`ErrorCodes.Conflict` en su manejador
centralizado. Usado por `UserAccountRepository`/`RoleAssignmentRepository`.

**Eventos v1** (outbox → RabbitMQ, exchange `access-service.events`, routing key = nombre del
evento): `UserCreated` (`UserCreatedV1`), `UserUpdated` (`UserUpdatedV1`), `UserDeactivated`
(`UserDeactivatedV1`), `RoleAssigned` (`RoleAssignedV1`, incluye `previousRoleCode`). **No**
publica `ResponsibleAssigned`: ver "Decisiones locales" #1. Contrato de payload
`ResponsibleAssignedV1` publicado en `contracts/Events/Access/` para que V2-PTY-001 lo use cuando
lo publique.

**Bases y colecciones Mongo** (D8): base `crm_access`, colecciones `user_accounts` (`_id` =
`UserId`, índice único adicional en `KeycloakSubject`), `role_assignments` (`_id` = `UserId`,
enforce "un solo rol" por construcción), `outbox_messages`/`inbox_consumed_messages` (heredadas
de `BuildingBlocks`).

**Catálogo de errores de dominio** (D11, `AccessErrorCodes`): `user_not_found` (404),
`user_already_exists` (409), `user_already_inactive` (409), `invalid_role_code` (422). Más
`ResponsibleAssignmentDenyReasons` (`responsible_user_not_found`, `responsible_user_inactive`,
además de `permission_not_granted` compartido con `DenyReasons`).

**Credenciales de desarrollo**: documentadas en `.env.example` y `DEVELOPMENT.md`
(`dev.administrador`/`dev.vendedor`/`dev.responsable`, password = username, deliberadamente
evidentes — nunca usar fuera de dev).

## Decisiones locales

1. **Alcance de ASSIGN-001/ASSIGN-002 (`ResponsibleAssigned`)** — decisión explícita del humano
   (dos opciones presentadas, tomó la tercera): access-service **no** implementa
   `AssignResponsible`/`ReassignResponsible` ni persiste ninguna asignación de responsable; solo
   valida (`POST /api/v1/assignments/validate`) que el actor tenga permiso y que el
   `responsibleUserId` propuesto exista y esté ACTIVE. El dato `responsibleUserId` y el evento
   real de negocio son responsabilidad del owner del recurso (`party-service`, V2-PTY-001).
   Consecuencia: el criterio de aceptación 5 ("el Responsable Comercial puede reasignar un
   registro...") y la evidencia requerida 4 ("evidencia de una reasignación") quedan
   **parciales** en esta task — la validación está probada (tests de
   `AccessServiceResponsibleAssignmentValidator`), la persistencia y el evento reales quedan
   para V2-PTY-001.
2. **Concurrencia optimista reutilizable en `BuildingBlocks.Infrastructure`, no en
   `access-service`** — pedido explícito: `VersionedMongoRepository<TAggregate, TId>` (no
   `IVersioned` como interfaz — un aggregate en `Domain` no puede implementar nada de
   `BuildingBlocks`; se usa un delegado `versionSelector`, igual que `idSelector` en
   `MongoRepository`). Sin romper `IRepository<TAggregate, TId>`.
3. **Evaluador de permisos con precedencia PENDING → INACTIVE → sin permiso** — pedido explícito.
   La caché de `HttpAuthorizationPort` (≤60 s) documentada como janela de hasta 60 s de retraso
   para que una desactivación se refleje en otros servicios.
4. **BFF: screens/mutations con los `screenId` reales de `screen-registry.ts`** — pedido
   explícito, sin inventar ids. Implementados `ADM-01`/`ADM-03`/`ADM-04` (los 3 con
   `task: "V2-ACL-001"` que son de acceso, no de invitación). Mutations (`createUser`/
   `updateUser`/`deactivateUser`/`assignRole`) sin convención previa en el repo (nada las usa
   todavía: `crm-web` sigue 100% en modo demo), nombradas por los 4 comandos de la task y
   documentadas acá.
5. **`actorId` = sub de Keycloak, `responsibleUserId`/`AssignedByUserId` = `userId` propio de
   access-service** — dos esquemas de id conviviendo a propósito en las mismas firmas (ver
   "Contratos publicados"). `ExecutionContextV1.ActorId` ya documenta "sub del token OIDC" como
   convención de todo el sistema; los DTOs de usuario (`UserSummary`, `GetUsers`) usan el
   `userId` propio de access-service como referencia pública. `AssignRoleAsync` resuelve el
   `UserAccount` del actor (por su sub) para stampear `AssignedByUserId` con su `userId`, no con
   el sub crudo.
6. **`IAuthorizationPort` con dos implementaciones**: `AccessServiceAuthorizationEvaluator`
   (local, Application layer, consume solo puertos — repositorios — no Infrastructure) para
   access-service mismo (backea también el endpoint HTTP); `HttpAuthorizationPort` (Infra, con
   caché) para los demás servicios. Evita el loop HTTP de access-service contra sí mismo.
7. **`UserAccount`/`RoleAssignment` como records inmutables**, mismo patrón que `OutboxMessage`
   de V2-FND-002 (ya probado contra Mongo real: AutoMap + `MapIdMember` sin necesitar atributos
   `MongoDB.Bson` en Domain). Métodos de dominio devuelven una nueva instancia con `Version + 1`.
8. **Endpoints internos sin `[Authorize]`** (`/authorization/evaluate`, `/authorization/
   role-permission-matrix`, `/assignments/validate`): D6 decidió explícitamente "sin client
   credentials internas en la POC". Documentado como superficie sin autenticación de red
   (aceptable en el POC de una sola instalación local, follow-up si se necesita cerrar la red
   interna).
9. **Tests de servicio bajo `services/access-service/tests/`** (no en `tests/` global): primer
   servicio con tests propios; se mantiene la convención `src/`/`tests/` dentro del propio
   servicio en vez de mezclarlos con los tests transversales (arquitectura/contratos/building
   blocks) de `tests/`.
10. **`scripts/run-slice.{sh,ps1}` (D10) no se creó**: con un solo slice real (`access-service` +
    `operations-bff`) no agrega valor sobre `dotnet run` directo; documentado en `DEVELOPMENT.md`
    como pendiente para cuando exista más de un servicio (V2-CAT-001/V2-PTY-001). No estaba en la
    write zone declarada de esta task.
11. **`ConcurrencyConflictException` vive en `BuildingBlocks/Persistence` (puertos), no en
    `BuildingBlocks.Infrastructure`** — confirmado explícitamente por el humano: así
    `AccessService.Application`/cualquier Application layer puede atraparla (la usa para mapear a
    409) sin necesitar una `ProjectReference` a `BuildingBlocks.Infrastructure`, que el test de
    arquitectura `Application_projects_do_not_reference_BuildingBlocks_Infrastructure` prohíbe.
    Verificado que `ProblemDetailsExceptionHandler` (`AccessService.Api`) la mapea a
    `409 Conflict` con `ErrorCodes.Conflict` (ver "Comandos y resultados" arriba: no hay commit
    de esta clase sin ese mapeo probado).
12. **`.env.example`/`DEVELOPMENT.md` modificados, aunque no estén en el nombre corto de la write
    zone de la sección 7.2 del plan** — requeridos por el ciclo (plan Wave 2, sección 6, ítem 8:
    "Si la task agrega usuarios/roles/catálogos al seed o al realm, documentar credenciales de
    dev"). Confirmado explícitamente por el humano antes de commitear.

## Supuestos

- El BFF no implementa el flujo interactivo completo de login (`/login`, redirect, callback):
  `AddKeycloakOpenIdConnectCookieAuthentication` ya lo configura (V2-FND-002), pero esta task no
  agregó una pantalla/endpoint de login propio — `[Authorize]` en los controllers del BFF dispara
  el challenge OIDC por defecto de ASP.NET Core. No se verificó manualmente el flujo interactivo
  completo en un browser (fuera del alcance verificable sin UI real conectada, `apps/crm-web`
  sigue en modo demo).
- La screen `ADM-04` (Permisos efectivos) del BFF requiere `?userId=` explícito: no existe hoy un
  endpoint "mis propios permisos" sin id, y `crm-web` no llama a esta screen todavía (0 call
  sites de `saveMutation`/`getScreenData` fuera de `data-source.ts` mismo). Documentado como
  simplificación deliberada.
- `GetUsersAsync`/`GetUsers` resuelve el rol de cada usuario con una consulta extra por usuario
  (`RoleAssignments.GetByIdAsync`), N+1 aceptable a la escala de esta POC (instalación única, no
  miles de usuarios).

## Follow-ups

- **V2-PTY-001**: implementar `AssignPartyResponsible`, persistir `responsibleUserId` en `Party`,
  llamar a `IResponsibleAssignmentValidationPort` (fake en tests, adapter HTTP real después de
  mergear esta task) antes de mutar, y publicar `ResponsibleAssignedV1` (contrato ya definido acá)
  con su propio `EventEnvelopeV1`. Cierra el criterio de aceptación 5 y la evidencia 4 que esta
  task deja parciales.
- **V2-CAT-001**: usar `VersionedMongoRepository` para `CatalogEntry` (concurrencia optimista al
  publicar versión de catálogo) y `HttpAuthorizationPort`/`AddAuthorizationHttpClients` en vez de
  `FakeAuthorizationPort` una vez que esta task esté mergeada.
- Si una wave futura corre más de una instancia de un servicio, revisar el supuesto de instancia
  única de `OutboxRelayBackgroundService` (agregar lock distribuido o partición si hace falta).
- `scripts/run-slice.{sh,ps1}` (D10): crearlo cuando exista más de un servicio real para levantar.

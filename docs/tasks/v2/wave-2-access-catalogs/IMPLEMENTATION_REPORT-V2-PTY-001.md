# Implementation report — V2-PTY-001 — Party, Empresa, Contacto, relaciones y estados

## Decisiones del equipo (tomadas con el humano antes de escribir código)

1. **Traducción `sub` → `userId` (A1, con ajuste).** `ExecutionContextV1.ActorId` es el `sub` de
   Keycloak, pero `responsibleUserId` (validador de asignación, eventos) es el `userId` propio de
   access-service, y party-service no tenía cómo pasar de uno al otro (necesario para la regla de
   propiedad, el bypass del Administrador y el responsable inicial). Se resolvió con
   `GET /api/v1/users/me` en access-service (resuelve desde el token, no expone búsqueda por `sub`
   de terceros) en vez del `GET /users/by-subject/{sub}` propuesto. Autorizado tocar
   `services/access-service` y `building-blocks` **solo para esto y de forma aditiva**: ningún
   endpoint, DTO ni port existente se modificó (ver "Archivos": los únicos cambios a archivos
   existentes de access-service/building-blocks son una línea de registro DI en
   `AccessServiceInfrastructureServiceCollectionExtensions`).
2. **Propiedad (B).** Se mantiene la matriz de ACL-001a. El Administrador edita todo; Vendedor y
   Responsable Comercial editan solo las parties donde son `responsibleUserId`. **Corrección
   posterior del equipo:** `parties.change_commercial_status` y `parties.assign_responsible`
   dependen SOLO del permiso (Administrador y Responsable Comercial), sin regla de propiedad; la
   propiedad aplica únicamente a `parties.write` (UpdateCompany, UpdateContact,
   RelateContactToCompany).
3. **Captura mínima (C) — desvío explícito de la task.** La task pide "nombre + un dato de
   contacto"; el formulario real de `crm-web` (`PTY-02`/`03`/`04`, `save()` en `crm-workspace.tsx`)
   exige **solo el nombre**. Se alineó con `crm-web` (D5: no se toca el front): `displayName` es lo
   único obligatorio; teléfono y email son opcionales (un email presente debe tener formato válido;
   una cadena vacía cuenta como ausente).

## UCs cubiertos

- **PTY-001** — `POST /api/v1/companies`: Party `LEGAL_ENTITY`, `ACTIVE + POTENTIAL`, responsable =
  `userId` del creador. Evento `PartyRegistered`.
- **PTY-002** — `PUT /api/v1/companies/{id}`: misma identidad (mismo `partyId`), `Version + 1`
  con concurrencia optimista (conflicto → 409). Evento `PartyUpdated` solo con **nombres** de campos
  cambiados; una edición sin cambios no persiste ni publica.
- **PTY-003** — `POST /api/v1/contacts`: Party `NATURAL_PERSON`. Puede existir sin Empresa.
- **PTY-004** — `PUT /api/v1/contacts/{id}`: conserva sus relaciones (test explícito).
- **PTY-005** — `POST /api/v1/contacts/{id}/relationships` (`PartyRelationship`, `CONTACT_OF` por
  defecto o `REPRESENTS`). Un contacto puede tener varias empresas y ninguna. Visible en ambos
  detalles. Evento `PartyRelationshipCreated`.
- **PTY-006** — `GET /api/v1/companies/{id}`, `GET /api/v1/contacts/{id}` (+ `GET /api/v1/parties/{id}`
  genérico) y `GET /api/v1/parties` (SearchParties paginado). El detalle incluye responsable, origen
  (+ `originCatalogVersion`), las dos dimensiones de estado por separado y las relaciones vigentes.
- **PTY-007** — `POST /api/v1/parties/{id}/commercial-status` (los 4 estados; la baja lógica es
  `INACTIVE`, la reactivación un cambio explícito a otro estado; guarda actor y fecha en
  `commercialStatusChangedBy/At`) y `POST /api/v1/parties/{id}/responsible` (evidencia end-to-end de
  ASSIGN-001/002: publica `PartyResponsibleAssigned` con `ResponsibleAssignedV1`, cerrando lo que
  V2-ACL-001 dejó parcial).
- **BFF** — ramas nuevas en `ScreensController`/`MutationsController` existentes (sin reestructurar,
  sin controllers nuevos en la misma ruta): screens `PTY-01`, `PTY-05`, `PTY-06`, `PTY-07`, `PTY-08`,
  `PTY-09`, `PTY-10`, `PTY-11`, `PTY-13`, `GLB-11`; mutations `createCompany`, `updateCompany`,
  `createContact`, `updateContact`, `relateContactToCompany`, `changePartyCommercialStatus`,
  `assignPartyResponsible`. `PTY-02/03/04` son formularios de alta (no leen datos; usan las
  mutations) y `PTY-12` (Historial) es de ACT. `PTY-14` es F2 (diferida).

## Archivos

**`contracts/RealEstateCrm.Contracts/`** (nuevo, aditivo): `Users/UserSelfV1.cs`,
`Parties/PartyVocabulary.cs` (`PartyKinds`, `IdentityStatuses`, `CommercialStatuses`,
`RelationshipTypes`), `Parties/PartyDtosV1.cs` (`PartySummaryV1`, `PartyRelationshipV1`,
`PartyDetailV1`), `Events/Party/PartyEventsV1.cs`.

**`building-blocks/`** (aditivo, autorizado): `RealEstateCrm.BuildingBlocks/Authorization/IUserDirectoryPort.cs`
(+ `UserSelfResult`); `BuildingBlocks.Infrastructure/Authorization/HttpUserDirectoryPort.cs`,
`UserDirectoryClientServiceCollectionExtensions.cs`. **No se tocó** ningún archivo existente de
`Authorization/` (ni `HttpAuthorizationPort`, ni el validador, ni `AuthorizationClientServiceCollectionExtensions`).

**`services/access-service/`** (aditivo, autorizado): `Application/Users/UserSelfService.cs`,
`Api/Users/UserSelfController.cs` (controller propio, ruta `api/v1/users/me`, no se agregó nada a
`UsersController`), una línea `services.AddScoped<UserSelfService>()` en
`AccessServiceInfrastructureServiceCollectionExtensions.cs`. Tests: `UserSelfServiceTests` (6) y
`UsersMeEndpointTests` (7, Mongo/RabbitMQ/Keycloak reales).

**`services/party-service/src/`**: `Domain/{Party,PartyRelationship}.cs` (cero `ProjectReference`);
`Application/{PartyDomainException.cs,Ports/IPartyReadPort.cs,Parties/PartyManagementService.cs,Parties/PartyWire.cs}`;
`Infrastructure/{PartyServiceInfrastructureServiceCollectionExtensions.cs,Persistence/Mongo/*}`
(class maps, `PartyRepository` sobre `VersionedMongoRepository`, `PartyRelationshipRepository`,
`PartyReadRepository`, `PartyServiceIndexInitializer`); `Api/{Program.cs,appsettings.json,Parties/*,
ExecutionContextResolution/*,ErrorHandling/*,Http/BearerTokenRelayHandler.cs}`; los `.csproj` de
Application/Infrastructure ganaron sus referencias a contracts/building-blocks (mismo patrón que
access-service).

**`bffs/operations-bff/src/OperationsBff.Api/`**: `PartyService/PartyServiceClient.cs` (nuevo, token
relay D6), ramas nuevas en `Screens/ScreensController.cs` y `Mutations/MutationsController.cs`, una
línea de registro del `HttpClient<PartyServiceClient>` en `Program.cs`, `PartyService:BaseUrl` en
`appsettings.json`.

**Tests**: `services/party-service/tests/PartyService.Application.Tests` (59: dominio + servicio con
fakes, incluye propiedad, estados, relaciones, origen, responsable, búsqueda, sin borrado),
`services/party-service/tests/PartyService.Api.Tests` (3 del relay de Bearer sin infra + 12 e2e
`RequiresMongo|RequiresRabbitMq|RequiresKeycloak`), `tests/RealEstateCrm.ContractTests` (`UserSelfV1` y
Party, 3 + 5 tests), `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests/Authorization/HttpUserDirectoryPortTests`
(10), `tests/RealEstateCrm.TestSupport` (`FakeUserDirectoryPort`, `Catalogs/FakeCatalogReaderPort`).
Modificados: `RealEstateCrm.slnx` (2 proyectos de test), `DEVELOPMENT.md` y `.env.example` (sección
del slice; **sin credenciales nuevas**: no hay seed ni cambios al realm).

No se tocó `apps/crm-web`, `docs/adr`, `README.md`/`ARCHITECTURE.md`/`AGENTS.md`, ni el board.

## Comandos y resultados

```text
$ bash scripts/test-integration.sh        (en main, ANTES de cualquier cambio)
→ verde (BuildingBlocks.Infrastructure.Tests 14/14, AccessService.Api.Tests 2/2, PlatformConfigService.Api.Tests 2/2)

$ bash scripts/test-fast.sh
==> dotnet build         → 0 advertencias, 0 errores
==> dotnet test          → sin infra
    RealEstateCrm.ArchitectureTests            3/3
    AccessService.Application.Tests           52/52  (46 previos + 6 nuevos)
    RealEstateCrm.ContractTests               88/88  (80 previos + 8 nuevos)
    RealEstateCrm.BuildingBlocks.Infrastructure.Tests 65/65  (55 previos + 10 nuevos)
    PlatformConfigService.Application.Tests   19/19
    PartyService.Application.Tests            59/59  (nuevo)
    PartyService.Api.Tests                     3/3   (nuevo: BearerTokenRelayHandler)
==> apps/scripts/build-webs.sh → crm-web y platform-admin-web compilan

$ bash scripts/test-integration.sh        (Docker real: mongo, rabbitmq, keycloak)
    RealEstateCrm.BuildingBlocks.Infrastructure.Tests 14/14
    AccessService.Api.Tests                    9/9   (2 previos + 7 nuevos de /users/me)
    PlatformConfigService.Api.Tests            2/2
    PartyService.Api.Tests                    12/12  (nuevo)
```

Una primera corrida de integración falló y **no se maquilló**: reveló que `AddMongoPersistence` lee
`Mongo:DatabaseName` de forma eager al armar `Program.cs`, por lo que un
`ConfigureAppConfiguration` de test **no** cambia la base (ver "Hallazgos"). Los tests nuevos usan
`UseSetting`, que sí aísla la base.

## Evidencia requerida de la task

1. Flujo Empresa → Contacto → detalle de ambos: `Company_then_contact_then_relationship_then_detail_of_both`.
2. Contacto individual: `An_individual_contact_needs_no_company`.
3. DO_NOT_CONTACT sin borrado: `DO_NOT_CONTACT_keeps_the_party_and_is_independent_from_the_identity_status`
   (e2e) y `DO_NOT_CONTACT_does_not_delete_nor_hide_the_party_and_keeps_identityStatus_ACTIVE`.
4. Independencia ACTIVE+POTENTIAL vs ACTIVE+DO_NOT_CONTACT: `Both_dimensions_are_independent_...` y
   `ChangeCommercialStatus_leaves_identityStatus_untouched_and_stamps_actor_and_date`.
5. Baja lógica con actor y fecha: `Logical_deactivation_request_records_actor_and_date_and_the_party_is_not_removed`
   (e2e: `POST .../commercial-status {"commercialStatus":"INACTIVE"}` → `commercialStatusChangedBy` =
   `sub` del Administrador, `commercialStatusChangedAt` reciente; el `DELETE` no existe → 405).
6. Reasignación end-to-end (ASSIGN-001/002, cierra lo parcial de V2-ACL-001):
   `Reassigning_the_responsible_is_published_end_to_end_with_the_original_correlation_id` — request
   real del Responsable Comercial → outbox → RabbitMQ real → consumidor real; el evento trae
   previous/new/`assignedByUserId` (userId), `actorId` (sub) y el `correlationId` del header. El test
   espera **por polling con timeout** tanto al consumidor como a que el outbox marque publicado, sin
   asumir orden entre ambos.

## Contratos publicados

### Contrato nuevo de access-service: `GET /api/v1/users/me`

Resuelve el usuario de negocio **del propio token** (sin parámetros: no acepta ningún id, no expone
búsqueda por `sub` de terceros). `[Authorize]`, sin permiso de negocio (todo usuario puede saber quién
es). Respuesta `200` `UserSelfV1(userId, status, roleCode)`:

```json
{ "userId": "…guid de access-service…", "status": "Active", "roleCode": "Vendedor" }
```

- `userId` = id propio de access-service (el mismo de `GET /api/v1/users`), **no** el `sub`.
- `status` usa el mismo texto que `UserSummary.Status` (`Active`); solo se responde 200 con `Active`.
- `roleCode`: `Administrador` | `Vendedor` | `Responsable Comercial`; ausente si el usuario está activo sin rol.
- Usuario `PENDING` → `403` `ProblemDetailsV1` (`errorCode: forbidden`, `detail: user_pending`);
  `INACTIVE` → `403` con `detail: user_inactive` (los mismos `reasonCode` de la autorización).
  Un `sub` desconocido se auto-provisiona `PENDING` (D3) y recibe `user_pending`.
- Port: `IUserDirectoryPort.GetSelfAsync(actorId, ct)` → `UserSelfResult(User?, DenyReasonCode?)`
  (`RealEstateCrm.BuildingBlocks.Authorization`); adapter `HttpUserDirectoryPort`
  (`BuildingBlocks.Infrastructure/Authorization`), token relay del Bearer de la request actual (D6) y
  caché `IMemoryCache` de 60 s **por `sub`** (`AccessService:CacheDuration`), que guarda también los
  403. Registro: `services.AddUserDirectoryHttpClient(configuration)`. Sin Bearer en la request
  falla con `InvalidOperationException` (no llama sin credenciales). Fake: `FakeUserDirectoryPort` (TestSupport).
- **Consecuencia de la caché:** un cambio de rol, una desactivación o una activación puede tardar
  hasta 60 s en reflejarse en party-service (la misma ventana ya documentada para `HttpAuthorizationPort`).

### Endpoints REST v1 de party-service (D1, `[ApiController]` + `ProblemDetailsV1`, puerto 5155)

| Método y ruta | Permiso (`IAuthorizationPort`) | Regla de propiedad (party-service, D2) |
|---|---|---|
| `GET /api/v1/parties?q=&kind=&commercialStatus=&responsibleUserId=&page=&pageSize=` | `parties.read` | — (`pageSize` 1..100) |
| `GET /api/v1/parties/{id}` · `GET /api/v1/companies/{id}` · `GET /api/v1/contacts/{id}` | `parties.read` | — |
| `POST /api/v1/companies` · `POST /api/v1/contacts` | `parties.write` | responsable inicial = `userId` del creador |
| `PUT /api/v1/companies/{id}` · `PUT /api/v1/contacts/{id}` | `parties.write` | Administrador, o `responsibleUserId == userId` del actor |
| `POST /api/v1/contacts/{id}/relationships` `{companyId, relationshipType?}` | `parties.write` | igual que editar, sobre el **contacto** |
| `POST /api/v1/parties/{id}/commercial-status` `{commercialStatus}` | `parties.change_commercial_status` | ninguna: solo el permiso (Administrador y Responsable Comercial) |
| `POST /api/v1/parties/{id}/responsible` `{responsibleUserId}` | `parties.assign_responsible` | ninguna: valida access-service (`IResponsibleAssignmentValidationPort`) |

Body de alta/edición (`PartyDataRequest`): `displayName` (obligatorio, ≤200), `legalName`,
`givenNames`, `familyNames`, `taxIdentifier`, `identityDocument`, `email`, `phone`, `address`,
`industry`, `notes`, `originCode` (todos opcionales). Empresa ignora `givenNames/familyNames/identityDocument`;
Contacto ignora `legalName/industry`. Respuestas: `PartyDetailV1`, `PartySummaryV1` en `PagedResult`,
`PartyRelationshipV1` (201). Vocabulario: `kind` `LEGAL_ENTITY|NATURAL_PERSON`; `identityStatus`
`PROVISIONAL|ACTIVE|ALIASED|INACTIVE|RESTRICTED` (V2 solo produce `ACTIVE`; nunca `ALIASED`);
`commercialStatus` `POTENTIAL|CUSTOMER|INACTIVE|DO_NOT_CONTACT`; `relationshipType` `CONTACT_OF|REPRESENTS`.
No existe ningún endpoint de borrado. Los detalles muestran solo relaciones vigentes.

`403 forbidden` (D4) para permiso denegado (`detail` = `reasonCode`: `permission_not_granted`,
`user_pending`, `user_inactive`) y para propiedad (`detail: party_not_responsible`); `404` para party
inexistente **o de otro tipo** (una Empresa pedida como Contacto).

### Eventos v1 (outbox → RabbitMQ, exchange `party-service.events`, routing key = nombre del evento)

| Evento | Payload | Cuándo |
|---|---|---|
| `PartyRegistered` | `PartyRegisteredV1(partyId, kind, displayName, identityStatus, commercialStatus, responsibleUserId?, originCode?, originCatalogVersion?)` | alta |
| `PartyUpdated` | `PartyUpdatedV1(partyId, kind, displayName, changedFields[])` — solo nombres de campos | edición con cambios |
| `PartyRelationshipCreated` | `PartyRelationshipCreatedV1(relationshipId, fromPartyId, toPartyId, relationshipType, validFrom)` | relación |
| `PartyCommercialStatusChanged` | `PartyCommercialStatusChangedV1(partyId, previousStatus, newStatus)` | cambio de estado (incluye baja/reactivación) |
| `PartyResponsibleAssigned` | `ResponsibleAssignedV1(resourceType="party", resourceId, previousResponsibleUserId?, newResponsibleUserId, assignedByUserId)` | asignación/reasignación |

Ningún payload lleva CUIT, documento, email, teléfono, dirección ni notas (contract test). `actorId`
del envelope = `sub`; `assignedByUserId`/`responsibleUserId` = `userId` de access-service.
`PartyIdentityStatusChanged` **no** se publica: V2 no cambia el ciclo técnico.

**Nombre del evento de reasignación (decisión confirmada):** se publica `PartyResponsibleAssigned`
(no `ResponsibleAssigned`, como lo nombraban los textos de ACL-001) con payload `ResponsibleAssignedV1`
(`resourceType = "party"`). Los consumidores deben suscribirse por ese nombre de evento.

### Permisos consumidos
`parties.read`, `parties.write`, `parties.change_commercial_status`, `parties.assign_responsible`
(`Permissions`, sin cambios; matriz de ACL-001 sin cambios).

### Códigos de error de dominio (D11, `PartyErrorCodes`)
`party_not_found` (404), `party_kind_mismatch` (reservado, hoy un tipo distinto sale como 404),
`party_invalid_origin` (422), `party_invalid_commercial_status` (422),
`party_invalid_relationship_type` (422), `party_relationship_already_exists` (409),
`party_commercial_status_unchanged` (409), `party_responsible_unchanged` (409),
`party_responsible_invalid` (422; `detail` = `responsible_user_not_found|responsible_user_inactive`),
más los genéricos `validation_error` (400), `forbidden` (403), `conflict` (409, concurrencia
optimista) y `dependency_unavailable` (503, access-service/platform-config-service caído).

### Persistencia (D8)
Base `crm_party`: `parties` (`_id` = `partyId`, `version` para concurrencia optimista, `Profile`
embebido, `IdentityStatus` y `CommercialStatus` como campos separados en texto), `party_relationships`,
`outbox_messages`, `inbox_consumed_messages`. Índices **solo de búsqueda** (nombre, email, teléfono,
CUIT, responsable; relaciones por extremo): ninguno `Unique` (test e2e lo verifica).

### BFF
Ver "UCs cubiertos". Query params de screens: `entityId` (partyId), `q`, `kind`, `commercialStatus`,
`responsibleUserId`, `page`, `pageSize`. Los `partyId` viajan en el payload de las mutations
(`partyId`, o `contactId` en `relateContactToCompany`).

## Decisiones locales

1. **Nombre de evento (confirmado por el equipo):** se mantiene `PartyResponsibleAssigned` con
   payload `ResponsibleAssignedV1`. Los textos de ACL-001 lo llamaban `ResponsibleAssigned`; el
   nombre vigente es el de la task de PTY.
2. **Propiedad solo en `parties.write` (corrección del equipo, aplicada en un commit posterior).**
   La primera versión exigía además propiedad para cambiar el estado comercial (lectura de la
   columna "con propiedad" de la matriz de ACL-001a). El equipo aclaró que
   `parties.change_commercial_status` y `parties.assign_responsible` dependen solo del permiso; el
   código ya no consulta al responsable en esos dos commands (tests actualizados).
3. **Relacionar exige propiedad solo sobre el Contacto**, no sobre la Empresa (la operación modifica
   la ficha del contacto). El Vendedor puede relacionar su contacto con una empresa ajena.
4. **Responsable inicial = `userId` del creador** (también si crea un Administrador o un Responsable).
5. **Rol Administrador = literal `"Administrador"`** (`PartyManagementService`): el `RoleCode` vive en
   `AccessService.Domain`, que party-service no puede referenciar. Follow-up: publicar los códigos de
   rol en `contracts`.
6. **`status` de `/users/me` = `"Active"`** (`ToString()` del enum, igual que `UserSummary`), no
   `ACTIVE` como sugería el pedido: se priorizó consistencia con el contrato ya publicado.
7. **Auditoría de actor/fecha:** `createdBy/updatedBy/commercialStatusChangedBy` guardan el `sub`
   (misma convención que `actorId` de los eventos). El actor de la asignación de responsable viaja
   como `userId` en el evento (`assignedByUserId`).
8. **Edición sin cambios = no-op** (sin `Version + 1` ni evento).
9. **`GET /api/v1/parties/{id}` genérico** agregado para las screens `PTY-07..13`, que operan sobre
   una Party de cualquier tipo sin que el BFF sepa cuál. `GLB-11` (contenido archivado) se resuelve
   como la búsqueda con `commercialStatus=INACTIVE`.
10. **Sin seed de desarrollo de parties** (D9 no lo pide; las parties se crean por API/BFF).
11. **`originCode` se revalida solo si cambia**; un origen dado de baja después no invalida un
    registro histórico, y se conserva su `catalogVersion` original.
12. **`scripts/run-slice.{sh,ps1}` (D10) no se creó** (ya hay 3 servicios + BFF; sigue fuera de la
    write zone de esta task, ver Follow-ups).
13. **Fuera de alcance (no implementado):** cierre/`validTo` de una relación (el campo existe, sin
    comando), reactivación de identidad, `PartyIdentityStatusChanged`, Identity Resolution, dedupe.

## Hallazgos (defectos previos encontrados; no se corrigieron fuera de la write zone)

1. **`HttpCatalogReaderPort` (V2-CAT-001) no reenvía credenciales, pero `GET /api/v1/catalogs/...`
   exige JWT (`[Authorize]`).** Contra platform-config-service real devolvería `401` y party-service
   fallaría al validar `originCode`. Nunca se había ejercitado (los tests de CAT no lo llaman).
   **Workaround local, sin tocar el adapter compartido:** `BearerTokenRelayHandler` en
   `PartyService.Api`, adjunto al mismo cliente tipado (`AddHttpClient<ICatalogReaderPort,
   HttpCatalogReaderPort>().AddHttpMessageHandler<...>()`), con 3 tests. Corrección de fondo
   (relay en el adapter compartido, o `[AllowAnonymous]` de lectura interna como en access-service)
   queda como follow-up de CAT.
2. **`AddMongoPersistence` lee `Mongo:DatabaseName` de forma eager**, así que los
   `ConfigureAppConfiguration` de los tests de ACL-001 y CAT-001 **no** aíslan la base: esos tests
   escriben en `crm_access`/`crm_platform_config` y su `DropDatabase` borra una base inexistente.
   Dos clases de test arrancando el seed de access-service en paralelo chocaron con
   `E11000 duplicate key` (visto en la primera corrida). Los tests de esta task usan
   `UseSetting("Mongo:DatabaseName", …)`, que sí aísla. Follow-up: migrar los tests existentes.

## Supuestos y límites de lo verificado

- **El camino party-service → access-service / platform-config-service por HTTP real NO se ejercita
  end-to-end**: `scripts/test-integration.sh` solo levanta Mongo, RabbitMQ y Keycloak (D10). En los
  tests e2e esas dependencias se sustituyen por los doubles de TestSupport, con la matriz rol→permiso
  copiada de ACL-001. Sí están probados por separado: el adapter `HttpUserDirectoryPort` (stub HTTP,
  10 tests), el endpoint `/users/me` real contra Mongo+Keycloak (7 tests) y el relay de Bearer (3 tests).
  Falta una corrida manual con los 4 procesos `dotnet run` (DEVELOPMENT.md).
- **Las screens y mutations del BFF no tienen tests automáticos** (no existe un proyecto de test del
  BFF; ACL-001 y CAT-001 tampoco los tienen). Solo se verificó que compilan.
- `crm-web` sigue en modo demo: los criterios de "pantallas distintas" y "etiquetas claras" se
  cumplen a nivel de contrato de datos, no de UI conectada.
- Los `partyId` de la UI de demo son strings tipo `contact-123`; el backend usa `Guid` (D1 FND-002).
  Conectar el front (D5) requiere adaptar ese id en `data-source.ts`, fuera de esta task.

## Follow-ups

- ✅ **RESUELTO (ver "Fix post-PTY-001")** — **PR "fix" antes de Wave 3** (acordado): (1) mover `BearerTokenRelayHandler` a
  `BuildingBlocks.Infrastructure` y usarlo en `HttpCatalogReaderPort` (Hallazgo 1), y (2) aislar la
  base Mongo en los tests de ACL/CAT con `UseSetting` (Hallazgo 2). Follow-ups 1 y 2: resueltos.
- Publicar los códigos de rol en `contracts` para no hardcodear `"Administrador"`.
- `scripts/run-slice.{sh,ps1}` (D10) y una corrida manual con los servicios reales levantados.
- V2-ANA-001 compone la vista 360 sobre estos detalles; V2-PRP/DMD referencian `partyId`.
- Tests del BFF si una wave los adopta.

## Fix post-PTY-001

PR de fix (no es una task nueva) que resuelve los follow-ups 1 y 2 y los Hallazgos 1 y 2.

### Qué cambió

**Fix 1 — Token relay (Hallazgo 1, follow-up 1)**
- `BearerTokenRelayHandler` se movió de `PartyService.Api` a
  `BuildingBlocks.Infrastructure` (`Infrastructure/Http`, namespace `...Infrastructure.Http`); la copia
  local y su carpeta se borraron. Es el **único** mecanismo de relay: `HttpUserDirectoryPort` tenía uno
  inline (leía `HttpContext` y armaba el header) y ahora delega en el handler; conserva solo el guard que
  lanza `InvalidOperationException` si no hay Bearer en la request actual.
- El handler agrega `Authorization` solo si hay un Bearer entrante; sin request en curso o sin header
  **no lanza** y la llamada sale sin credenciales (el cliente de catálogos se puede usar fuera de un
  request). No pisa un `Authorization` ya presente.
- `AddCatalogHttpClient` y `AddUserDirectoryHttpClient` adjuntan el handler vía
  `AddBearerTokenRelay()`, que registra el handler como transient y `AddHttpContextAccessor()`; el
  `Program.cs` de cada servicio no tiene que acordarse de nada. En party-service se eliminó el
  `AddHttpClient<ICatalogReaderPort, ...>` duplicado.
- **`HttpAuthorizationPort` y `HttpResponsibleAssignmentValidationPort` no reenvían token a propósito
  (D6):** los endpoints de access-service que consumen no llevan `[Authorize]`. Solo los endpoints con
  JWT (`GET /api/v1/catalogs/...`, `GET /api/v1/users/me`) necesitan relay.
- Sin cambios de firma en `ICatalogReaderPort` ni en DTOs de contracts.

**Fix 2 — Aislamiento de Mongo (Hallazgo 2, follow-up 2)**
- `CorrelationIdAndAuthorizationEndToEndTests` (access-service) y `CatalogsAuthorizationEndToEndTests`
  (platform-config-service) ahora usan `UseSetting("Mongo:DatabaseName", <base con sufijo Guid>)`, el
  mismo patrón que PTY-001 y `UsersMeEndpointTests`, y dropean su base al terminar.
  `AddMongoPersistence` no se modificó.

**Test flaky corregido en el camino (party-service)**
- `Reassigning_the_responsible_is_published_end_to_end_...` fallaba (4 de 5 corridas) porque el listener
  de RabbitMQ tomaba el `PartyResponsibleAssigned` de *otra* party (otros tests de la clase también
  reasignan). Se agregó `match` por `ResourceId == contact.PartyId`, como ya hacía el test de
  `PartyRegistered`. Solo cambia el test; sin cambios de código productivo.

### Cómo se verificó
- Tests nuevos/movidos en `BuildingBlocks.Infrastructure.Tests`: los 3 del handler (movidos desde
  party-service) + uno sin request en curso; dos tests de `HttpCatalogReaderPort` registrado vía
  `AddCatalogHttpClient` contra un servidor fake (el header `Authorization` llega; fuera de un request
  no falla y no envía header). `HttpUserDirectoryPortTests` pasó a armar la cadena con el handler real.
- `bash scripts/test-fast.sh` y `bash scripts/test-integration.sh`: dos corridas consecutivas de cada uno
  en verde (fast: 292 tests; integration: 37 tests). La primera tanda de integration había fallado por
  el test flaky de arriba; se corrigió y se repitió el ciclo completo.

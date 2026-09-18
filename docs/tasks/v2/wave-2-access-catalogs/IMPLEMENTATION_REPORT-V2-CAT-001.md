# Implementation report — V2-CAT-001 — Catálogos comerciales obligatorios y defaults inmobiliarios

## UCs cubiertos

- **CAT-001** — `CommercialStage`: CRUD + `order` + `pipelineKind` (DEMAND/SUPPLY) +
  `semanticState` (OPEN/WON/LOST, protegido: no editable). Seed reproducible con las 8 etapas
  DEMAND y las 6 SUPPLY literales de la task.
- **CAT-002** — `ActivityType`: CRUD genérico (sin `order`/`pipelineKind`/`semanticState`). Seed
  con los 9 tipos literales.
- **CAT-003** — `CommercialOrigin`: CRUD genérico. Seed con los 8 orígenes literales, incluido
  "Portal inmobiliario" como origen manual (sin integración con portales).
- **CAT-004** — `LossReason`: CRUD genérico. Seed con los 9 motivos literales.
- **CAT-005** — `OperationType`/`PropertyType`: mismo CRUD genérico. Seed con los 4 tipos de
  operación y los 8 tipos de inmueble del plan maestro §5.
- **CAT-006** — `PublishCatalogVersion` (contador incremental por `catalogType`, decisión del
  equipo — ver "Decisiones locales" #1) + seed que deja la versión 1 publicada de los seis
  catalogTypes al arrancar en `Development`.

## Archivos

Write zone declarada (`services/platform-config-service/**`, archivos nuevos en
`bffs/operations-bff/` para screens de catálogos, tests) **más** el adapter cliente de catálogos
en `BuildingBlocks.Infrastructure/Catalogs/` (entregable explícito de la task, D7) y las
constantes compartidas de catálogo en `contracts/` (mismo criterio que `Permissions`/
`ResourceTypes` de V2-ACL-001a: las consume tanto el owner como sus consumidores).

**`contracts/RealEstateCrm.Contracts/`**:
- `Catalogs/{CatalogTypes,PipelineKinds,SemanticStates,CatalogEntryV1,CatalogQueryResultV1}.cs`
  (5 archivos): constantes compartidas + DTOs de la respuesta de catálogo (una sola forma para la
  screen administrativa del BFF y para el cliente HTTP de otros servicios, D7).
- `Events/PlatformConfig/CatalogEventsV1.cs`: `CatalogEntryCreatedV1`, `CatalogEntryUpdatedV1`,
  `CatalogEntryDeactivatedV1`, `CatalogVersionPublishedV1`.

**`building-blocks/RealEstateCrm.BuildingBlocks/`** (puertos, sin dependencias de infraestructura):
- `Catalogs/ICatalogReaderPort.cs`.

**`building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/`**:
- `Catalogs/{CatalogClientOptions,HttpCatalogReaderPort,CatalogClientServiceCollectionExtensions}.cs`
  — adapter HTTP con `IMemoryCache` ≤60 s (D7/D2), mismo patrón que `HttpAuthorizationPort`
  (V2-ACL-001). No se tocó ningún archivo existente de `Authorization/`.

**`services/platform-config-service/src/`**:
- `PlatformConfigService.Domain/`: `CatalogEntry.cs` (aggregate único discriminado por
  `catalogType`, ver "Decisiones locales" #2), `CatalogTypeIdentity.cs` (deriva un `Guid`
  determinístico del `catalogType` para `EventEnvelopeV1.AggregateId` de
  `CatalogVersionPublished`, que no tiene una `CatalogEntry` propia). Cero `ProjectReference`
  (verificado por `RealEstateCrm.ArchitectureTests`).
- `PlatformConfigService.Application/`: `PlatformConfigDomainException.cs` (+
  `PlatformConfigErrorCodes`), `Ports/{ICatalogEntryReadPort,ICatalogVersionPort}.cs`,
  `Catalogs/CatalogService.cs`. Referencia `RealEstateCrm.BuildingBlocks` (puertos) y
  `RealEstateCrm.Contracts` — no `BuildingBlocks.Infrastructure` (test de arquitectura lo
  verifica).
- `PlatformConfigService.Infrastructure/`: `Persistence/Mongo/{PlatformConfigServiceMongoClassMapBootstrap,
  CatalogEntryRepository,CatalogEntryReadRepository,CatalogVersionRepository,
  PlatformConfigServiceIndexInitializer}.cs`, `Seeding/{DevSeedCatalogEntries,
  PlatformConfigServiceDevSeedHostedService}.cs`,
  `PlatformConfigServiceInfrastructureServiceCollectionExtensions.cs`.
- `PlatformConfigService.Api/`: `Program.cs` (reescrito, wire-up real),
  `appsettings.json` (Mongo/RabbitMq/OutboxRelay/Keycloak/AccessService),
  `Catalogs/{CatalogRequests,CatalogsController}.cs`,
  `ExecutionContextResolution/CurrentExecutionContextProvider.cs`,
  `ErrorHandling/{ProblemDetailsExceptionHandler,ProblemDetailsResults}.cs`.

**`bffs/operations-bff/src/OperationsBff.Api/`**: `PlatformConfigService/PlatformConfigServiceClient.cs`
(nuevo, mismo patrón que `AccessServiceClient`). **Modificados** (no reestructurados, ver
"Decisiones locales" #3): `Screens/ScreensController.cs` (agregadas ramas `ADM-05`..`ADM-12`),
`Mutations/MutationsController.cs` (agregados `createCatalogEntry`/`updateCatalogEntry`/
`deactivateCatalogEntry`/`publishCatalogVersion`), `Program.cs` (una línea de registro del
`HttpClient<PlatformConfigServiceClient>`, después de la de `AccessServiceClient` — el wire-up
base de auth/health/observability es el mismo que dejó V2-ACL-001, sin tocarlo), `appsettings.json`
(`PlatformConfigService:BaseUrl`).

**Tests** (nuevos, 41 tests: 19 Application + 2 Api + 20 contratos/infra transversales):
- `services/platform-config-service/tests/PlatformConfigService.Application.Tests/` (proyecto
  nuevo, 19 tests): `Domain/CatalogEntryTests.cs` (4), `Catalogs/CatalogServiceTests.cs` (15:
  alta genérica y de etapa, catalogType inválido, campos de etapa inválidos/faltantes, código
  duplicado dentro y entre catalogTypes, edición, 404, baja lógica simple y doble, `activeOnly`,
  publicación de versión sin mutar entradas, `catalogVersion` reportado, `?version=` obsoleto).
- `services/platform-config-service/tests/PlatformConfigService.Api.Tests/` (proyecto nuevo, 2
  tests, `RequiresMongo|RequiresRabbitMq|RequiresKeycloak`): `CatalogsAuthorizationEndToEndTests.cs`
  — ver "Decisiones locales" #4 sobre por qué usa `FakeAuthorizationPort` en vez de
  `HttpAuthorizationPort` real.
- `tests/RealEstateCrm.TestSupport/` no se tocó: `CatalogServiceTestHarness`/
  `FakeCatalogEntryReadPort`/`InMemoryCatalogVersionPort` viven en el propio proyecto de tests de
  Application (como `FakeUserAccountReadPort` en V2-ACL-001), no en el paquete transversal —
  `ICatalogVersionPort` no lo va a reutilizar ningún otro servicio.
- `tests/RealEstateCrm.ContractTests/`: `Catalogs/{CatalogTypesContractTests,
  CatalogEntryV1ContractTests}.cs` (16), `Events/PlatformConfig/CatalogEventsV1ContractTests.cs`
  (4).
- `tests/RealEstateCrm.BuildingBlocks.Infrastructure.Tests/`: `Catalogs/HttpCatalogReaderPortTests.cs`
  (4, caché, sin infra).

**Modificados fuera de la write zone corta**: `RealEstateCrm.slnx` (2 proyectos de test nuevos),
`.env.example` y `DEVELOPMENT.md` (sección 8 del ciclo — sin credenciales nuevas: el seed de esta
task son catálogos, no usuarios ni cambios al realm).

No se tocó `apps/crm-web`, ningún archivo de la lista prohibida (Wave 1 §3), ni código de
`access-service`/`party-service`/otro servicio de dominio.

## Comandos y resultados

```text
$ dotnet build RealEstateCrm.slnx
Compilación correcta.
    0 Advertencia(s)
    0 Errores

$ bash scripts/test-fast.sh
==> dotnet build         → Compilación correcta.
==> dotnet test          → sin infra
    RealEstateCrm.ArchitectureTests.dll                   → Superado: 3,  Total: 3
    RealEstateCrm.ContractTests.dll                       → Superado: 80, Total: 80  (60 previos + 20 nuevos de Catalogs/Events)
    PlatformConfigService.Application.Tests.dll           → Superado: 19, Total: 19  (nuevo)
    RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 55, Total: 55  (51 previos + 4 nuevos, HttpCatalogReaderPort)
    AccessService.Application.Tests.dll                   → Superado: 46, Total: 46  (sin cambios)
==> apps/scripts/build-webs.sh
    crm-web: next build              → ✓ Compiled successfully
    platform-admin-web: next build   → ✓ Compiled successfully

$ bash scripts/test-integration.sh   (Docker real: mongo, rabbitmq, keycloak)
==> healthchecks: crm-mongo / crm-rabbitmq / crm-keycloak → healthy
==> dotnet test --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak"
RealEstateCrm.BuildingBlocks.Infrastructure.Tests.dll → Superado: 14, Total: 14  (heredados de FND-002/FND-003/ACL-001, sin cambios)
AccessService.Api.Tests.dll                            → Superado: 2,  Total: 2   (heredados de ACL-001, sin cambios)
PlatformConfigService.Api.Tests.dll                    → Superado: 2,  Total: 2   (nuevo)
```

Gate del Paso 0 (plan Wave 2, sección 2) verificado antes de escribir código: Wave 1 DONE en el
board, las 11 decisiones de la sección 5 ya marcadas, Docker Desktop levantado a pedido del
humano y `bash scripts/test-integration.sh` en verde en `main` (14/14 + 2/2, heredado de ACL-001)
antes de cualquier cambio de esta task.

## Ejemplos de dos versiones del mismo catálogo (evidencia requerida #2)

```text
POST /api/v1/catalogs
{ "catalogType": "LossReason", "code": "PRECIO", "label": "Precio" }
→ 201, entryId=e1, version interna del documento=1

GET /api/v1/catalogs/LossReason
→ { "entries": [...9 seed + "PRECIO"...], "catalogVersion": 1 }   // publicado por el seed

POST /api/v1/catalogs/LossReason/publish   (Administrador)
→ 200 { "catalogType": "LossReason", "catalogVersion": 2 }

GET /api/v1/catalogs/LossReason?version=1
→ 409 catalog_version_mismatch, "La versión vigente de 'LossReason' es 2, no 1."

GET /api/v1/catalogs/LossReason?version=2
→ 200, mismos entries de arriba (publicar NO mutó ningún registro, ver
  PublishCatalogVersionAsync_increments_the_counter_without_mutating_existing_entries)
```

## Contratos publicados

**Endpoint REST v1** (`PlatformConfigService.Api`, D1 — controller + `ProblemDetailsV1`), único
recurso `api/v1/catalogs`:

| Método y ruta | Permiso requerido | Notas |
|---|---|---|
| `GET /api/v1/catalogs/{catalogType}?activeOnly=&version=` | `catalogs.read` | CAT-001..CAT-005. `activeOnly` default `false` (la screen admin ve también inactivos). `version` opcional: si no coincide con la vigente, 409 `catalog_version_mismatch`. Respuesta `CatalogQueryResultV1`. |
| `POST /api/v1/catalogs` | `catalogs.manage` | Alta. Body `CreateCatalogEntryRequest`. 201 + `CatalogEntryV1`. 409 `catalog_entry_duplicate_code` si `(catalogType, code)` ya existe. 422 `invalid_catalog_type`/`catalog_entry_invalid_stage_fields`. |
| `PUT /api/v1/catalogs/{entryId}` | `catalogs.manage` | Edita `label`/`order`. `catalogType`/`code`/`pipelineKind`/`semanticState` no editables. |
| `POST /api/v1/catalogs/{entryId}/deactivate` | `catalogs.manage` | Baja lógica. 409 `catalog_entry_already_inactive` si ya estaba inactiva. |
| `POST /api/v1/catalogs/{catalogType}/publish` | `catalogs.manage` | CAT-006. Respuesta `CatalogVersionPublishedV1`. No muta ninguna `CatalogEntry`. |

`403 forbidden` (D4) para cualquier permiso denegado, `ProblemDetailsV1` con `errorCode` igual al
`ReasonCode` de `AuthorizationDecision` en `Detail` — mismo patrón que `AccessService.Api`.

**DTOs compartidos** (`RealEstateCrm.Contracts.Catalogs`, consumidos por el BFF y por
`HttpCatalogReaderPort`, D7):
- `CatalogEntryV1(EntryId, CatalogType, Code, Label, Order?, PipelineKind?, SemanticState?, Active)`.
- `CatalogQueryResultV1(Entries, CatalogVersion)`.
- `CatalogTypes` (6 constantes + `All`/`IsValid`), `PipelineKinds` (`DEMAND`/`SUPPLY`),
  `SemanticStates` (`OPEN`/`WON`/`LOST`).

**Puerto de lectura para consumidores** (D7, entregable explícito de la task):
`RealEstateCrm.BuildingBlocks.Catalogs.ICatalogReaderPort.GetAsync(catalogType, activeOnly, ct)`
→ `CatalogQueryResultV1`. Adapter `HttpCatalogReaderPort`
(`BuildingBlocks.Infrastructure/Catalogs/`) con `IMemoryCache` ≤60 s (mismo `CacheDuration` que
`HttpAuthorizationPort`, configurable). Registro: `services.AddCatalogHttpClient(configuration)`
(sección `PlatformConfigService:BaseUrl`). `platform-config-service` no se registra a sí mismo con
este adapter (lee su propio Mongo directo, igual que `access-service` con `IAuthorizationPort`).
**V2-PTY-001 lo usa para validar `originCode` contra `CommercialOrigin`** (plan Wave 2, sección
7.4).

**Eventos v1** (outbox → RabbitMQ, exchange `platform-config-service.events`, routing key =
nombre del evento): `CatalogEntryCreated` (`CatalogEntryCreatedV1`), `CatalogEntryUpdated`
(`CatalogEntryUpdatedV1`), `CatalogEntryDeactivated` (`CatalogEntryDeactivatedV1`),
`CatalogVersionPublished` (`CatalogVersionPublishedV1`, `AggregateId` derivado
determinísticamente del `catalogType` vía `CatalogTypeIdentity.ToAggregateId`, ver "Decisiones
locales" #5).

**Bases y colecciones Mongo** (D8): base `crm_platform_config`, colecciones `catalog_entries`
(`_id` = `EntryId`, índice único compuesto `(CatalogType, Code)`), `catalog_type_versions` (`_id`
= `catalogType`, un documento por tipo con el contador `CurrentVersion`), `outbox_messages`/
`inbox_consumed_messages` (heredadas de `BuildingBlocks`).

**Catálogo de errores de dominio** (D11, `PlatformConfigErrorCodes`): `catalog_entry_not_found`
(404), `catalog_entry_duplicate_code` (409), `catalog_entry_already_inactive` (409),
`invalid_catalog_type` (422), `catalog_entry_invalid_stage_fields` (422),
`catalog_version_mismatch` (409, decisión local, ver abajo).

**BFF** — `GET /screens/{screenId}`: `ADM-05` (índice estático de los seis `catalogType`, sin ida
a platform-config-service), `ADM-06`..`ADM-11` (una por `catalogType` fijo,
`?activeOnly=`), `ADM-12` (`?catalogType=&activeOnly=`, panel de versión — reusa el mismo GET).
`POST /mutations/{name}`: `createCatalogEntry`, `updateCatalogEntry` (requiere `entryId` en el
body), `deactivateCatalogEntry` (requiere `entryId`), `publishCatalogVersion` (requiere
`catalogType`). Excluidas como overrides POC: `AUT-04` (wizard de primer ingreso de la
instalación) y `ADM-13` (datos de la inmobiliaria) — ver "Decisiones locales" #6.

**Seed de desarrollo** (D9, CAT-006, `PlatformConfigServiceDevSeedHostedService`, solo
`Development`): 52 `CatalogEntry` (14 `CommercialStage` + 9 `ActivityType` + 8
`CommercialOrigin` + 9 `LossReason` + 4 `OperationType` + 8 `PropertyType`) y versión 1 publicada
de los seis `catalogType`. Sin credenciales ni usuarios: no hay nada que agregar a
`.env.example`/`DEVELOPMENT.md` más allá de la URL del servicio (ya agregada).

## Decisiones locales

1. **Modelo de versionado — "versión por entrada" (decisión del equipo, presentada como pregunta
   explícita porque la propia task V2-CAT-001 la marca "Frenar en")**: `CatalogEntry.Version` es
   un contador de concurrencia optimista por ENTRADA (`VersionedMongoRepository`, igual que
   `UserAccount`). `catalogVersion` (lo que devuelve `GET` y lo que publica
   `CatalogVersionPublished`) es un contador SEPARADO por `catalogType`
   (`catalog_type_versions`), incrementado únicamente por `PublishCatalogVersion`. **No hay
   snapshot inmutable**: `catalogVersion` permite saber en qué momento de publicación se escribió
   un `code`, no reconstruir el contenido completo del catálogo en esa versión. `GET
   /api/v1/catalogs/{catalogType}?version=` es una decisión local adicional (no pedida
   literalmente por la task, pero consistente con la decisión del equipo): si se pasa y no
   coincide con la vigente, devuelve 409 `catalog_version_mismatch` en vez de ignorarlo — deja a
   un consumidor detectar que su caché quedó desactualizada, sin prometer reconstruir historia
   que este modelo no guarda. Si una wave futura (ANA-001, lineage real) necesita snapshots
   inmutables, es un cambio de modelo, no una extensión de este.
2. **Aggregate único `CatalogEntry` discriminado por `catalogType`**, no seis aggregates
   distintos: los seis catálogos comparten exactamente el mismo ciclo de vida (Create/Update/
   Deactivate/Publish) y solo `CommercialStage` necesita `order`/`pipelineKind`/`semanticState`
   (nullable en el resto). Consistente con como la propia task los agrupa bajo un único query
   `GET /catalogs/{catalogType}`.
3. **`ScreensController`/`MutationsController` extendidos, no reestructurados**: el plan Wave 2
   (§1, §7.3) imaginaba "archivos propios" por task para evitar conflictos, pero V2-ACL-001 (ya
   mergeada) implementó un único controller por ruta con branching por `screenId`/`name`. Dos
   controllers con `[Route("screens")]` + `[HttpGet("{screenId}")]` son una ruta ambigua para
   ASP.NET Core (`AmbiguousMatchException` en cualquier request), así que la única forma de
   agregar screens/mutations de catálogo sin romper el ruteo es agregar ramas a los archivos que
   ya existen. Es un agregado (nuevos `if`/`case`), no una reestructuración: ninguna rama de
   ACL-001 cambió. Documentado explícitamente en el resumen previo a escribir código.
4. **`CatalogsAuthorizationEndToEndTests` usa `FakeAuthorizationPort`, no `HttpAuthorizationPort`
   real**: a diferencia de `access-service` (que evalúa su propia matriz localmente, sin salir
   por HTTP), `platform-config-service` SIEMPRE necesita a `access-service` vivo como proceso
   aparte para autorizar (D2, D10: los servicios corren con `dotnet run`, no en el
   `docker-compose.yml` que levanta `scripts/test-integration.sh`). Sin `access-service` corriendo,
   la request real devuelve 500, no 403. Se sustituyó `IAuthorizationPort` vía
   `WebApplicationFactory.ConfigureTestServices` por el mismo `FakeAuthorizationPort` que
   documenta `IMPLEMENTATION_REPORT-V2-ACL-001a.md` para tests de CAT-001, probando así el mapeo
   decisión→403/200 de `CatalogsController` sin acoplar el test a que otro servicio esté vivo. La
   producción (`Program.cs`) sigue usando `HttpAuthorizationPort` real sin cambios.
5. **`CatalogVersionPublished` usa un `AggregateId` determinístico derivado del `catalogType`**
   (`CatalogTypeIdentity.ToAggregateId`, SHA-256 truncado a 16 bytes, sin uso de seguridad): el
   evento ocurre sobre el `catalogType` completo, no sobre una `CatalogEntry`, y
   `EventEnvelopeV1.AggregateId` no es nullable. Mismo `catalogType` siempre produce el mismo id,
   así que todos los eventos `CatalogVersionPublished` de un mismo tipo comparten `aggregateId`.
6. **Screens excluidas de esta task (overrides POC)**: `AUT-04` ("Primer ingreso de la
   instalación", wizard de 4 pasos) y `ADM-13` ("Datos de la inmobiliaria") tienen
   `task: "V2-CAT-001"` en `screen-registry.ts` pero no corresponden a datos de catálogo — son
   configuración de instalación/organización, fuera de alcance de V2 (ADR-001, sin
   `organizationId`). Mismo criterio que V2-ACL-001 excluyó `ADM-02` ("Invitar usuario").
7. **Regla de proceso "LOST exige `lossReasonCode`"/"OPEN no va a WON/LOST" NO implementada acá**
   (instrucción explícita para esta sesión, confirmada contra el propio texto de la task en
   "Explícitamente fuera"): el catálogo solo expone `semanticState`/`LossReason` como datos; la
   regla de proceso la aplica `V2-PIPE-001`. `CatalogEntryTests`/`CatalogServiceTests` prueban que
   el catálogo provee esos datos correctamente (`semanticState` por etapa, motivos de pérdida
   listados), no la regla en sí. Los criterios de aceptación 4 y 5 de la task
   (`docs/tasks/v2/wave-2-access-catalogs/V2-CAT-001-catalogos-comerciales.md`) y la evidencia
   requerida 3 ("test de cierre con motivo de pérdida") quedan **fuera de alcance de esta task por
   diseño**, no pendientes por olvido.
8. **Códigos (`Code`) de las 52 entradas del seed**: la task no los da literalmente (solo las
   etiquetas en español), así que se derivaron en `SCREAMING_SNAKE_CASE` sin acentos
   (`DevSeedCatalogEntries.cs`, ej. "Correo electrónico" → `CORREO_ELECTRONICO`). Documentado
   como decisión local, no como literal de la task.
9. **`ICatalogVersionPort`/`CatalogTypeVersionDocument` no participan del test de arquitectura de
   aggregates versionados**: `catalog_type_versions` es un contador de infraestructura (D8), no
   un aggregate de dominio con invariantes — vive enteramente en
   `PlatformConfigService.Infrastructure`, sin equivalente en `Domain`.

## Supuestos

- `activeOnly` default `false` en `GET /api/v1/catalogs/{catalogType}`: la screen administrativa
  (`ADM-06`..`ADM-11`) necesita ver también las entradas desactivadas (para mostrar el historial
  de baja lógica); un consumidor de negocio (D7, `ICatalogReaderPort`) pasa `activeOnly=true`
  explícitamente. No estaba fijado por la task.
- `ADM-05` (índice de catálogos) se resuelve en el propio `operations-bff` con una lista estática
  de los seis `catalogType` — no hay ningún endpoint de "índice" en `platform-config-service`
  porque los seis tipos son un literal fijo de la task, no datos que puedan cambiar en runtime.
- `ADM-12` ("Versión de un catálogo") reutiliza el mismo `GET /api/v1/catalogs/{catalogType}`
  (que ya devuelve `catalogVersion`) en vez de un endpoint separado: no hay dato adicional que
  mostrar más allá de lo que ya trae la lista.
- El realm-export de Keycloak (`dev.administrador`/`dev.vendedor`) no se tocó: ya tiene los tres
  usuarios de V2-ACL-001 con los roles necesarios para probar `catalogs.manage`/`catalogs.read`.

## Follow-ups

- **V2-PTY-001**: usar `ICatalogReaderPort`/`AddCatalogHttpClient` para validar `originCode`
  contra `CommercialOrigin` (D7, guardando `catalogVersion` junto con la Party).
- **V2-PIPE-001**: implementar la regla de proceso "LOST exige `lossReasonCode`"/"OPEN no va a
  WON/LOST" (fuera de alcance de esta task por diseño, ver "Decisiones locales" #7), y decidir si
  necesita un modelo de snapshot inmutable de catálogo para lineage real (`catalogVersion` de esta
  task solo es un contador, no reconstruye contenido histórico).
- Si una wave futura necesita reactivar una entrada desactivada, agregar `ActivateCatalogEntry`:
  no existe hoy (la task no lo pide — solo `DeactivateCatalogEntry` — y el seed nunca la
  necesita).
- `scripts/run-slice.{sh,ps1}` (D10) sigue sin crearse: ahora hay dos servicios reales
  (`access-service` + `platform-config-service`) más `operations-bff`; documentado en
  `DEVELOPMENT.md` como pendiente, no en la write zone declarada de esta task.
- Si una wave futura corre `platform-config-service` con más de una instancia, revisar
  `CatalogVersionRepository.IncrementAsync`: es atómico por documento (`FindOneAndUpdate` +
  `$inc`), pero no hay lock distribuido a nivel de negocio (mismo supuesto ya documentado para
  `OutboxRelayBackgroundService` en V2-ACL-001).

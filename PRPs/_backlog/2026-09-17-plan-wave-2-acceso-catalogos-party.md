# Plan de ejecución — Wave 2 (Acceso, catálogos y Party) · CRM Inmobiliario V2

> **Para:** Claude Sonnet (o cualquier IA) ejecutando en **Claude Code** sobre la PC del desarrollador.
> **Alcance:** `W2-0` (skills) → `V2-ACL-001a` (contrato) → `V2-ACL-001` + `V2-CAT-001` en paralelo → `V2-PTY-001`.
> **Fecha:** 2026-09-17 · **Estado:** decisiones de la sección 5 cerradas (opciones recomendadas). Resto del Paso 0 a verificar antes de cada task.
> **Continúa:** `PRPs/_backlog/2026-09-16-plan-wave-1-fundacion.md`. Todo lo que ese plan dice en sus secciones 1, 2, 3, 6 y 8 **sigue vigente** salvo lo que este archivo cambie explícitamente.

---

## 0. Punto de partida (verificado en `main`)

Wave 1 cerrada (`V2-FND-001/002/003` en DONE). Ya existe y **no se re-decide**:

| Tema | Estado en `main` | Dónde |
|---|---|---|
| Solution | 11 servicios × 4 capas + 2 BFF, test de arquitectura | `RealEstateCrm.slnx`, `tests/RealEstateCrm.ArchitectureTests` |
| Contratos | `ExecutionContextV1` (sin tenant), `ProblemDetailsV1`, `PageV1<T>`, `EventEnvelopeV1<T>`, `ErrorCodes` (5 genéricos, snake_case) | `contracts/RealEstateCrm.Contracts` |
| Puertos | `IAuthenticationPort`, `IRepository<TAggregate,TId>`, `IUnitOfWork`, `IOutbox`, `IInbox`, `IEventPublisher`, `IEventConsumer<T>` | `building-blocks/RealEstateCrm.BuildingBlocks` |
| Adapters | Mongo (repo, UoW transaccional, outbox, inbox), RabbitMQ (exchange topic por servicio, routing key = nombre de evento, cola+DLQ por consumidor), auth cookie OIDC + JwtBearer | `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure` |
| IDs | `Guid` nativo, string en JSON | Reporte FND-002 |
| Health/observabilidad | `AddCrmHealthChecks`, `MapCrmHealthEndpoints`, `AddCrmObservability`, `UseCrmCorrelationId`, redacción de datos sensibles — **sin wire-up** en ningún `Program.cs` | Reporte FND-003 |
| Infra | Compose: `mongo:7.0.43` (replica set `rs0`), `rabbitmq:4.3.6-management`, `keycloak:26.7.4` realm `crm-dev` (roles Administrador/Vendedor/Responsable Comercial, usuario `dev.vendedor`) | `docker-compose.yml`, `infra/keycloak` |
| CI | `scripts/test-fast.*` y `scripts/test-integration.*`; Traits `RequiresMongo/RequiresRabbitMq/RequiresKeycloak` | `.github/workflows/ci.yml` |
| Front | `crm-web` consume BFF por `GET /screens/:screenId` y `POST /mutations/:name` con cookie | `apps/crm-web/src/lib/data-source.ts` |

**Regla nueva para Wave 2:** todo test que toque infraestructura real lleva uno de los 3 Traits. `bash scripts/test-fast.sh` debe quedar verde sin Docker.

---

## 1. Orden y paralelismo

```text
W2-0  Corregir skills amarillas necesarias (1 PR, humano revisa el diff)
  ↓
ACL-001a  Contrato de autorización: IAuthorizationPort + permisos + stub (1 PR chico)
  ↓
ACL-001 (access-service)  ║  CAT-001 (platform-config-service)     ← en paralelo, worktrees separados
  ↓                          ↓
PTY-001 (party-service)   ← cuando ACL y CAT estén mergeadas
```

**Zonas compartidas que generan conflicto:** `RealEstateCrm.slnx`, `bffs/operations-bff/src/OperationsBff.Api/Program.cs`, `docker-compose.yml`.
- ACL-001 es dueña del **primer wire-up** de `operations-bff` (auth cookie, health, observabilidad, correlationId, seed de usuarios).
- CAT-001 **no toca** `Program.cs` del BFF hasta que ACL-001 esté mergeada: deja sus endpoints BFF en archivos propios y hace rebase + una línea de registro al final.
- Ninguna task modifica código existente de otra task salvo registro en `Program.cs` / `.slnx`.

---

## 2. Paso 0 — Gate humano

- [x] Wave 1 en DONE en `docs/tasks/v2/TASK_BOARD.md` (verificado).
- [x] **Todas las decisiones de la sección 5 tienen respuesta** (opción recomendada tildada, 2026-09-17) y quedaron commiteadas en este archivo.
- [ ] Docker Desktop corriendo y `bash scripts/test-integration.sh` en verde en `main`.

Sin esto: **frenar**.

---

## 3. W2-0 — Skills amarillas que Wave 2 necesita

Wave 2 es la primera con endpoints, aggregates y persistencia reales. Estas skills se **corrigen** (no se crean nuevas) para que dejen de contradecir ADR-001 y las decisiones ya tomadas:

| Skill | Corregir |
|---|---|
| `aspnetcore-rest-layer` | Quitar `tenantId` y regla "404 en vez de 403" (V2 usa **403** para acciones prohibidas). Reemplazar referencia a ADR-006 por `ProblemDetailsV1` + `ErrorCodes` de `contracts`. Estilo de API según decisión D1. Quitar refs a FND-003/FND-005 viejas. |
| `ddd-hexagonal-architecture` | Quitar `tenantId`, "~20 servicios" → los 11 de V2-FND-001. `IRepository<T>` genérico **está permitido** (es el puerto de FND-002) siempre que no exponga `IQueryable`. Ejemplos solo con entidades V2. |
| `mongodb-document-modeling` | Quitar `tenantId` de documentos, índices y unicidad. Sin índice único de CUIT/email (V2-PTY-001 excluye resolución de duplicados). Replica set ya decidido. Nombres de base/colección según D8. |
| `aspnetcore-security-owasp-baseline` | Quitar tenancy, 404-por-tenant, uploads/IObjectStorage. Mantener: autorización revalidada en owner, validación en borde, sin PII en logs, CORS explícito, cookie en BFF. |
| `aspnetcore-error-and-observability` | Quitar `tenantId` del contexto de log; apuntar a `AddCrmObservability`/`SensitiveDataRedactor` existentes. |
| `mongodb-dotnet-driver` | Versión = `3.11.2` (la instalada); quitar `tenantId`; resto de guía del driver 3.x se mantiene. |
| `event-driven-outbox-inbox` | Envelope = `EventEnvelopeV1` real (sin `tenantId`); outbox/inbox como ya los implementó FND-002 (`outbox_messages`, índice `EventId+ConsumerName`); quitar servicios excluidos de los ejemplos. |
| `oidc-keycloak-aspnetcore` | Keycloak `26.7.4`; tenant fuera; roles según D3. |

**Reglas de W2-0:**
- Solo editar esos `SKILL.md` en `.github/skills/` **y** su copia en `.claude/skills/` (mismo contenido).
- No agregar reglas nuevas que no estén en ADR-001, tasks V2, reportes FND o sección 5 de este plan.
- `multitenancy-authorization` **no se trae**. Las demás amarillas siguen sin usarse.
- PR `chore(skills): alinear skills de Wave 2 con V2`. Un humano revisa el diff línea por línea antes de mergear.

---

## 4. Política de skills en Wave 2

**Aplicables:** las 9 verdes de Wave 1 + las 8 corregidas en W2-0 (solo después de su merge).
**No aplicables:** `multitenancy-authorization`, `cqrs-read-models-projections`, `nextjs-frontend-architecture`, `crm-ux-quick-capture`, `aspnetcore-outgoing-http`, `aspnetcore-messaging`, `aspnetcore-config-and-secrets`, `aspnetcore-di-and-middleware-pipeline`, `aspnetcore-microservice-orchestrator` hasta que se corrijan en su propia wave.
Si una task necesita algo de una skill no aplicable: opción más simple compatible con la task, documentada como decisión local; si cae en la sección 5 o 7, frenar.

---

## 5. Decisiones de Wave 2 — CERRAR ANTES DE ARRANCAR

Marcá una opción por decisión (la recomendada va primero). Estas decisiones son **transversales**: ACL, CAT y PTY tienen que usar exactamente las mismas.

**D1. Estilo de API de los servicios de dominio**
- [x] (Recomendada) Controllers `[ApiController]`, rutas `/api/v1/<recurso>`, errores solo `ProblemDetailsV1`. Minimal API solo para health.
- [ ] Minimal APIs agrupadas por módulo, mismas rutas y errores.

**D2. Cómo un servicio owner verifica permisos (`IAuthorizationPort`)**
- [x] (Recomendada) `access-service` es dueño de la matriz rol→permiso y la expone por HTTP interno (`POST /api/v1/authorization/evaluate`). Los owners llaman vía adapter con caché `IMemoryCache` corto (≤60 s). La regla de **propiedad del registro** (ej. Vendedor solo edita lo asignado) la evalúa el owner con su propio dato `responsibleUserId`.
- [ ] Matriz rol→permiso como paquete de contrato estático en `contracts`; cada owner la evalúa localmente con los roles del token (sin llamada HTTP). Cambios de matriz requieren redeploy.

**D3. Fuente de verdad de roles y alta de usuarios**
- [x] (Recomendada) Keycloak = solo identidad (login). `access-service` = fuente de verdad de `UserAccount` y `RoleAssignment`, vinculado por el `sub` del token. Usuario se crea en Keycloak **manualmente o por realm-export** (sin integrar la Admin API en la POC) y el Administrador lo habilita y le asigna rol en el CRM. Primer login de un `sub` desconocido → usuario `PENDING` sin permisos. Los roles del realm se ignoran para autorizar.
- [ ] `access-service` crea también el usuario en Keycloak vía Admin REST API (client credentials). Más completo, más integración y más secretos.

**D4. Acción prohibida vs recurso inexistente**
- [x] (Recomendada) Prohibido → **403** `forbidden`; inexistente → **404** `not_found` (literal de V2-ACL-001).

**D5. Contrato BFF ↔ `crm-web`**
- [x] (Recomendada) `operations-bff` implementa el contrato que **ya usa** `crm-web`: `GET /screens/{screenId}` y `POST /mutations/{name}`, mapeando cada screen/mutation del slice a llamadas REST a los servicios. `crm-web` no se reescribe. Cada task implementa solo las screens/mutations de su slice (listadas en `apps/crm-web/docs/SCREEN_TRACEABILITY.md`).
- [ ] BFF REST por recurso (`/users`, `/catalogs`, `/parties`) y se adapta `data-source.ts` de `crm-web` en cada slice.

**D6. BFF → servicios: identidad del usuario**
- [x] (Recomendada) El BFF reenvía el access token del usuario (token relay) como Bearer; cada servicio valida JWT y reconstruye `ExecutionContextV1`. Sin client credentials internas en la POC.
- [ ] Client credentials del BFF + headers de actor. Requiere confiar en headers internos.

**D7. Consumo de catálogos por otros servicios (PTY, y luego PRP/DMD/PIPE)**
- [x] (Recomendada) Consulta HTTP a `platform-config-service` (`GET /api/v1/catalogs/{type}?activeOnly=true`) con caché corto. Cada registro guarda `code` + `catalogVersion` usados. Sin read model local todavía.
- [ ] Réplica local por eventos `CatalogVersionPublished` en cada consumidor.

**D8. Bases y colecciones Mongo**
- [x] (Recomendada) Una base por servicio en la misma instancia: `crm_access`, `crm_platform_config`, `crm_party`. Colección por aggregate root en snake_case plural (`user_accounts`, `catalog_entries`, `parties`, `party_relationships`). `outbox_messages`/`inbox_consumed_messages` dentro de la base de cada servicio.
- [ ] Una sola base `crm` con prefijo por servicio (`access_user_accounts`...).

**D9. Seeds de desarrollo**
- [x] (Recomendada) `IHostedService` idempotente por servicio, activo solo en `Development`: ACL siembra 3 usuarios (Administrador, Vendedor, Responsable Comercial) vinculados a usuarios del realm; CAT siembra los defaults de V2-CAT-001. Los usuarios faltantes se agregan al realm-export existente.
- [ ] Scripts `mongosh` manuales en `infra/seed/`.

**D10. Cómo se corren los servicios en local**
- [x] (Recomendada) Infra en Compose; servicios y BFF con `dotnet run` usando puertos de `launchSettings.json`. Script `scripts/run-slice.{sh,ps1}` que levanta access + platform-config + party + operations-bff. Servicios fuera de Compose por ahora.
- [ ] Agregar servicios .NET al `docker-compose.yml`.

**D11. Catálogo de errores de dominio**
- [x] (Recomendada) Cada servicio define sus códigos en su capa Api/Application, snake_case con prefijo de dominio (`catalog_entry_inactive`, `party_not_found`), documentados en el reporte de la task. `contracts/ErrorCodes` sigue con los 5 genéricos.

> Si el grupo elige distinto a la recomendada en D2, D3 o D5, **re-leer las secciones 7.2–7.4**: cambian las interfaces.

---

## 6. Ciclo por task (igual que Wave 1, con 2 agregados)

1. `git checkout main && git pull` · worktree propio: `git worktree add ../crm-<task> -b feat/v2-<task>-<slug> main`.
2. Contexto: plan Wave 1 §2 + **este plan** + task + reportes FND-002/003 + skills aplicables.
3. Resumen ≤10 líneas y **esperar OK**.
4. Implementar solo la write zone.
5. `bash scripts/test-fast.sh` **y** `bash scripts/test-integration.sh` (Docker) en verde.
6. Reporte `IMPLEMENTATION_REPORT-V2-<TASK>.md` en la carpeta de la wave + sección **Handoff** en la task.
7. **(Nuevo)** Sección "Contratos publicados" en el reporte: endpoints, eventos v1, códigos de error, permisos. Es lo que leen las tasks siguientes.
8. **(Nuevo)** Si la task agrega usuarios/roles/catálogos al seed o al realm, documentar credenciales de dev en `.env.example`/`DEVELOPMENT.md`.
9. Commit, push, PR. Un humano mergea y marca DONE.

---

## 7. Tasks

### 7.1 V2-ACL-001a — Contrato de autorización (bloquea CAT y PTY)

**Write zone:** `building-blocks/RealEstateCrm.BuildingBlocks/Authorization/`, `contracts/` (DTOs de autorización), `tests/RealEstateCrm.TestSupport` (fake), `tests/RealEstateCrm.ContractTests`.

**Entregables:**
- Puerto `IAuthorizationPort.EvaluateAsync(actorId, permission, resourceType, resourceId?, ct)` → `AuthorizationDecision` (Allowed/Denied + reason code). Firma exacta de V2-ACL-001.
- Constantes de permisos V2 en `contracts` (ej. `users.manage`, `catalogs.read`, `catalogs.manage`, `parties.read`, `parties.write`, `parties.assign_responsible`, `parties.change_commercial_status`) y matriz rol→permiso **como documento del reporte** (la implementación vive en ACL-001).
- `FakeAuthorizationPort` en TestSupport.
- Contract tests del DTO.

**No hace:** persistencia, endpoints, adapter HTTP. PR chico, se mergea antes de arrancar ACL-001 y CAT-001.

**Handoff (completado, ver `docs/tasks/v2/wave-2-access-catalogs/IMPLEMENTATION_REPORT-V2-ACL-001a.md`):**
- Publicado: `IAuthorizationPort` (`building-blocks/RealEstateCrm.BuildingBlocks/Authorization/`),
  `AuthorizationDecision`/`Permissions` (8, incluye `users.read` agregado sobre los "ej." de
  arriba)/`ResourceTypes`/`DenyReasons` (`contracts/RealEstateCrm.Contracts/Authorization/`),
  `FakeAuthorizationPort` (`tests/RealEstateCrm.TestSupport/Authorization/`).
- Matriz rol→permiso: propuesta (no implementada) en el reporte, tabla Administrador/Vendedor/
  Responsable Comercial × 8 permisos con columna de notas de propiedad. `V2-ACL-001` la confirma
  o ajusta al implementar.
- `V2-CAT-001`/`V2-PTY-001`: inyectar `FakeAuthorizationPort` hasta que `V2-ACL-001` publique el
  adapter HTTP real.
- Pendiente: todo lo de implementación real queda para `V2-ACL-001` (matriz, adapter HTTP,
  endpoint `POST /api/v1/authorization/evaluate`).

### 7.2 V2-ACL-001 — Usuarios, roles, permisos y responsables

**Depende de:** ACL-001a. **Write zone:** `services/access-service/**`, `bffs/operations-bff/**` (wire-up inicial + screens de usuarios), adapter HTTP de `IAuthorizationPort` en `building-blocks/RealEstateCrm.BuildingBlocks.Infrastructure/Authorization/`, realm-export (solo usuarios de dev), tests.

**Entregables (de la task):**
- Aggregates `UserAccount` (userId, keycloakSubject, displayName, email, status ACTIVE/INACTIVE/PENDING, version) y `RoleAssignment`; commands `CreateUser`, `UpdateUser`, `DeactivateUser`, `AssignRole`, `ResolvePermission`; queries `GetUsers` (paginado `PageV1`), `GetEffectivePermissions`.
- Matriz rol→permiso en backend + endpoint de evaluación (según D2).
- Eventos v1: `UserCreated`, `UserUpdated`, `UserDeactivated`, `RoleAssigned`, `ResponsibleAssigned` vía outbox.
- **Primer wire-up real** en `AccessService.Api` y `OperationsBff.Api`: auth (JwtBearer en servicio, cookie OIDC en BFF), `AddCrmObservability`, `UseCrmCorrelationId`, health live/ready.
- **Cierra follow-up de FND-003:** test end-to-end de `correlationId` BFF → access-service → evento publicado.
- BFF: screens/mutations de administración de usuarios (según D5).
- Seed de 3 usuarios (D9).

**Frenar en:** cualquier cosa de D2/D3/D5 no decidida; integración con Keycloak Admin API si D3 = recomendada; invitaciones por email (fuera de alcance).

**Aceptación:** los 6 criterios de V2-ACL-001 + correlationId e2e + desactivar usuario no borra historia + 403 ProblemDetails desde backend.

### 7.3 V2-CAT-001 — Catálogos comerciales (paralelo con 7.2)

**Depende de:** ACL-001a. **Write zone:** `services/platform-config-service/**`, archivos nuevos en `bffs/operations-bff/` para screens de catálogos (registro en `Program.cs` **después** del merge de ACL-001), tests.

**Entregables (de la task):**
- `CatalogEntry` / versión publicada para los 6 tipos: `CommercialStage` (code, label, order, pipelineKind DEMAND/SUPPLY, semanticState OPEN/WON/LOST, active, version), `ActivityType`, `CommercialOrigin`, `LossReason`, `OperationType`, `PropertyType`.
- Commands `CreateCatalogEntry`, `UpdateCatalogEntry`, `DeactivateCatalogEntry`, `PublishCatalogVersion`; query `GET /api/v1/catalogs/{catalogType}?version=&activeOnly=`.
- Invariantes: `semanticState` no editable; desactivar no rompe historia; publicar no muta registros previos.
- Eventos v1: `CatalogEntryCreated`, `CatalogEntryUpdated`, `CatalogEntryDeactivated`, `CatalogVersionPublished`.
- Seed con **los defaults literales** de V2-CAT-001 (etapas DEMAND/SUPPLY, tipos de actividad, orígenes, motivos de pérdida) + OperationType/PropertyType del plan maestro §5.
- Autorización: solo Administrador gestiona; todos los autenticados leen (vía `IAuthorizationPort`, fake en tests hasta que ACL-001 mergee).
- Adapter cliente de catálogos para consumidores (según D7) en `BuildingBlocks.Infrastructure/Catalogs/`.

**Explícitamente fuera:** validar "LOST exige lossReasonCode" y "OPEN no va a etapa WON/LOST" como regla de proceso — el catálogo expone los datos, la regla la aplica **PIPE-001**. En CAT solo se testea que el catálogo provee `semanticState` y motivos.

**Frenar en:** modelo de versionado si surge ambigüedad entre versión por entrada vs snapshot (proponer opciones).

### 7.4 V2-PTY-001 — Party, Empresa, Contacto, relaciones y estados

**Depende de:** ACL-001 y CAT-001 mergeadas. **Write zone:** `services/party-service/**`, archivos de screens Party en `bffs/operations-bff/`, tests.

**Entregables (de la task):** modelo mínimo **literal** de V2-PTY-001 (Party + PartyRelationship), commands `CreateCompany`, `UpdateCompany`, `CreateContact`, `UpdateContact`, `RelateContactToCompany`, `ChangePartyCommercialStatus`, `AssignPartyResponsible`; queries `SearchParties` (paginado), `GetCompanyDetail`, `GetContactDetail`; eventos v1 de la task.

**Reglas que más fácil se rompen:**
- `identityStatus` y `commercialStatus` en campos separados. Alta = `ACTIVE + POTENTIAL`. Nunca se genera `ALIASED`.
- Captura mínima: solo nombre + un dato de contacto. Todo lo demás opcional.
- Sin índice único de CUIT/email/teléfono; sin resolución de duplicados.
- Baja lógica; nunca borrado físico.
- `originCode` validado contra `CommercialOrigin` vía cliente de catálogos (D7), guardando versión.
- Responsable: `responsibleUserId` validado contra access-service; Vendedor solo edita parties asignadas (regla de propiedad en el owner, D2).

**Frenar en:** política de "dato de contacto mínimo" si la UI de `crm-web` exige otro campo; cualquier propuesta de deduplicar.

---

## 8. Condiciones de frenado (se suman a Wave 1 §8)

- Una decisión de la sección 5 sin marcar.
- Necesidad de modificar un contrato publicado por otra task en curso (se congela el contrato y se pregunta).
- Necesidad de tocar `apps/crm-web` fuera de lo que D5 permite.
- Aparece Organization, sucursal, scope por equipo/unidad u overrides temporales (fuera de V2).
- `test-integration.sh` no se puede correr.

---

## 9. Prompt para Claude Code (Sonnet)

```text
Vas a ejecutar UNA task del CRM inmobiliario V2: <W2-0 | V2-ACL-001a | V2-ACL-001 | V2-CAT-001 | V2-PTY-001>.

Leé en este orden y seguilos al pie de la letra:
1) PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md (este plan manda sobre el de Wave 1 donde difieran)
2) PRPs/_backlog/2026-09-16-plan-wave-1-fundacion.md secciones 2, 3 y 8
3) La task y los IMPLEMENTATION_REPORT de FND-002 y FND-003

- Verificá el Paso 0 (sección 2): si alguna decisión de la sección 5 no está marcada, frená y decime cuál.
- Usá solo las skills permitidas en la sección 4.
- Seguí el ciclo de la sección 6: antes de escribir código mostrame tu resumen y esperá mi OK.
No uses /dev. No marques la task como DONE. Ante cualquier condición de la sección 8, frená y preguntame.
```

---

## 10. Después de Wave 2 (vista general)

| Wave | Tasks | Qué habilita |
|---|---|---|
| 3 — Núcleo inmobiliario | PRP-001 → DMD-001 → MAT-001 | Property/Listing, Requirement/Captación, matching |
| 4 — Progresión comercial | PIPE-001 → COM-001 | Oportunidad visible, tablero, cambio de etapa (**flujo de 1ra entrega completo con PIPE**) |
| 5 — Historial y métricas | ACT-001 → ANA-001 | Timeline y dashboard con lineage |
| 6 — IA y cierre | AI-001 → REL-001 | Asistente revisable, búsqueda/paginación final, demo |

**Transversales pendientes:**
- Conectar `crm-web` a datos reales en cada slice (D5) y retirar el modo demo como fuente al cerrar REL-001.
- Corregir las skills amarillas restantes cuando su wave las necesite (CQRS en PIPE/ANA, Next.js/UX si se toca el front).
- Inconsistencia interna de la reducida: `AGENTS.md`, `POC_TECH_DECISIONS.md` y `ARCHITECTURE.md` todavía mencionan `tenantId` (ADR-001 prevalece; decidir si el grupo lo corrige formalmente).
- Mantener Dependabot: revisar sus PRs con el mismo criterio (sin saltos de major sin decisión).

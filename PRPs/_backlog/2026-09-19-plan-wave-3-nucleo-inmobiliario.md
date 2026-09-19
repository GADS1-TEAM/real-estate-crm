# Plan de ejecución — Wave 3 (Núcleo inmobiliario) · CRM Inmobiliario V2

> **Para:** el agente de IA que ejecute la task (Claude Code, Codex u otro) sobre la PC del desarrollador. Proceso completo y handoff en **§11**.
> **Alcance:** `W3-0` (base transversal) → `V2-PRP-001` → `V2-DMD-001` → `V2-MAT-001`. **Secuencial** (PRP y DMD comparten `supply-service`; MAT consume eventos de ambos).
> **Fecha:** 2026-09-19 · **Estado:** decisiones de la sección 5 cerradas con la opción recomendada.
> **Continúa:** plan Wave 2 (`2026-09-17-plan-wave-2-acceso-catalogos-party.md`) y plan Wave 1. D1–D11, §6 (ciclo) y §8 (frenado) de Wave 2 **siguen vigentes** salvo lo que este archivo cambie explícitamente.

---

## 0. Punto de partida (verificado en `main`, `a73c9b5`)

Wave 2 cerrada (`ACL-001a`, `ACL-001`, `CAT-001`, `PTY-001` en DONE + PR #29 de fix). Ya existe y **no se re-decide**:

| Tema | Estado en `main` | Dónde |
|---|---|---|
| Autorización | `IAuthorizationPort` + `HttpAuthorizationPort` (caché ≤60 s), matriz en access-service, 8 permisos | `contracts/.../Authorization`, reporte ACL-001 |
| Usuario del actor | `GET /api/v1/users/me` → `UserSelfV1(userId, status, roleCode)`; `IUserDirectoryPort`/`HttpUserDirectoryPort` | reporte PTY-001 |
| Asignación | `IResponsibleAssignmentValidationPort` + adapter; evento `ResponsibleAssignedV1` lo publica el **owner** | reporte ACL-001 / PTY-001 |
| Catálogos | `ICatalogReaderPort`/`HttpCatalogReaderPort` (D7). Seed: `OperationType`, `PropertyType`, `CommercialStage` DEMAND/SUPPLY, `CommercialOrigin` | reporte CAT-001 |
| Token relay | `BearerTokenRelayHandler` compartido en `BuildingBlocks.Infrastructure/Http` | fix PR #29 |
| Party | `GET /api/v1/parties/{id}` (party-service, puerto 5155) | reporte PTY-001 |
| Persistencia | `VersionedMongoRepository` (409 por `version`), outbox + `AddOutboxRelay`, inbox `EventId+ConsumerName` | reportes FND-002 / ACL-001 |
| Esqueletos vacíos | `property-service` (5117), `supply-service` (5069), `demand-service` (5220), `matching-service` (5290): solo `Program.cs` Hello World | `services/*` |
| Front | `crm-web` con screenIds reales en `apps/crm-web/src/lib/screen-registry.ts` (columna `task`) | D5 |

**Lecciones de Wave 2 que pasan a ser regla:**
- Tests de integración: **esperar por polling con timeout**; nunca asumir orden entre "el consumidor recibió" y "el outbox marcó publicado"; filtrar eventos por id del recurso propio (no tomar "el primer evento que llegue").
- Tests de Api: base Mongo propia con sufijo Guid vía `UseSetting("Mongo:DatabaseName", …)` y drop al terminar.
- Todo cliente HTTP hacia otro servicio que exija JWT usa `BearerTokenRelayHandler` (D6). Los endpoints internos sin `[Authorize]` (evaluate/validate) no reenvían token.
- `git add` explícito, nunca `git add .`; autor del commit = **la persona que ejecuta la task** (su `user.name`/`user.email` de git), nunca un usuario genérico ni el del agente.

---

## 1. Orden

```text
W3-0  Base transversal (permisos, ports de referencia, consumer genérico, skills, run-slice)   1 PR chico
  ↓
PRP-001  property-service (Property, PropertyInterest) + supply-service (Listing, productos)
  ↓
DMD-001  demand-service (Requirement) + supply-service (CaptationCase, Valuation, CommercialMandate)
  ↓
MAT-001  matching-service (MatchCase) + comando de selección en demand-service
```

**Zonas compartidas:** `RealEstateCrm.slnx`, `bffs/operations-bff/.../ScreensController.cs` y `MutationsController.cs` (solo ramas nuevas, nunca controllers nuevos en la misma ruta), `bffs/operations-bff/.../Program.cs` (solo líneas de registro), `supply-service` (PRP primero; DMD agrega sus aggregates sin modificar `Listing` salvo por su command público).

---

## 2. Paso 0 — Gate humano

- [x] Wave 2 en DONE en `docs/tasks/v2/TASK_BOARD.md`.
- [x] Decisiones de la sección 5 cerradas (opción recomendada, 2026-09-19).
- [ ] Docker corriendo y `bash scripts/test-integration.sh` en verde en `main` **antes de tocar nada**.
- [ ] Para PRP/DMD/MAT: `W3-0` mergeada.

Sin esto: **frenar**.

---

## 3. W3-0 — Base transversal (1 PR, antes de PRP)

**Branch:** `chore/w3-0-base-wave-3`. **Write zone:** `contracts/`, `building-blocks/`, `services/access-service/` (solo matriz de permisos), `services/party-service/` (solo seed de dev, ver D21), `tests/`, `.github/skills/` + `.claude/skills/`, `scripts/`, `DEVELOPMENT.md`.

**Entregables:**
1. **Permisos y resource types (D14):** constantes nuevas en `Permissions`/`ResourceTypes` + matriz en access-service + tests de la matriz. Aditivo: no cambia los 8 permisos existentes.
2. **Ports de referencia (D13):** `IPartyReferencePort.GetAsync(partyId, ct)` → `PartyReferenceV1?` (`partyId`, `kind`, `displayName`, `identityStatus`, `commercialStatus`) + `HttpPartyReferencePort` (token relay, sin caché) + fake en TestSupport. Mapea al `GET /api/v1/parties/{id}` ya existente; party-service no se modifica.
3. **Consumer genérico (D18):** `services.AddEventConsumer<TPayload, TConsumer>(exchange, eventName, queue)` en `BuildingBlocks.Infrastructure/Messaging` como `BackgroundService` sobre `RabbitMqEventConsumerHost` + inbox existente. Test de integración con Trait `RequiresMongo` + `RequiresRabbitMq` (polling con timeout).
4. **Money (D15):** `MoneyV1(decimal Amount, string Currency)` + `Currencies` (`ARS`, `USD`) en `contracts`, contract test. Serialización Mongo como `Decimal128`.
5. **Skills:** corregir `aspnetcore-outgoing-http` y `aspnetcore-messaging` (quitar `tenantId`, apuntar a `BearerTokenRelayHandler`, `AddEventConsumer`, outbox/inbox reales, D6/D7/D13/D18). Mismo contenido en `.github/skills/` y `.claude/skills/`.
6. **`scripts/run-slice.{sh,ps1}` (D10, pendiente de Wave 2):** levanta access, platform-config, party, property, supply, demand, matching y operations-bff con `dotnet run`. Documentar en `DEVELOPMENT.md`.
7. **Seed de dev en party-service (D21):** 2 propietarios y 1 interesado con **GUIDs fijos** documentados en `DEVELOPMENT.md` (idempotente, solo Development).

**No hace:** nada de Property/Listing/Requirement/Match. PR `chore(w3-0): base transversal de Wave 3`.

---

## 4. Política de skills en Wave 3

**Aplicables:** las de Wave 2 + `aspnetcore-outgoing-http` y `aspnetcore-messaging` **después** del merge de W3-0.
**No aplicables:** `multitenancy-authorization`, `cqrs-read-models-projections` (se corrige en Wave 4 para PIPE), `nextjs-frontend-architecture`, `crm-ux-quick-capture`, `aspnetcore-config-and-secrets`, `aspnetcore-di-and-middleware-pipeline`, `aspnetcore-microservice-orchestrator`, `aspnetcore-database-access-efcore` (no hay EF en V2).

---

## 5. Decisiones de Wave 3 (cerradas)

**D12. Bases y colecciones (extiende D8)**
- [x] (Recomendada) `crm_property`: `properties`, `property_interests`. `crm_supply`: `listings`, `captation_cases`, `valuations`, `commercial_mandates`. `crm_demand`: `requirements`. `crm_matching`: `match_cases`, `requirement_snapshots`, `listing_snapshots`. `outbox_messages`/`inbox_consumed_messages` en cada base.

**D13. Validar referencias a otro owner (Party, Property, Listing)**
- [x] (Recomendada) HTTP síncrono al owner con token relay, **antes** de abrir la transacción Mongo. Se guarda solo el id (+ `displayName` si la UI lo necesita en listados, marcado como dato no autoritativo). Nunca se copia el perfil completo.
  - Referencia inexistente → **422** con código local `<recurso>_reference_not_found` (ej. `party_reference_not_found`).
  - Owner caído / timeout → **503** con código local `<servicio>_dependency_unavailable`. Fail closed.
  - Cada owner publica su port: `IPartyReferencePort` (W3-0), `IPropertyReferencePort` y `IListingReferencePort` (PRP-001), `IRequirementReferencePort` y `ICaptationReferencePort` (DMD-001, para PIPE).
- [ ] Réplica local por eventos de cada referencia.

**D14. Permisos nuevos y matriz**
- [x] (Recomendada)

| Permiso | Administrador | Vendedor | Responsable Comercial |
|---|:--:|:--:|:--:|
| `properties.read` / `listings.read` / `requirements.read` / `captations.read` / `matches.read` | ✅ | ✅ | ✅ |
| `properties.write` | ✅ | ✅ | ✅ |
| `listings.write` (incluye activar/pausar/cerrar) | ✅ | ✅ (solo si es responsible) | ✅ (solo si es responsible) |
| `requirements.write` (incluye seleccionar/descartar match) | ✅ | ✅ (solo si es responsible) | ✅ (solo si es responsible) |
| `captations.write` (incluye tasación y mandato) | ✅ | ✅ (solo si es responsible) | ✅ (solo si es responsible) |

- `ResourceTypes` nuevos: `property`, `listing`, `requirement`, `captation`, `match`.
- Regla de propiedad = misma que `parties.write` (D2): la evalúa el owner comparando `responsibleUserId` con `UserSelfV1.userId`; Administrador (`roleCode`) siempre puede. Alta: `responsibleUserId` = `userId` del creador. Property no tiene responsable → sin regla de propiedad.
- Reasignar responsable de Listing/Requirement/Captation: **fuera de Wave 3** (se reutilizará `parties.assign_responsible`-style en PIPE si hace falta).

**D15. Dinero**
- [x] (Recomendada) `MoneyV1(decimal Amount, string Currency)` con `ARS`|`USD`; value object `Money` en cada dominio; Mongo `Decimal128`. Nunca `double`/`float`. Sin conversión de moneda.

**D16. Ubicación, superficie y dato desconocido (GLB-10)**
- [x] (Recomendada) `Location { province, locality, neighborhood?, street?, streetNumber?, latitude?, longitude? }` (province y locality obligatorias). Superficies y atributos como **nullable**: `null` = "Desconocido", **nunca** 0/false por defecto. Urbana: `surfaceM2`, `physicalAttributes` (ambientes, dormitorios, baños, antigüedad…). Rural: `surfaceHa`, `ruralAttributes` (uso, agua, acceso, mejoras…). Qué bloque aplica lo decide el `PropertyTypeCode` (`CAMPO` = rural; el resto urbano), documentado como regla local.

**D17. Política para activar un Listing**
- [x] (Recomendada) `ActivateListing` exige: Property existente (D13) + **al menos un PropertyInterest ACTIVE** (consultado a property-service) + `operationTypeCode` válido en catálogo. El **Mandate NO es requisito** (la UI lo muestra como advertencia, LST-05/LST-07); `supply-service` expone `hasActiveMandate` en el detalle.
  - Estados usados en V2: `DRAFT → ACTIVE ⇄ PAUSED → CLOSED`. `READY`, `WITHDRAWN`, `EXPIRED` existen en el enum pero no se generan (igual criterio que `ALIASED`).

**D18. Mensajería entre servicios de Wave 3**
- [x] (Recomendada) Eventos por RabbitMQ con el consumer genérico de W3-0. Matching consume `RequirementCreated/Updated/Activated` y `ListingActivated/Updated/Paused/Closed`, guarda un **snapshot mínimo** (solo campos de scoring) y marca los `MatchCase` afectados como `STALE` (`MatchInvalidated`).
  - Por esto, **PRP-001 agrega `ListingUpdated`** a los eventos de la task (MAT lo consume) y los payloads de `ListingActivated/Updated` y `Requirement*` llevan los campos de scoring (operación, tipo, provincia/localidad, precio/presupuesto, superficie, ambientes). Documentarlo como decisión del equipo.

**D19. `commercialProgress` (preparación de PIPE)**
- [x] (Recomendada) `commercialProgress = { stageCode, stageCatalogVersion, changedAt, changedBy }`. Al crear Requirement se inicializa con la primera etapa OPEN de `CommercialStage` pipelineKind **DEMAND**; CaptationCase con la primera de **SUPPLY** (menor `order`, vía `ICatalogReaderPort`). **El cambio de etapa NO se implementa en Wave 3** (lo hace PIPE-001). Sin colección `opportunities`.

**D20. Selección de un match (MAT-004)**
- [x] (Recomendada) El BFF llama a matching `POST /api/v1/matches/{id}/select`; matching llama por HTTP (token relay) a demand-service `POST /api/v1/requirements/{id}/selected-listings` (demand valida `requirements.write` + propiedad y es el único que escribe el Requirement) y, si responde OK, marca `SELECTED` y publica `MatchSelected`. Descartar no toca el Requirement (solo `DISCARDED` + `feedbackReasonCode`). DMD-001 deja creado ese endpoint en demand-service (DMD-004 ya exige ver los listings seleccionados).

**D21. Datos precargados (demo)**
- [x] (Recomendada) Seeds idempotentes solo en Development (D9): party-service (W3-0) crea propietarios/interesado con GUIDs fijos; property-service siembra 3 Properties (2 urbanas, 1 rural) con PropertyInterest a esos GUIDs; supply-service siembra 4 Listings (2 de la misma Property con operaciones distintas), 3 ACTIVE. DMD siembra 1 Requirement del interesado. Todo documentado en `DEVELOPMENT.md`.

**D22. Scoring (MAT-002)**
- [x] (Recomendada) Criterios duros (`REQUIRED`): operación, tipo (si el Requirement lo indica), localidad/provincia, presupuesto máximo (misma moneda; distinta moneda → criterio "no evaluable", nunca convierte). Blandos: superficie, ambientes, barrio, presupuesto dentro del ±10 %. `score` 0–100 entero, pesos fijos en código, `scoringPolicyVersion = "v1"`. Un duro incumplido → `INELIGIBLE` sin importar blandos. Dato del Listing `null` → criterio `UNKNOWN` (ni suma ni resta; se explica).

---

## 6. Ciclo por task

Igual que Wave 2 §6 (worktree propio, resumen ≤10 líneas y **esperar OK**, write zone, `test-fast.sh` + `test-integration.sh` en verde, reporte con **Contratos publicados** y **Handoff**, commit único con el autor humano configurado en git, push). Agregados:
- **Después del OK, trabajar hasta terminar** sin frenar salvo condición de §8.
- En el reporte, sección **"Datos de demo"** si la task agrega seed.
- Si `gh` está autenticado: `gh pr create --base main --fill` y `gh pr checks --watch`. **No mergear**: el merge lo hace un humano del equipo después de revisar (checklist §11.4).

---

## 7. Tasks

### 7.1 V2-PRP-001 — Property, PropertyInterest, Listing y productos

**Depende de:** W3-0. **Write zone:** `services/property-service/**`, `services/supply-service/**` (solo Listing y catálogo de productos), ports `IPropertyReferencePort`/`IListingReferencePort` + adapters en building-blocks, contratos/eventos de Property y Listing en `contracts/`, ramas BFF, tests.

**Entregables:** modelo mínimo **literal** de la task; commands `CreateProperty`, `UpdateProperty`, `AddPropertyInterest`, `CreateListing`, `UpdateListing`, `ActivateListing`, `PauseListing`, `CloseListing`; queries `SearchProperties`, `GetPropertyDetail`, `SearchListings`, `GetProductCatalog` (paginadas `PageV1`); eventos v1 de la task **+ `ListingUpdated`** (D18). Seed D21.

**BFF (screenIds de `screen-registry.ts` con task V2-PRP-001):** PRP-01, 03, 04, 05, 06, 10, 13 y LST-01, 02, 04, 05, 07, 09. **Quedan con datos demo** (no se implementan): PRP-02/07 (mapa), PRP-08, PRP-09 (multimedia), PRP-12 (unidades), LST-06. Documentarlas en el reporte.

**Reglas que más fácil se rompen:**
- Property ≠ Listing: owners distintos (`crm_property` vs `crm_supply`); supply **nunca** lee la base de property.
- Varios dueños por PropertyInterest; **no existe `ownerId` único** en Property.
- Cerrar un Listing no toca la Property. Nada se borra físicamente; Property se archiva (`lifecycleStatus`).
- `propertyTypeCode` y `operationTypeCode` validados contra catálogo guardando `catalogVersion`.
- Rural no se reduce a "terreno en m²" (D16).

**Frenar en:** política de activación distinta a D17 que pida la UI; cualquier publicación a portales.

### 7.2 V2-DMD-001 — Requirement, CaptationCase, Valuation, Mandate

**Depende de:** PRP-001 mergeada. **Write zone:** `services/demand-service/**`, `services/supply-service/**` (solo CaptationCase/Valuation/CommercialMandate; Listing solo vía su command existente), ports `IRequirementReferencePort`/`ICaptationReferencePort` + adapters, contratos/eventos en `contracts/`, ramas BFF, tests.

**Entregables:** modelo mínimo **literal** de la task; commands `CreateRequirement`, `UpdateRequirement`, `ActivateRequirement`, `OpenCaptationCase`, `UpdateCaptationCase`, `IssueValuation`, `GrantCommercialMandate`; endpoint `POST /api/v1/requirements/{id}/selected-listings` (D20); queries `GetRequirementDetail`, `SearchRequirements`, `GetCaptationDetail`, `SearchCaptations`; eventos v1 de la task. `commercialProgress` según D19.

**BFF:** DEM-01..06, CAP-01..08, LST-03 (crear publicación desde captación → llama a `CreateListing` existente).

**Reglas que más fácil se rompen:**
- Una Party con N Requirements activos; **sin deduplicar**.
- `IssueValuation` **siempre inserta** una nueva Valuation (historia); corregir = nueva versión, nunca update.
- Mandate `ACTIVE` → CaptationCase puede pasar a `CAPTURED`; recién ahí se habilita LST-03.
- Montos con `Money` (D15). Party/Property validadas por D13.
- **No hay colección ni entidad `Opportunity`**; nada de ManagedAgreement, pagos ni cronogramas.

**Frenar en:** si la UI pide cambio de etapa (es PIPE); cualquier documento/firma de mandato.

### 7.3 V2-MAT-001 — Matching explicable y selección

**Depende de:** DMD-001 mergeada. **Write zone:** `services/matching-service/**`, consumers de eventos, ramas BFF, tests. **No modifica** property, supply ni party; en demand-service solo **consume** el endpoint de D20.

**Entregables:** `MatchCase` literal de la task; queries `GetMatchesForRequirement`, `GetMatchDetail` (+ "búsquedas compatibles con una publicación" para MAT-03); commands `SelectListingForRequirement`, `DiscardMatch`; eventos `MatchCalculated`, `MatchPresented`, `MatchSelected`, `MatchDiscarded`, `MatchInvalidated`. Scoring D22 con tests unitarios exhaustivos (tabla de casos). Consumers D18.

**BFF:** MAT-01, 02, 03, 05, 06.

**Reglas que más fácil se rompen:**
- Duro incumplido nunca se compensa con blandos.
- La explicación dice qué dato produjo cada resultado y la `scoringPolicyVersion`.
- Cambio de fuente → `STALE` + `MatchInvalidated`; recalcular es explícito o al consultar.
- Sin ML, sin OpenSearch, sin servicio externo.

**Frenar en:** criterios o pesos no cubiertos por D22.

---

## 8. Condiciones de frenado (se suman a Wave 2 §8)

- Necesidad de leer la base de otro servicio (siempre por HTTP o eventos).
- Necesidad de modificar un contrato publicado de Wave 2 (se agrega uno nuevo o se pregunta).
- Aparece `Opportunity` como entidad/colección, portales, pagos, ManagedAgreement o documentos legales.
- Una decisión D12–D22 no alcanza para resolver algo: proponer opciones y esperar.

---

## 9. Prompt para el agente (Claude Code o Codex)

```text
Vas a ejecutar UNA task del CRM inmobiliario V2: <W3-0 | V2-PRP-001 | V2-DMD-001 | V2-MAT-001>.

Leé en este orden y seguilos al pie de la letra:
1) PRPs/_backlog/2026-09-19-plan-wave-3-nucleo-inmobiliario.md (manda sobre los planes anteriores donde difieran)
2) PRPs/_backlog/2026-09-17-plan-wave-2-acceso-catalogos-party.md secciones 5, 6 y 8
3) PRPs/_backlog/2026-09-16-plan-wave-1-fundacion.md secciones 2, 3 y 8
4) La task en docs/tasks/v2/ (salvo W3-0, que está definida en el plan §3)
5) "Contratos publicados" de los IMPLEMENTATION_REPORT de ACL-001, CAT-001, PTY-001 y de las tasks de Wave 3 ya mergeadas
6) Las skills de §4 que apliquen: leé a mano .github/skills/<skill>/SKILL.md (no asumas que se cargan solas)

Precedencia: ADR-001 y estos planes prevalecen sobre AGENTS.md, README.md y ARCHITECTURE.md.
En V2 NO existe tenantId/organizationId ni Opportunity como entidad.

- Verificá el Paso 0 (§2). Decisiones D1–D22 cerradas.
- Usá solo las skills de §4.
- Antes de escribir código mostrame un resumen de ≤10 líneas y esperá mi OK.
  Después del OK trabajá hasta terminar sin frenar, salvo condición de §8.
- Al terminar: test-fast.sh y test-integration.sh en verde, reporte con "Contratos publicados"
  y Handoff, commit único con el autor de git ya configurado (no lo cambies) y git add explícito,
  push -u. Si gh está autenticado,
  abrí el PR y esperá los checks; NO mergees.
No marques la task como DONE. No mergees. No reescribas historia de main ni de ramas con PR abierto.
```

---

## 10. Después de Wave 3

Wave 4: `PIPE-001` (proyección/fachada de oportunidades, tablero y **cambio de etapa persistido** sobre `commercialProgress` de Requirement/Captation — requiere corregir `cqrs-read-models-projections`) → `COM-001`. Plan propio.

---

## 11. Proceso y handoff (para quien continúe)

### 11.1 Preparación (una sola vez)
- Acceso a `GADS1-TEAM/real-estate-crm` con permiso de crear ramas y mergear PRs.
- Docker, .NET 10 SDK, Node 22, git. En Windows: `git config --global core.longpaths true`.
- `git config --global user.name "<tu nombre>"` y `user.email` (es el autor de los commits).
- Clonar el repo y verificar en `main`: `bash scripts/test-fast.sh` y `bash scripts/test-integration.sh` en verde.
- **Si el agente es Codex:** lee `AGENTS.md` solo (ver su bloque "Precedencia V2"); las skills **no** se cargan solas, por eso el prompt §9 pide leerlas. Usá un modo de aprobación que le permita correr `dotnet`, `docker` y red (restore de NuGet); si no, no puede correr los tests. Una sesión nueva por task.

### 11.2 Ciclo de una task (PowerShell, desde la carpeta del repo principal)
```bash
# arrancar
git checkout main; git pull
git worktree add ../crm-<task> -b <rama> main     # rama: feat/v2-<task>-<slug> | chore/... | fix/...
# abrir ../crm-<task> en el editor, sesión nueva del agente, pegar el prompt §9

# el agente muestra un resumen ≤10 líneas → revisarlo contra este plan → responder OK o corregir
# al terminar: el agente pushea; abrir PR a main si no lo abrió; esperar CI (jobs fast e integration)

# cerrar (después del merge; SIEMPRE desde el repo principal, nunca desde el worktree)
git checkout main; git pull
dotnet build-server shutdown
git worktree remove --force ../crm-<task>
git branch -d <rama>
git worktree prune
git remote prune origin

# marcar DONE (solo tasks del board; W3-0 no está en el board)
git checkout -b docs/board-<task>-done
# editar docs/tasks/v2/TASK_BOARD.md (TODO → DONE)
git add docs/tasks/v2/TASK_BOARD.md
git commit -m "docs(tasks): marcar V2-<TASK> como DONE"
git push -u origin docs/board-<task>-done        # → PR → merge
git checkout main; git pull; git branch -d docs/board-<task>-done
```
Para actualizar una rama de trabajo con `main`: `git merge origin/main` dentro del worktree (no abrir un PR de `main` hacia la rama).

### 11.3 Qué revisar en el resumen del agente (antes del OK)
- Que liste la write zone de §7 y no más.
- Que cite las decisiones D12–D22 que aplica y no invente otras en silencio.
- Que las preguntas que haga se respondan con opciones y queden como "decisiones del equipo" en el reporte.

### 11.4 Checklist antes de mergear un PR
- [ ] CI verde en `fast` e `integration`.
- [ ] Cambios solo en la write zone (sin `README.md`, `ARCHITECTURE.md`, `docs/adr`, `docs/implementation`, `design_handoff_*`, board, `apps/` salvo lo que la task permita).
- [ ] Sin `tenantId`, `organizationId`, `Opportunity` como entidad/colección.
- [ ] Autor del commit = persona real del equipo.
- [ ] Reporte con "Contratos publicados" y Handoff en la task; decisiones nuevas documentadas.
- [ ] Tests de infra con Trait `RequiresMongo`/`RequiresRabbitMq`/`RequiresKeycloak`; polling con timeout; base Mongo aislada.

### 11.5 Problemas conocidos
| Problema | Solución |
|---|---|
| `git worktree remove` falla con "Filename too long" o "Deletion failed" | Cerrar el editor del worktree, `dotnet build-server shutdown`; borrar la carpeta con `mkdir vacio; robocopy vacio <carpeta> /MIR; rmdir vacio, <carpeta>`; después `git worktree prune` |
| "'main' is already used by worktree" | Estás parado en el worktree: volvé a la carpeta del repo principal |
| Worktree "prunable" / "not a git repository" tras mover carpetas | No mover repos con worktrees vivos; si pasó: `git worktree repair <ruta-nueva>` desde el repo principal |
| Test de integración que falla a veces | Casi siempre es una carrera: esperar por polling con timeout y filtrar por el id del recurso propio |
| Autor de commit incorrecto (rama sin PR) | `git commit --amend --reset-author --no-edit` y `git push --force-with-lease` |
| Carpeta `Claude outputs/` sin trackear | No commitear; usar siempre `git add <archivo>` explícito |

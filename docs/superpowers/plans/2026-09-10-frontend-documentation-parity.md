# Frontend documentation parity

## Goal

Revisar y corregir las brechas verificables entre la documentación fuente y las dos aplicaciones frontend del repositorio, manteniendo `apps/crm-web` y `apps/platform-admin-web` como superficies independientes. El trabajo se limita a comportamiento, estados, navegación y contratos de presentación del frontend; no inventa un BFF, persistencia de dominio, Keycloak, MongoDB, RabbitMQ ni integraciones externas.

## Architecture

- `apps/crm-web` representa la experiencia CRM y conserva `DemoCrmDataSource` únicamente para revisión local explícita; el modo BFF seguirá mostrando estados de indisponibilidad cuando el contrato real no esté disponible.
- `apps/platform-admin-web` representa Platform Admin y conserva su boundary `PlatformAdminDataSource`; las mutaciones de revisión seguirán siendo locales y las mutaciones productivas continuarán dependiendo del BFF.
- No se compartirán entidades de dominio entre las dos apps ni se agregará `tenantId` a CRM, una entidad física `Opportunity`, tareas, notificaciones, pagos o envíos de correo/WhatsApp.
- La URL conservará `screen` y agregará el identificador de registro solo donde la superficie lo necesite, para que listas, búsqueda global y acciones de retorno abran el registro seleccionado sin convertir el frontend en fuente de verdad productiva.

## Tech Stack

- Next.js 15, React 19, TypeScript, CSS local y Vitest/Playwright existentes en cada app.
- Componentes primitivos existentes (`Alert`, `Toast`, `Tabs`, `Pagination`, `Modal`, `Drawer`) y reducers existentes como puntos de integración.
- Voseo, formato monetario argentino, estados `UNKNOWN`, targets táctiles de al menos 44px y alertas persistentes/toasts transitorios según los handoffs.

## Spec

### Fuentes de verdad

- `README.md`, `ARCHITECTURE.md`, `AGENTS.md`.
- `design_handoff_crm/README.md` y sus 172 referencias; `apps/crm-web/docs/SCREEN_TRACEABILITY.md`.
- `design_handoff_platform_admin/README.md` y `apps/platform-admin-web/docs/SCREEN_MATRIX.md`.
- `docs/tasks/v2/TASK_BOARD.md` y documentos de tareas V2 relacionados con Party, Property, Demand, Matching, Pipeline, Commercial, Activity, Analytics, AI, ACL, Catalogs y release.

### Casos de uso y owners revisados

- CRM: UX-001/002/003; Party/PTY owner; Property/PRP owner; Listing/LST owner; Demand/DEM owner; Matching/MAT read model; CommercialPipeline/OPP façade; Commercial/COM owner; Activity/ACT owner; Analytics/ANA read model; AI/IA review-only; Access/ACL y Catalog/ADM; estados globales/auth.
- Platform Admin: revisión de overview y catálogo, creación/edición de draft, validación, publicación/deprecación, precedencia efectiva, tenants/capabilities, auditoría, correcciones, promoción y permisos.

### APIs, eventos y write zones

- CRM consume `GET /screens/:screenId` y envía mutaciones al boundary `POST /mutations/:name` solo en modo BFF; no agrega acceso directo a DB ni eventos. Las acciones de demo escriben exclusivamente el `DemoStore` local explícito.
- Platform Admin consume sus endpoints de overview/drafts/validate/publish/deprecate/corrections mediante `PlatformAdminDataSource`; la revisión local escribe `ReviewState` local y no simula el BFF productivo.
- Las write zones quedan en cada app/BFF respectivo; Party, Property, Requirement/Captation, CommercialPipelineItem/Activity y artefactos/configuración no se editan desde la otra superficie.

### Acceptance criteria

1. La auditoría no altera el inventario: CRM conserva 172 pantallas (137 V2 y 35 F2) y Platform Admin conserva 72 pantallas / 153 referencias sin mezclar módulos.
2. Cada navegación desde búsqueda, tabla, tablero y acción de retorno conserva el registro elegido; no se usa silenciosamente el primer elemento de un array cuando existe una selección.
3. Las acciones documentadas como confirmables/explicables exigen los datos mínimos (motivo de etapa/cierre, fecha y valor final de cierre, motivo de resolución/cancelación) y mantienen historial; estados cerrados no ofrecen mutaciones incompatibles.
4. Parties distinguen contacto/empresa, estados comerciales e identidad, y el alta asistida puede revisar texto histórico sin enviar mensajes. Activities exigen fecha ocurrida, actor y relación válida; email/WhatsApp siguen siendo históricos manuales.
5. Matching explica contribuciones y desconocidos sin convertir `UNKNOWN` en falso/cero; Opportunity conserva `sourceType`; Analytics no expone una exportación no documentada; AI permite revisar/aceptar/editar/rechazar sin mutar dominio automáticamente.
6. Platform Admin conserva solo lectura para publicados/overrides, validación visible y persistente, correlation IDs copiables, auditoría con contexto antes/después cuando existe y feedback de publicación enlazable al registro de auditoría.
7. Permisos, estados vacíos/carga/error/readonly/conflicto/sesión y responsive móvil son visibles y accionables según la documentación; ningún botón mostrado como disponible queda sin handler.
8. Cada comportamiento nuevo tiene prueba unitaria o de integración de frontend que primero falla por la brecha y luego pasa; lint, typecheck, build y suites existentes de ambas apps se ejecutan desde una ruta corta si OneDrive bloquea esbuild. La validación renderizada cubre desktop y mobile con consola limpia, sin overlay de framework.

## Implementation plan

### Task 1 — Freeze the audit baseline and add parity test helpers

Files:

- `apps/crm-web/src/lib/demo-store.ts`
- `apps/crm-web/src/lib/screen-registry.ts`
- `apps/crm-web/src/lib/*.test.ts` or new focused tests under `apps/crm-web/src/lib/`
- `apps/platform-admin-web/src/lib/*.test.ts` or new focused tests under `apps/platform-admin-web/src/lib/`

Steps:

1. Add pure helpers for selected-record resolution, documented commercial statuses, close validation, activity validation, and Platform Admin audit/correlation presentation.
2. Write focused tests for missing behavior, run them through the short-path test command, and confirm each fails for the intended missing behavior.
3. Implement the smallest reducer/helper changes, then rerun focused and existing tests.

### Task 2 — Make CRM navigation and primary record workflows selection-safe

Files:

- `apps/crm-web/src/components/crm-workspace.tsx`
- `apps/crm-web/src/components/crm-shell.tsx`
- `apps/crm-web/src/lib/demo-store.ts`
- `apps/crm-web/src/components/ui/primitives.tsx` only when a shared primitive needs a documented accessibility/interaction fix
- CRM tests covering URL selection and reducer transitions

Steps:

1. Carry a selected record ID in navigation and resolve it by collection/type for Party, Property, Listing, Captation, Demand, Matching, Opportunity, Reservation and Operation views.
2. Replace no-op tab/filter/pagination/cancel actions in the touched primary flows with local controlled state and existing primitives.
3. Preserve disabled reasons and readonly/empty/error behavior, and ensure F2 routes remain deferred rather than gaining undocumented product scope.
4. Add tests for selection, tab/filter actions and closed-record action availability.

### Task 3 — Align CRM domain-facing forms with documented invariants

Files:

- `apps/crm-web/src/lib/demo-store.ts`
- `apps/crm-web/src/lib/permissions.ts`
- `apps/crm-web/src/components/crm-workspace.tsx`
- `apps/crm-web/src/components/domain/domain-components.tsx`
- CRM domain/reducer/component tests

Steps:

1. Distinguish Contact and Company capture/detail presentation, add the documented commercial/identity status vocabulary without destructive deletion, and add the fifth persona used by the handoff while keeping authorization visible.
2. Add reviewable historical-text prefill for quick capture, with no outbound send behavior.
3. Enforce required occurred facts and relationships for activities, proposal/reservation/operation cancellation or resolution, opportunity stage changes and closure; preserve append-only history and explicit `UNKNOWN`.
4. Make matching explanations criterion-level and make AI suggestion decisions review-only and dismissible with history.
5. Add tests first for each pure validation/reducer rule and then for the connected UI behavior.

### Task 4 — Align CRM analytics, admin and global surfaces

Files:

- `apps/crm-web/src/components/crm-workspace.tsx`
- `apps/crm-web/src/lib/demo-store.ts`
- `apps/crm-web/src/lib/permissions.ts`
- CRM tests

Steps:

1. Remove or disable undocumented analytics export behavior while retaining the documented filters, definitions, lineage and `UNKNOWN` treatment.
2. Make role/permission explanations reflect the five documented personas and keep unauthorized actions visible but disabled with a reason.
3. Complete quick-create, onboarding/session recovery and global-state actions with safe local feedback or explicit BFF-unavailable behavior; do not introduce productive fallbacks.
4. Verify no task, reminder, notification, payment, rental or external-send affordance appears in V2.

### Task 5 — Close Platform Admin documentation gaps without changing ownership

Files:

- `apps/platform-admin-web/src/lib/types.ts`
- `apps/platform-admin-web/src/lib/review-store.ts`
- `apps/platform-admin-web/src/components/platform-app.tsx`
- `apps/platform-admin-web/src/components/platform-workspace.tsx`
- `apps/platform-admin-web/src/components/platform-shell.tsx`
- Platform Admin tests

Steps:

1. Add accessible copy affordances for correlation IDs and documented error context.
2. Preserve before/after/context in correction audit entries and expose the audit destination from publication feedback when the local review state can prove it.
3. Keep published artifacts/tenant overrides readonly, draft references validated, warning/error drawers persistent, and permission-restricted actions visible with reasons.
4. Add focused tests for audit evidence, copy affordance state and draft/publish guards.

### Task 6 — Verify, document and report

Files:

- `apps/crm-web/docs/SCREEN_TRACEABILITY.md`
- `apps/platform-admin-web/docs/SCREEN_MATRIX.md` only if a verified behavior mapping changes
- `docs/tasks/v2/wave-0-scope-design/IMPLEMENTATION_REPORT.md` only for evidence directly produced by this work
- this plan file if execution notes need correction

Steps:

1. Run focused tests, full unit suites, lint, typecheck and production builds for both apps from a short mapped path if needed.
2. Run Playwright render/interaction checks at desktop and mobile sizes, collect screenshots outside the repository, inspect console output and record any unavailable BFF/file:// comparison explicitly.
3. Reconcile every V2/F2 registry entry and every acceptance criterion against fresh evidence; report implemented, intentionally deferred and externally blocked items separately.
4. Before claiming completion, rerun the exact verification commands and inspect `git diff`/`git status` to ensure unrelated user changes remain untouched.

# Handoff: Platform Admin / Pack Studio

Backoffice interno de plataforma del CRM inmobiliario. **No** es el CRM que usan las inmobiliarias: es la aplicación separada con la que el equipo de producto/plataforma configura, versiona, valida, publica, pilotea y audita el comportamiento configurable del producto.

Repo: `GADS1-TEAM/real-estate-crm` · branch `main`

---

## 1. Antes de escribir código: leé esto

### Los archivos de `designs/` son **referencias de diseño**, no código de producción

Son prototipos en HTML que muestran el aspecto y el comportamiento buscados. **La tarea es recrear estos diseños en el entorno del codebase** (framework, librería de componentes y patrones que ya existan en el repo), no copiar el HTML. Si todavía no hay frontend definido, elegí el stack apropiado para el proyecto e implementá los diseños ahí.

Cada archivo es un **HTML autocontenido**: se abre con doble click en cualquier navegador, sin servidor y sin internet. Todo está embebido (el design system Brick, las fuentes, el runtime del prototipo). Abrilos para ver y clickear el diseño; leé su código fuente para extraer valores exactos.

Son grandes (~900 KB cada uno) justamente porque llevan Brick adentro. Ese peso es del prototipo, no del diseño: no lo tomes como referencia de nada.

### Fidelidad: **alta (hifi)**

Colores, tipografía, espaciado, estados e interacciones son finales. Recreá la UI con fidelidad usando la librería de componentes del codebase. Los textos de la interfaz también son finales: están en español de Argentina con voseo y se pueden usar tal cual.

### Lo que el diseño da por cierto del dominio

Estas nueve cosas salieron de leer `README.md` y `ARCHITECTURE.md`, y **contradicen** suposiciones habituales. Si al implementar encontrás que alguna no se sostiene, es un tema de diseño, no de código: avisá antes de resolverlo por tu cuenta.

1. **No hay un solo "pack argentino".** `README.md` §4.5 define cinco packs iniciales (Argentina Base, Argentina Compliance, Argentina Closing, Commission, Automation Starter). El principio es explícito: *no construir un mega Config Service*. Cada bounded context sigue siendo dueño de su semántica y el `PackManifest` **coordina versiones** de plantillas ajenas. Un pack es una **composición de referencias a versiones publicadas**, no un contenedor que edita todo adentro.
2. **`Capability` tiene estado por organización**: `DISABLED · ENABLED · PILOT · RESTRICTED`. El piloto ya existe en el dominio como estado de capability por tenant; no hay que inventar un segundo mecanismo de targeting.
3. **Toda etapa de un workflow mapea a un estado semántico obligatorio** (`README.md` §24): `PREPARING · DUE_DILIGENCE · READY_TO_CLOSE · CLOSED · CANCELLED · FAILED`. Una etapa sin ese mapeo es un bloqueo de publicación.
4. **Los checklists no son un artefacto aparte.** `README.md` §10.2 los define dentro del `WorkflowTemplate` (`stages[] + tasks[] + documentRequirements[] + approvals[] + SLAs[]`). No hay módulo "Checklists": la vista previa del workflow *es* el checklist como lo verá la administrativa.
5. **`ExtensionSchema` tiene forma fija**: `namespace · entityType · schemaVersion · typedFields · validationRules`, con prohibición explícita de un `customFields` libre. Una clave que colisiona con un campo core es bloqueo, no aviso.
6. **Los overrides de tenant se editan en el CRM, no acá.** `COMI-001`/`COMI-002` tienen superficie CRM Web. Platform Admin define defaults y límites (`PLT-012`). En el backoffice el override es **visible y explicable, nunca editable**.
7. **Dos niveles de catálogo.** `CAT-001…006` son catálogos que administra cada tenant en su CRM. `PLT-006` son los catálogos **base** que provee el pack. En el backoffice se llaman "catálogos base".
8. **"No metric without lineage".** `MetricDefinition` exige `sourceEvents[]`, `dimensions[]`, `exclusions[]`, `formulaVersion`. Publicar una métrica sin eventos fuente es bloqueo.
9. **Las correcciones son comandos del owner.** `ARCHITECTURE.md` §12: *"ejecutar correcciones administrativas autorizadas mediante comandos del servicio owner"*. Nada más. El catálogo solo lista comandos declarados por cada servicio; si no hay, lo dice.

### Reglas no negociables que el código debe hacer cumplir

- Platform Admin **jamás** escribe MongoDB directamente.
- No redefine `Party`, `Property`, `Listing`, `Requirement`, `Transaction`, aggregates ni invariantes core.
- Una versión publicada es **solo lectura**. "Editar" sobre una publicada crea un borrador de la siguiente.
- Publicar **no migra**: las instancias conservan `appliedPackVersion` / `policyVersion`. Ningún cálculo o caso histórico se recalcula.
- Deprecar **no borra**: deja de ofrecerse para instancias nuevas; las pinned siguen funcionando.
- Un pack no puede referenciar un **borrador** ajeno. Bloqueo.
- Los tenant overrides nunca modifican el default global.
- Todo cambio de estado se audita: actor, motivo, `before`/`after`, `correlationId`.

---

## 2. Modelo de configuración: cinco capas

Es el modelo mental que toda la UI expone. Implementarlo mal acá se nota en cada pantalla.

| Capa | Qué es | Quién edita | Ejemplo |
|---|---|---|---|
| 1 · Pack publicado | Composición versionada de referencias a artefactos publicados, para un mercado | Platform Admin | `Argentina Base Pack v8` |
| 2 · Artefacto versionado | El default global de una regla, con su ciclo `DRAFT → PUBLISHED → DEPRECATED` | Platform Admin | `CommissionPolicy SALE_DEFAULT v5 = 4 %` |
| 3 · Estado por organización | Qué capabilities tiene cada tenant y en qué estado | Platform Admin (pilotos) · onboarding | `RENTAL_ADMINISTRATION = PILOT` |
| 4 · Override del tenant | La inmobiliaria adapta el default dentro de los límites | Director/Admin del tenant, **en CRM Web** | `Pepito Commission v2 = 3 %` |
| 5 · Configuración efectiva | Lo que el CRM aplica en un contexto concreto | **Nadie**: se resuelve y se explica | `3 %` porque el override tiene precedencia |

La capa 5 es la razón de existir de la pantalla `PA-132` (Inspector de configuración efectiva). Es la pantalla más valiosa del backoffice: responde *"¿por qué a esta inmobiliaria le calcula 3 % y a la otra 4 %?"* sin abrir Mongo ni logs.

### Dos estados que no hay que confundir

- **Estado de la versión del artefacto**: `DRAFT · PUBLISHED · DEPRECATED`. Global, lo maneja Platform Admin. Responde *"¿esta definición está lista?"*.
- **Estado de una capability en una organización**: `DISABLED · ENABLED · PILOT · RESTRICTED`. Por tenant. Responde *"¿esta inmobiliaria la tiene?"*.

---

## 3. Los cuatro verbos y la arquitectura de información

Toda la app se organiza en cuatro verbos, no en 25 CRUDs: **AUTHOR → VALIDATE → RELEASE → EXPLAIN**.

Sidebar de 236 px, siete grupos:

```
OVERVIEW           Inicio · Centro de atención · Historial de producto
PACKS              Packs · Capabilities · Catálogos base
REGLAS OPERATIVAS  Workflows · Requisitos documentales · Compliance · Comisiones · Automatizaciones
DATOS              Métricas
INTEGRACIONES      Conectores · Feature flags · Extension Schemas
RELEASE            Impacto · Pilotos y rollout · Ambientes
DIAGNÓSTICO        Tenants (solo lectura) · Auditoría · Correcciones
```

### Shell (siempre presente)

| Elemento | Especificación |
|---|---|
| Sidebar | 236 px, fondo `--eminent-dark`, ítems de 30 px, contadores de borradores y avisos por grupo |
| Top bar | 44 px. Búsqueda global (`Ctrl K`) · **badge de ambiente siempre visible** · usuario con sus permisos |
| Ambiente en Production | La top bar lleva además una línea superior de 2 px `--eminent-primary`, para que ningún screenshot sea ambiguo |
| Breadcrumb | `Tipo › Artefacto › vN`, una sola línea con scroll horizontal, siempre clickeable |
| `VersionHeader` | Fijo bajo el breadcrumb en cualquier artefacto. **Es el único lugar desde donde se publica** |
| Validation drawer | 360 px a la derecha, se abre desde el ícono de estado, persiste al navegar dentro del editor |
| Toast | Toda publicación produce un toast con link a la entrada de auditoría |

### Patrón de navegación de artefacto (idéntico para los 11 tipos)

Lista → Detalle con pestañas (`Resumen · Versiones · Contenido/Composición · Dependencias · Uso · Comparar · Historial · Auditoría`) → Editor del borrador.

La URL siempre lleva tipo, ID y versión: `/workflows/residential-sale/v7`. Una versión publicada abre en solo lectura con "Crear nueva versión" como única acción de escritura.

---

## 4. Taxonomía: 11 artefactos versionados

El **owner** importa: `ARCHITECTURE.md` §6 asigna cada artefacto a un servicio y el BFF solo compone. La UI tiene que saber quién publica para mostrar el evento correcto y para no prometer ediciones que el owner no expone.

| Artefacto | Campos del dominio | Owner |
|---|---|---|
| `PackManifest` | Referencias versionadas por contexto, mercado, capabilities incluidas | `platform-config` |
| `Capability` | ID, nombre, categoría, requiere/incompatible, aplicabilidad + estado por organización | `platform-config` |
| `Catalog` (base) | Entradas con `code` estable, label, orden, `deprecated` | `platform-config` |
| `WorkflowTemplate` | `operationType · jurisdiction · packVersion · stages[] · tasks[] · documentRequirements[] · approvals[] · SLAs[]` | `closing-service` |
| `DocumentRequirement` | `requirementCode · appliesTo · role · operationType · jurisdiction · requiredLevel · validityRules · acceptedDocumentTypes[]` | `documents-compliance` |
| `CompliancePolicy` | Reglas que abren/satisfacen `ComplianceCase`; UIF 43/2024 como pack argentino | `documents-compliance` |
| `CommissionPolicy` (default) | `scope · operationTypes[] · calculationRules[] · splitRules[] · incentiveRules[] · validFrom/Until · version` | `commission-service` |
| `AutomationPolicy` (starter) | `trigger · conditions[] · actions[] · SLA? · approvalPolicy? · enabled · version` + autonomía | `automation-ai` |
| `MetricDefinition` | `metricCode · semanticDefinition · formulaVersion · sourceEvents[] · dimensions[] · exclusions[] · effectiveFrom` | `analytics-copilot` |
| `Connector` · `FeatureFlag` | Conector: proveedor, acciones, eventos, mercados, prerequisitos. Flag: valor por ambiente, historial | `platform-config` |
| `ExtensionSchema` | `namespace · entityType · schemaVersion · typedFields · validationRules` | `platform-config` |

---

## 5. Design tokens

**Todo sale del design system Brick, tema `eminent`.** No inventes valores: usá los tokens. Los archivos de diseño cargan `_ds/brick-design-system-.../tokens/*.css` y referencian todo por `var(--*)`.

### Densificación propia del backoffice

Es una herramienta interna profesional, desktop-first. Tres cambios sobre el Brick del CRM:

| Token | Valor |
|---|---|
| Fila de tabla | `min-height: 44px` · padding `9px 14px` (alto automático, **nunca** `height` fijo: el contenido de dos o tres líneas se pisa) |
| Fila de árbol/lista | `min-height: 30px` · padding `6px 12px` |
| Control inline | 26–28 px |
| Sidebar | 236 px · ítem 30 px |
| Top bar | 44 px |
| Drawer de propiedades | 320 px · validación 360 px |
| Gap entre cards | 12 px · padding interno 14 px |
| Ancho mínimo del shell | **1260 px** con scroll horizontal. Debajo de eso no se comprime: scrollea |

### Tipografía

| Uso | Valor |
|---|---|
| Título de pantalla | 20/26 semibold, `letter-spacing: var(--ls-tighter)` |
| Título de sección | 14/19 semibold |
| Cuerpo denso (base de tablas y editores) | 13/18 regular |
| Secundario | 12/17 regular, `--text-secondary` |
| Overline | 10/14 semibold, uppercase, `--ls-overline` |
| Monoespaciada | 11–12 px, `ui-monospace, "SF Mono", Menlo, Consolas, monospace` |

**La monoespaciada es para todo lo que es identidad**: códigos, versiones, keys, IDs, `correlationId`. Nunca para prosa.

### Color de estado: un significado por color

| Token | Significa |
|---|---|
| `--state-success` | Publicado · válido · agregado |
| `--state-warning` | Borrador · warning · modificado |
| `--state-error` | Blocker · quitado · fallido |
| `--state-info` | En revisión · info · condicional |
| `--eminent-primary` | Producción · piloto · selección · acción primaria |
| `--grey-30` | Deprecado · no aplica · deshabilitado |

> `--grey-70` y `--grey-90` **no existen** en Brick. La escala es `grey-20 → grey-80`.

### Accesibilidad

Severidad **nunca** solo por color: `BLOCKER` siempre con glifo `✕`, `WARNING` con `⚠`, `INFO` con `i`, OK con `✓` (WCAG 2.2 AA).

---

## 6. Componentes

### De Brick, tal cual

`Button` (primary/outline/simple, size small) · `Alert` (info/warning/error/success) · `Avatar` · `Stepper` · `Table` · `Card` · `Chip` · `Tag` · `Modal` · `SideDrawer` · `Tabs` · `Timeline` · `Select` · `TextInput` · `Switch` · `Checkbox` · `RadioButton` · `Slider` · `ProgressBar` · `Breadcrumbs`.

### Propios del backoffice (41)

Los nueve **patrones transversales** están especificados con todos sus estados en `designs/02 - Design System.dc.html`. Abrí ese archivo antes de construir cualquier pantalla: es la fuente de verdad de estos componentes.

| Grupo | Componentes |
|---|---|
| Shell | `AppShell` `AdminSidebar` `TopBar` `GlobalSearch` `CommandPalette` `EnvironmentBadge` |
| Artefactos | `ArtifactTable` `ArtifactCard` `ArtifactTypeBadge` `ArtifactStatusBadge` |
| Versionado | `VersionBadge` `VersionHeader` `VersionSelector` `VersionTimeline` `VersionHistory` `VersionCompare` `DraftIndicator` |
| Relaciones | `DependencyGraph` `DependencyList` `ReferencedByPanel` |
| Cambios | `ChangeSummary` `SemanticDiff` `TechnicalDiff` `BeforeAfterDiff` |
| Validación e impacto | `ValidationSummary` `ValidationIssue` `ValidationDrawer` `ImpactSummary` `ImpactMetric` `AffectedTenantsList` `ImpactIssue` |
| Publicación | `PublishButton` `PublishReview` `PublishConfirmation` `DeprecationBanner` `DeprecationFlow` |
| Reglas | `RuleBuilder` `ConditionGroup` `ConditionRow` `OperatorSelector` `TypedValueInput` `ScopeBuilder` `PolicyEditor` `PolicyRule` `PolicyPriority` |
| Workflow | `WorkflowCanvas` `WorkflowStage` `WorkflowTaskNode` `WorkflowConditionNode` `WorkflowPropertiesPanel` |
| Dominio | `CatalogEditor` `CatalogEntryRow` `RequirementMatrix` `CommissionRuleEditor` `SplitEditor` `CommissionSimulator` `AutomationTrigger` `AutomationCondition` `AutomationAction` `AutonomySelector` `AutomationSimulator` `MetricFormulaBuilder` `LineageGraph` `ConnectorCard` `ConnectorCapabilityList` `FeatureFlagControl` `SchemaFieldBuilder` `SchemaPreview` |
| Release | `PilotCard` `PilotProgress` `RolloutTimeline` `CohortSelector` `EnvironmentCompare` `PromotionFlow` |
| Diagnóstico y gobierno | `TenantPicker` `EffectiveConfigurationTree` `ConfigurationResolutionExplanation` `AuditTable` `AuditEntry` `AdministrativeCorrectionFlow` |

### Los cuatro patrones que más código tocan

**`VersionHeader`** — cuatro estados: con borrador y blocker · publicada sin borrador · deprecada · sin permiso de publicar. Muestra `Published v6 · Draft v7 · 7 cambios sin publicar` + contadores de validación + acciones. Publicar deshabilitado **siempre con motivo visible**. Un solo borrador vivo por artefacto (evita merges; si hace falta ramificar, se duplica el artefacto).

**`ValidationSummary` + `ValidationDrawer`** — tres estados: `Válido` / `N warnings` / `Bloqueado`. Agrupado por `Estructura · Dependencias · Compatibilidad · Referencias · Reglas de negocio · Deploy`. **Click en un problema navega al nodo exacto del editor y lo resalta.** Los warnings se aceptan con motivo; los blockers no.

**`SemanticDiff`** — la fila se lee en lenguaje de negocio, no en JSON:

```
BIEN:  Comisión de venta          3 %  →  4 %
MAL:   rules[3].percentage      old:3  →  new:4
```

Cada tipo de artefacto declara su propio *describer*: el owner define cómo se lee un cambio en su lenguaje. La vista técnica (JSON patch) es un toggle opcional.

**`PublishReview`** — cuatro pasos: validación → dependencias → impacto → confirmación. Con blockers, el paso 4 no existe. Con warnings, hay que aceptarlos uno a uno con motivo. **La confirmación tipeada (escribir el nombre del artefacto) aparece solo cuando hay tenants afectados**; un catálogo que nadie usa se publica sin ceremonia.

---

## 7. Estados obligatorios en toda pantalla

| Estado | Tratamiento |
|---|---|
| Vacío declarado | Dice qué no hay y qué acción lo crea: *"Todavía no hay pilotos. Creá uno desde una versión publicada."* |
| Cargando | Skeleton de filas de 44 px. **Nunca** spinner a pantalla completa |
| Error | Qué falló y qué hacer. El `correlationId` se copia con un click |
| Sin permiso | Qué permiso falta y quién lo tiene. La acción se ve **deshabilitada, no desaparece** |
| Solo lectura | Versión publicada o deprecada: campos como texto, "Crear nueva versión" arriba |
| Referencia desactualizada | Ámbar + "Actualizar a v4" |
| Conflicto de dependencia | Rojo, con el par en conflicto nombrado |
| Usado por tenants activos | Contador junto a toda acción destructiva: *"Deprecar · 37 tenants"* |
| Piloto activo | Badge navy con cantidad de tenants; la versión de producción al lado |
| Promoción pendiente/fallida | Cola con destino y hora; fallida muestra la causa y "Reintentar" |
| Guardado / sin guardar | *"Guardado hace 3 s"* gris · *"Guardando…"* ámbar · error rojo con reintento |
| Seleccionado | Fondo `--eminent-light` + borde 2 px `--eminent-primary` |
| Mejor en escritorio | Editor de workflow, diff y matriz bajo 1024 px: aviso arriba, solo lectura |

### Autosave: `SAVE ≠ PUBLISH`

Los borradores se guardan solos, por campo. Esta distinción tiene que ser **extremadamente clara** en la UI. Descartar pide confirmación solo si hay más de un cambio; con uno, se descarta y se ofrece deshacer 10 s.

---

## 8. Las 72 pantallas

El inventario completo, con propósito, usuario, entradas, acciones, artefacto, estados, dependencias y UC trazado, está en `designs/01 - Arquitectura, Modelo e Inventario.dc.html` (sección 06, filtrable). `designs/00 - Mapa de Navegacion.dc.html` lista las 72 con su entrada y su estado de diseño.

Resumen por módulo:

| Rango | Módulo | Archivo de referencia |
|---|---|---|
| `PA-001` `PA-002` `PA-150…153` | Overview · Sistema · transversales | `03 - Pack Studio` |
| `PA-010…017` | Packs | `03 - Pack Studio` |
| `PA-020…023` | Capabilities | `03 - Pack Studio` |
| `PA-030…034` | Catálogos base · panel de validación | `03 - Pack Studio` |
| `PA-040…044` | Workflows | `04 - Rule Studios` |
| `PA-050…053` | Requisitos documentales | `04 - Rule Studios` |
| `PA-060…063` | Compliance | `04 - Rule Studios` |
| `PA-070…075` | Comisiones | `04 - Rule Studios` |
| `PA-080…083` | Automatizaciones | `04 - Rule Studios` |
| `PA-090…093` | Métricas | `04 - Rule Studios` |
| `PA-100…102` | Conectores · Feature flags | `05 - Release y Gobierno` |
| `PA-110…113` | Extension Schemas | `05 - Release y Gobierno` |
| `PA-120…127` | Impacto · Pilotos · Rollout · Ambientes · Promoción | `05 - Release y Gobierno` |
| `PA-130…132` | Diagnóstico de tenants · Inspector | `05 - Release y Gobierno` |
| `PA-140…143` | Auditoría · Correcciones | `05 - Release y Gobierno` |

De las 72, **55 están construidas y navegables** en los prototipos. Las 17 restantes tienen su patrón resuelto en el Design System y una pantalla hermana idéntica navegable (una lista, un diff, un detalle): son repeticiones del mismo patrón, no diseño faltante.

---

## 9. Los diez journeys críticos

Todos son navegables de punta a punta en los prototipos y **todos incluyen al menos un bloqueo real que hay que resolver dentro del flujo**. Eso no es decorativo: es el principio central del producto, *hacer un cambio es fácil, publicar un cambio peligroso por accidente es difícil*. Al implementar, estos bloqueos son requisitos funcionales.

| # | Journey | Bloqueo que el flujo tiene que producir y dejar resolver |
|---|---|---|
| J1 | Nueva versión del pack | El pack referencia un workflow que sigue en borrador ajeno |
| J2 | Modificar comisión default | Los splits deben sumar 100 %; un override de tenant queda fuera del nuevo límite |
| J3 | Modificar workflow de compraventa | La tarea nueva depende de una tarea de una etapa posterior |
| J4 | Crear requisito documental | Dos reglas activas aplican al mismo contexto (solapamiento en la matriz) |
| J5 | Modificar automatización | `AUTO_EXECUTE_WITH_GUARDRAILS` sin límite de volumen definido |
| J6 | Deprecar capability | Un pack publicado la requiere; se resuelve sin salir del flujo |
| J7 | Pilotear una configuración | Una inmobiliaria no cumple las dependencias de la capability |
| J8 | Debuggear una inmobiliaria | Sin bloqueo: es solo lectura. Explica la resolución en cuatro familias de reglas |
| J9 | Promover entre ambientes | Falta una dependencia en destino; y falta permiso `PROMOTE` según el usuario |
| J10 | Corrección administrativa | Sin `ADMIN_CORRECTION`, el paso 3 explica qué permiso falta y quién lo tiene |

### Detalle de los editores más complejos

**Workflow editor (`PA-042`)** — tres paneles: izquierda árbol de etapas y tareas · centro flujo lineal con ramas · derecha propiedades del nodo. Tipos de nodo: `Etapa · Tarea · Aprobación · Requisito · Rama condicional · Cierre`. **Evitar un canvas BPMN infinito**: representamos workflows inmobiliarios, no software arbitrario. Validaciones: etapa sin estado semántico (blocker) · cierre sin condición de finalización (blocker) · tarea sin rol responsable (warning) · dependencia hacia una etapa posterior (blocker) · referencia documental a una etapa renombrada (warning).

**Matriz de requisitos (`PA-051`)** — `operación × etapa × rol de Party`. La celda es el `requiredLevel`: `✓` requerido · `◐` condicional · `○` recomendado · `—` no aplica · `⚠` dos reglas se solapan · `∅` hueco (rol sin regla). Click en celda lista los requisitos; una celda vacía prellena el alcance de un requisito nuevo.

**Comisiones (`PA-072…074`)** — `PolicyPriority` visualiza qué regla gana: la más específica, con la fila que aplica resaltada y las demás atenuadas pero visibles. Es el **mismo componente en el editor y en el inspector**. El simulador toma contexto (operación, monto, moneda, sucursal, roles, fecha, tenant opcional) y devuelve política elegida, por qué, bruto, splits, incentivos y avisos. **La simulación no es un cálculo contable**: el real lo hace `commission-service` al cerrar, con la `policyVersion` fijada.

**Automatizaciones (`PA-081`)** — patrón `WHEN` (evento) / `IF` (condiciones tipadas) / `THEN` (acciones permitidas) / `WITH` (autonomía). Los cuatro modos de autonomía (`SUGGEST_ONLY` · `REQUIRE_APPROVAL` · `AUTO_EXECUTE_WITH_GUARDRAILS` · `AUTO_EXECUTE`) están **siempre visibles con su nivel de riesgo**, nunca escondidos en un dropdown, y cada uno lista sus requisitos con estado. Cada acción declara si es reversible y qué capability requiere.

**Inspector de configuración efectiva (`PA-132`)** — contexto (tenant, sucursal, operación, rol, fecha) → pack · default global · override del tenant · efectiva · **por qué**. Cuatro pestañas, y cada familia tiene su propia regla de precedencia, que hay que implementar distinto:

- **Comisiones**: el override del tenant gana sobre el default; un override por sucursal ganaría sobre el de organización.
- **Workflows**: no son sobrescribibles por el tenant. Efectiva = default.
- **Requisitos documentales**: los de plataforma son piso obligatorio; el tenant puede **agregar** los suyos, no quitar. Efectiva = unión.
- **Automatizaciones**: el tenant puede **bajar** la autonomía, nunca subirla por encima de lo que el pack permite.

---

## 10. Permisos

Ocho permisos: `VIEW · EDIT_DRAFT · PUBLISH · DEPRECATE · MANAGE_PILOTS · PROMOTE · ADMIN_CORRECTION · VIEW_AUDIT`.

> **Supuesto de diseño.** El dominio no define roles internos de plataforma; `ARCHITECTURE.md` §4 dice solo *"equipo interno de producto/plataforma"* y `W3-PLT-01` pide *"permisos backoffice/auditoría"*. Las cinco personas y estos ocho permisos son una propuesta para que `access-service` formalice.

Lo que **sí** es regla de UX: **una acción bloqueada dice por qué y quién puede hacerla.** *"Podés ver pero no publicar: requiere PUBLISH en Workflows. Lo tienen Rodrigo Vergara y Elena Vergara."* La acción se ve deshabilitada, no desaparece.

Cinco personas: Configuradora de producto (`EDIT_DRAFT`) · Release manager (`PUBLISH` `PROMOTE` `MANAGE_PILOTS` `DEPRECATE`) · Compliance/Legal (`PUBLISH` solo en Compliance) · Soporte de plataforma (`ADMIN_CORRECTION` `VIEW_AUDIT`) · Admin de plataforma (todos).

---

## 11. Trazabilidad

`PLT-001` a `PLT-020` están cubiertos, más seis superficies de otros bounded contexts (`COMI-008`, `ORG-011`, `CAT-006`, `ORG-001`, `DOC-014`, `AUT-001/002`). La matriz completa **caso de uso → pantalla → acción → componente** está en `designs/00 - Mapa de Navegacion.dc.html` (sección 04) y en `designs/01` (sección 07).

⚠️ **Diez UCs están nombrados en las tasks pero sin describir** en el repositorio: `PLT-002 · 003 · 005 · 007 · 008 · 015 · 016 · 017 · 018 · 020`. Su significado se **infirió del título de la task** y está marcado `inferido` en las matrices. Las pantallas están diseñadas; la semántica hay que confirmarla antes de implementarlas (ver `PQ-06`).

---

## 12. Supuestos de diseño y preguntas abiertas

### Supuestos aplicados (revisables)

| # | Supuesto |
|---|---|
| DA-01 | Estado `IN_REVIEW` opcional entre `DRAFT` y `PUBLISHED`, activable por tipo de artefacto (Compliance lo usa; Catálogos no). El dominio solo define los tres. |
| DA-02 | Ambientes `Development → Staging → Production`. Los nombres no están en el repositorio. |
| DA-03 | Cinco personas internas y ocho permisos, como propuesta para `access-service`. |
| DA-04 | Un solo borrador vivo por artefacto. |
| DA-05 | Piloto = capability en `PILOT` para un conjunto de tenants + versión asociada. No se agrega un aggregate nuevo. |
| DA-06 | Los eventos `ConnectorPublished`, `FlagChanged`, `ExtensionSchemaPublished` no existen en el repositorio; se nombran por analogía. |
| DA-07 | El impacto lo calcula `analytics-copilot-service` desde read models (`W7-PLT-01` lo lista como owner). |
| DA-08 | Autosave por cambio; confirmación tipeada solo cuando hay tenants afectados. |
| DA-09 | Tipos de nodo de workflow derivados de `stages`/`tasks`/`approvals`/`documentRequirements`/`SLAs`. |
| DA-10 | Desktop-first: editores de workflow, diff y matriz muestran "mejor en escritorio" bajo 1024 px. |

### Preguntas de producto sin responder

**No bloquean el diseño** (cada pantalla tiene un supuesto explícito y marcado), pero conviene resolverlas antes de implementar las pantallas que tocan.

| # | Dónde pega | Pregunta | Asumido |
|---|---|---|---|
| PQ-01 | `PA-072` `PA-132` | ¿Qué significa "dentro de límites permitidos" para los overrides de comisión? ¿Mín/máx por regla, roles de split obligatorios, o solo un flag de sobrescribible? | Mín/máx de porcentaje + roles obligatorios |
| PQ-02 | `PA-121…124` | ¿Un piloto es siempre de una capability, o también de una versión de artefacto sin capability asociada? | Capability en `PILOT` + versión. Si es lo segundo, hace falta un "pin de versión por tenant" |
| PQ-03 | `PA-125…127` | ¿Cuántos ambientes, cómo se llaman, y la promoción es por artefacto o por pack completo? | `Development → Staging → Production`, por artefacto con opción de lote |
| PQ-04 | `PA-142` `PA-143` | ¿Qué comandos de corrección expone cada servicio owner en la ola 7? | Cinco comandos ilustrativos marcados como tales. El patrón del flujo sí es definitivo |
| PQ-05 | `PA-042` | ¿Las aprobaciones del workflow se configuran acá con rol aprobador, o las define cada inmobiliaria? | Rol editable en la plantilla, reasignable por operación |
| PQ-06 | Trazabilidad | ¿Se confirma el significado inferido de los diez UCs sin descripción? | El título de cada task |

---

## 13. Archivos de este bundle

| Archivo | Contenido |
|---|---|
| `designs/00 - Mapa de Navegacion.dc.html` | **Empezá acá.** Las 72 pantallas con su entrada, los 10 journeys, la trazabilidad de 26 UCs y las preguntas abiertas |
| `designs/01 - Arquitectura, Modelo e Inventario.dc.html` | Fases 1–3: hallazgos del repo, sitemap, taxonomía, personas y permisos, modelo de 5 capas, inventario completo |
| `designs/02 - Design System.dc.html` | Fase 4: foundations densificadas, badges, los 9 patrones transversales con todos sus estados, inventario de componentes |
| `designs/03 - Pack Studio.dc.html` | Fases 5–6: shell completo, Overview, Packs, Capabilities, Catálogos. Journeys J1 y J6 |
| `designs/04 - Rule Studios.dc.html` | Fase 7: workflow, requisitos, compliance, comisiones, automatizaciones, métricas. Journeys J2–J5 |
| `designs/05 - Release y Gobierno.dc.html` | Fases 8–9: conectores, flags, schemas, impacto, pilotos, ambientes, inspector, auditoría, correcciones. Journeys J7–J10 |

**Cómo usarlos:** doble click y listo — cada uno es autocontenido y funciona offline. Tardan un segundo en aparecer la primera vez (están desempaquetando Brick). Los links entre archivos funcionan **si los seis quedan en la misma carpeta**.

Para valores exactos leé el código fuente: los estilos están inline y referencian tokens de Brick por `var(--*)`, con los tokens definidos en el mismo archivo.

## 14. Assets

No hay imágenes ni ilustraciones propias. Los íconos son los 120 SVG de Brick (`assets/icons/`, 24×24, `#2B2B2B`). Los glifos de severidad (`✕ ⚠ i ✓`) y de árbol (`├ └ ⋮⋮`) son caracteres Unicode, no íconos: mantenelos como texto para que los lea el screen reader. **Sin emoji** en ninguna superficie.

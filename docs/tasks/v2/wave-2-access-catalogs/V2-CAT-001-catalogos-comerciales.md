# V2-CAT-001 — Catálogos comerciales obligatorios y defaults inmobiliarios

- **Ola:** 2 — Acceso, Party y catálogos
- **Estado:** TODO
- **Dependencias:** V2-FND-002, V2-SCP-001
- **UCs:** CAT-001, CAT-002, CAT-003, CAT-004, CAT-005, CAT-006
- **Owner:** platform-config-service
- **Write zone:** platform-config-service, seed de catálogos y pantalla de configuración administrativa

## Resultado esperado

El Administrador puede consultar, crear, modificar, activar y desactivar
catálogos comerciales sin strings libres en los procesos. Las versiones
publicadas quedan disponibles para commands y la historia conserva la versión
que utilizó.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| CAT-001 | Administrador | Gestionar etapas ordenadas para pipelines DEMAND y SUPPLY. | platform-config-service | CRUD + validación de orden/semantic state. |
| CAT-002 | Administrador | Gestionar tipos de actividad. | platform-config-service | Catálogo usado por Activity. |
| CAT-003 | Administrador | Gestionar orígenes comerciales. | platform-config-service | Catálogo usado por Party y proceso. |
| CAT-004 | Administrador | Gestionar motivos de pérdida. | platform-config-service | Motivo obligatorio al cerrar LOST. |
| CAT-005 | Administrador | Gestionar tipos de operación y tipos de inmueble. | platform-config-service | Catálogos usados por Property/Listing/Requirement. |
| CAT-006 | Sistema | Aplicar defaults inmobiliarios y versionarlos. | platform-config-service | Seed reproducible y snapshot de versión. |

## Interfaces

- Query: GET /catalogs/{catalogType} con version y activeOnly.
- Commands: CreateCatalogEntry, UpdateCatalogEntry, DeactivateCatalogEntry y
  PublishCatalogVersion.
- Catalog types: CommercialStage, ActivityType, CommercialOrigin,
  LossReason, OperationType y PropertyType.
- CommercialStage fields: code, label, order, pipelineKind,
  semanticState OPEN/WON/LOST, active, version.
- Events v1: CatalogEntryCreated, CatalogEntryUpdated,
  CatalogEntryDeactivated y CatalogVersionPublished.

## Defaults obligatorios

- Demand stages: Consulta recibida, Necesidad relevada, Propiedades
  seleccionadas, Visita realizada, Negociación, Reserva, Operación concretada
  y Operación perdida.
- Supply stages: Contacto propietario, Tasación, Mandato, Publicación,
  Captación concretada y Captación perdida.
- Activity types: Llamada, Correo electrónico, Mensaje, Reunión presencial,
  Reunión virtual, Demostración, Envío de propuesta, Nota interna y Otro.
- Origins: Sitio web, Redes sociales, Publicidad, Recomendación, Evento,
  Prospección, Cliente existente y Portal inmobiliario.
- Loss reasons: Precio, Falta de presupuesto, Competidor, Inmueble
  inadecuado, Falta de respuesta, Decisión postergada, Inmueble no
  disponible, Financiación y Otro.

## Reglas

- Publicar una versión no muta registros históricos.
- No convertir CommercialStage en WorkflowTemplate ni agregar tareas, SLAs,
  aprobaciones, recordatorios o notifications.
- OPEN/WON/LOST son estados semánticos protegidos; el Administrador edita
  etiqueta, orden y activación, no su significado.
- Desactivar un código no rompe el historial; nuevos registros no lo pueden
  seleccionar.
- Portal inmobiliario como origen manual no implica integración con portales.

## Criterios de aceptación

- [ ] Los seis catálogos existen con seed reproducible.
- [ ] El Administrador puede publicar una nueva versión.
- [ ] Una oportunidad cerrada conserva stageCode y catalogVersion.
- [ ] No se puede cerrar LOST sin lossReasonCode.
- [ ] No se puede ubicar una fuente OPEN en una etapa WON o LOST.
- [ ] La UI no ofrece envío de email/WhatsApp ni configuraciones de workflow.

## Overrides POC

- El primer corte puede usar seed de etapas/productos; el CRUD administrativo
  completo queda para la entrega final.
- No hay catálogos por tenant, sucursal u organización.
- No se requiere un schema registry externo; los schemas son código + CI.

## Definition of Done

- [ ] Catálogos persistidos y versionados.
- [ ] Seed inmobiliario reproducible.
- [ ] Tests de lifecycle, compatibilidad y version histórica.
- [ ] Contratos y pantalla administrativa documentados.

## Evidencia requerida

1. Tabla de valores iniciales.
2. Ejemplo de dos versiones del mismo catálogo.
3. Test de cierre con motivo de pérdida.
4. Captura o request de configuración de cada catálogo.

## Handoff

**Completado** (ver `IMPLEMENTATION_REPORT-V2-CAT-001.md` para el detalle completo — contratos,
comandos corridos, decisiones locales):

- Los seis catálogos (`CatalogEntry`, aggregate único discriminado por `catalogType`) con CRUD +
  baja lógica + `PublishCatalogVersion`, persistidos en Mongo (`crm_platform_config`) con
  concurrencia optimista por entrada.
- Seed reproducible de los 52 valores literales (evidencia #1) + versión 1 publicada de cada
  `catalogType` (`Development`, `PlatformConfigServiceDevSeedHostedService`).
- Modelo de versionado: "versión por entrada" + contador `catalogVersion` por `catalogType`
  (decisión del equipo, sin snapshot inmutable — ver evidencia #2 en el reporte, sección "Ejemplos
  de dos versiones").
- Autorización real vía `IAuthorizationPort`/`HttpAuthorizationPort` (D2, reutilizado de
  V2-ACL-001, sin versión propia): solo Administrador gestiona, todos los autenticados leen.
- BFF: `GET /screens/{screenId}` (`ADM-05`..`ADM-12`) y `POST /mutations/{name}`
  (`createCatalogEntry`/`updateCatalogEntry`/`deactivateCatalogEntry`/`publishCatalogVersion`),
  agregados a los controllers ya existentes de V2-ACL-001 sin reestructurarlos.
- Adapter cliente de catálogos para consumidores (`ICatalogReaderPort`/`HttpCatalogReaderPort`,
  D7) en `BuildingBlocks.Infrastructure/Catalogs/`, listo para que V2-PTY-001 valide `originCode`.
- `bash scripts/test-fast.sh` y `bash scripts/test-integration.sh` en verde (evidencia #4 son los
  tests de `CatalogsController`/`CatalogService`, no una captura de UI: `apps/crm-web` sigue en
  modo demo, no conectado a este backend).

**Explícitamente fuera de alcance de esta task** (instrucción de la sesión que la implementó,
confirmada contra el propio texto de la task en "Explícitamente fuera"): la regla de proceso
"LOST exige `lossReasonCode`" y "OPEN no va a etapa WON/LOST" (criterios de aceptación 4 y 5,
evidencia requerida #3) — el catálogo expone `semanticState` y motivos como datos; la regla la
aplica **V2-PIPE-001**.

**Pendiente / para la siguiente task que toque este slice**:

- V2-PTY-001 (depende de esta task + V2-ACL-001 mergeadas): consumir `ICatalogReaderPort` para
  `originCode` contra `CommercialOrigin`.
- V2-PIPE-001: implementar la regla LOST/OPEN de arriba; evaluar si necesita un modelo de
  snapshot inmutable de catálogo (el `catalogVersion` de esta task es solo un contador, no
  reconstruye contenido histórico — decisión del equipo documentada en el reporte).
- No existe `ActivateCatalogEntry` (reactivar una entrada dada de baja): no lo pide la task.
- `scripts/run-slice.{sh,ps1}` (D10) sigue sin crearse.

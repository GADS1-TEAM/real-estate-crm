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

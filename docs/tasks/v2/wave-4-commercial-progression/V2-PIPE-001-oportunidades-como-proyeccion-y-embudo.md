# V2-PIPE-001 — Oportunidades como proyección, fachada y embudo

- **Ola:** 4 — Progresión comercial
- **Estado:** TODO
- **Dependencias:** V2-ACL-001, V2-CAT-001, V2-DMD-001, V2-MAT-001
- **UCs:** OPP-001, OPP-002, OPP-003, OPP-004, OPP-005, OPP-006, OPP-007, OPP-008, OPP-009
- **Owner:** analytics-service y operations-bff; los cambios de fuente pertenecen a demand-service/supply-service
- **Write zone:** CommercialPipelineItem read model, facade del BFF y contratos; no existe colección Opportunity

## Resultado esperado

El usuario puede crear, modificar, asignar, listar, consultar, filtrar y mover
una “Oportunidad” desde el BFF y el tablero, pero cada comando se ejecuta en
Requirement o CaptationCase. El tablero sobrevive a una recarga porque la
proyección se reconstruye desde eventos y estado de los owners.

## Alcance trazable

| UC | Actor | Comportamiento | Owner real | Evidencia |
|---|---|---|---|---|
| OPP-001 | Vendedor | Crear oportunidad de demanda que crea Requirement. | demand-service | Command + pipeline projection. |
| OPP-002 | Vendedor | Crear oportunidad de captación que crea CaptationCase. | supply-service | Command + pipeline projection. |
| OPP-003 | Vendedor/Responsable | Modificar datos comerciales a través de la fachada. | Owner de la fuente | Routing test. |
| OPP-004 | Admin/Responsable | Asignar o reasignar responsable. | access-service + owner | Actor/fecha persistidos. |
| OPP-005 | Vendedor/Responsable | Consultar listado con filtros y paginación inicial. | analytics-service | Query contract. |
| OPP-006 | Vendedor/Responsable | Consultar detalle y relaciones del proceso. | analytics-service | Detail contract. |
| OPP-007 | Vendedor/Responsable | Cambiar etapa desde detalle o tablero. | Owner de la fuente | Stage change event/history. |
| OPP-008 | Vendedor/Responsable | Cerrar como ganada o perdida con reglas de estado. | Owner de la fuente | Close tests. |
| OPP-009 | Responsable Comercial | Ver oportunidades agrupadas por etapa y pipeline kind. | analytics-service | Board e2e. |

## Modelo de lectura

~~~text
CommercialPipelineItem
- pipelineItemId
- sourceType: REQUIREMENT | CAPTATION_CASE
- sourceId
- pipelineKind: DEMAND | SUPPLY
- title
- relatedPartyIds[]
- propertyId?
- listingId?
- operationTypeCode
- responsibleUserId
- originCode?
- estimatedValue?
- estimatedCloseDate?
- currentStageCode
- semanticStatus: OPEN | WON | LOST
- stageHistoryRef
- matchIds[]
- visitIds[]
- negotiationIds[]
- reservationId?
- transactionId?
- sourceVersion
- projectionVersion
~~~

## Interfaces

- BFF queries: GET /opportunities y GET /opportunities/{pipelineItemId}.
- BFF commands: POST /opportunities, PATCH
  /opportunities/{pipelineItemId}, POST
  /opportunities/{pipelineItemId}/stage, POST
  /opportunities/{pipelineItemId}/win y POST
  /opportunities/{pipelineItemId}/loss.
- Create request:
  pipelineKind, title, relatedPartyIds, propertyId?, listingId?,
  operationTypeCode, responsibleUserId, originCode?, estimatedValue?,
  estimatedCloseDate?.
- Events consumidos: RequirementCreated/Updated, CaptationCaseOpened/Updated,
  ListingActivated/Closed, MatchSelected, VisitCompleted,
  NegotiationOpened, ReservationConfirmed, TransactionClosed y
  StageChanged, todos versionados.
- Comandos delegados: Create/Update/Advance/Satisfy/CloseLost de
  demand-service o supply-service según sourceType.

## Reglas

- CommercialPipelineItem es un read model; no acepta escrituras de usuario.
- sourceType es obligatorio y no puede cambiarse luego de crear el proceso.
- DEMAND usa Requirement como origen; SUPPLY usa CaptationCase como origen.
- La etapa se valida con CommercialStage y su semanticState.
- OPEN no puede ir a etapa WON/LOST; WON exige fecha de cierre y valor final
  si la operación usa dinero; LOST exige fecha y LossReason.
- Una oportunidad cerrada no vuelve a OPEN sin permiso explícito del backend.
- Cada stage change conserva etapa anterior, nueva, fecha/hora, userId y
  observación en un registro append-only.
- El tablero puede combinar ambos pipelineKind solo si muestra la diferencia.
- No se agrega una colección, aggregate o entidad fuente Opportunity.

## Criterios de aceptación

- [ ] POST /opportunities DEMAND crea Requirement y aparece en el board.
- [ ] POST /opportunities SUPPLY crea CaptationCase y aparece en el board.
- [ ] El listado y detalle muestran responsable, etapa, estado, origen,
  propiedad/listing y vínculos disponibles.
- [ ] El cambio de etapa desde board y detalle persiste después de recargar.
- [ ] El historial contiene etapa anterior, nueva, usuario, fecha y observación.
- [ ] Ganada y perdida ejecutan validaciones y no aceptan datos faltantes.
- [ ] PATCH nunca escribe directamente el read model.
- [ ] No existe una colección Opportunity en MongoDB.

## Overrides POC

- La primera entrega usa CommercialStage seed y Listings precargados.
- El read model puede reconstruirse al iniciar la aplicación en la POC.
- No se implementan exportación, agenda, tareas, notifications ni integraciones.

## Definition of Done

- [ ] Fachada BFF y read model contractualmente documentados.
- [ ] Routing por sourceType testeado.
- [ ] Board/list/detail y stage history testeados.
- [ ] Cierre OPEN/WON/LOST y motivo de pérdida testeados.

## Evidencia requerida

1. Flujo de primera entrega completo con reload.
2. Board DEMAND y SUPPLY diferenciados.
3. Request rechazado por etapa incompatible.
4. Registro append-only de dos cambios de etapa.

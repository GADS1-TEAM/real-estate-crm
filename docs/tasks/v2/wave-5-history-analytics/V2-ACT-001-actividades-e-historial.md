# V2-ACT-001 — Actividades manuales, timeline e historial comercial

- **Ola:** 5 — Historial, métricas y estadísticas
- **Estado:** TODO
- **Dependencias:** V2-PTY-001, V2-PIPE-001, V2-COM-001
- **UCs:** ACT-001, ACT-002, HIS-001, HIS-002, HIS-003
- **Owner:** activity-service y read model de historial
- **Write zone:** Activity en activity-service; timeline read model; no modifica stages ni aggregates de otros owners

## Resultado esperado

El usuario puede registrar una actividad que ya ocurrió y consultar una línea
de tiempo cronológica en el detalle de Empresa, Contacto y proceso comercial.
El historial de etapa se muestra junto con las actividades sin ser reemplazado
por la etapa actual.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| ACT-001 | Vendedor | Registrar llamada, correo, mensaje, reunión, demo, propuesta, nota u otro hecho ocurrido. | activity-service | Command + validation test. |
| ACT-002 | Vendedor | Relacionar actividad con Empresa/Contacto y opcionalmente pipelineItem. | activity-service | Relation contract. |
| HIS-001 | Vendedor/Responsable | Consultar timeline de una Empresa. | history read model | Ordered query. |
| HIS-002 | Vendedor/Responsable | Consultar timeline de un Contacto. | history read model | Ordered query. |
| HIS-003 | Vendedor/Responsable | Consultar actividades y stage changes del proceso comercial. | history read model | Combined timeline. |

## Modelo mínimo

~~~text
Activity
- activityId
- activityTypeCode
- occurredAt
- recordedByUserId
- primaryPartyId
- relatedCompanyId?
- relatedContactId?
- pipelineItemId?
- propertyId?
- listingId?
- description
- result
- createdAt
- version
~~~

Los tipos se obtienen de ActivityType en V2-CAT-001. Email, mensaje y
WhatsApp son tipos de registro manual; no contienen proveedor, credencial,
thread externo ni acción de envío.

## Interfaces

- Command: RecordActivity.
- Queries: GetPartyTimeline, GetCompanyTimeline, GetContactTimeline y
  GetCommercialTimeline.
- Evento v1: ActivityRecorded.
- Consume: PartyReference, PipelineItemReference,
  CommercialStageChanged, VisitCompleted, ProposalSubmitted,
  ReservationConfirmed y TransactionClosed.
- Produce: ActivityReference y HistoryEntry para analytics.

## Reglas

- occurredAt y recordedByUserId son obligatorios.
- Toda actividad se relaciona al menos con una Party o con un
  pipelineItemId válido; la consigna no permite actividad huérfana.
- Registrar una actividad no crea tarea, follow-up, agenda, reminder ni
  notification.
- Las actividades no se eliminan físicamente; una corrección queda auditada.
- El timeline se ordena por occurredAt y usa createdAt como desempate estable.
- Stage changes siguen siendo registros independientes con etapa anterior,
  nueva, fecha, usuario y observación.
- activity-service no modifica Requirement, CaptationCase, Listing ni
  Transaction.

## Criterios de aceptación

- [ ] Una actividad manual aparece en Empresa, Contacto y proceso cuando tiene
  esas relaciones.
- [ ] El formulario impide guardar sin tipo, fecha, usuario, relación y
  descripción mínima.
- [ ] Email/WhatsApp solo se registran y no muestran acciones de envío.
- [ ] El timeline mezcla actividades y cambios de etapa en orden cronológico.
- [ ] Un cambio de etapa anterior sigue visible después de otro cambio.
- [ ] Un usuario sin permiso no puede consultar el historial del proceso.

## Overrides POC

- No se ingestan conversaciones ni archivos externos.
- Las notas son texto; no grabación, transcripción ni media pipeline.
- Se permite un read model reconstruible desde eventos.

## Definition of Done

- [ ] Activity persistida y validada.
- [ ] Timelines de Empresa, Contacto y pipeline.
- [ ] Historial de etapas append-only visible.
- [ ] Tests de orden, relación, permisos y baja lógica.

## Evidencia requerida

1. Registro de una actividad y consulta en los tres detalles.
2. Timeline con dos actividades y dos cambios de etapa.
3. Request rechazado por actividad huérfana.
4. Test que demuestra que no hay envío externo.

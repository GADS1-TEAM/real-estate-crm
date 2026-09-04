# V2-COM-001 — Visita ocurrida, negociación, reserva y operación cerrada

- **Ola:** 4 — Progresión comercial
- **Estado:** TODO
- **Dependencias:** V2-PIPE-001, V2-PRP-001
- **UCs:** VIS-001, NEG-001, NEG-002, RES-001, TRX-001, TRX-002, TRX-003
- **Owner:** commercial-service
- **Write zone:** Visit, Negotiation, Proposal, Reservation y Transaction en commercial-service

## Resultado esperado

El CRM representa los hitos propios de una operación inmobiliaria sin
confundirlos con agenda, tareas o pagos: una visita ya realizada, una
negociación con propuestas inmutables, una reserva y una Transaction que puede
cerrar la operación con fecha y valor final.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| VIS-001 | Vendedor | Registrar una visita que ya ocurrió sobre Property/Listing y Party. | commercial-service | Visit persisted. |
| NEG-001 | Vendedor | Abrir Negotiation para Requirement + Listing. | commercial-service | Negotiation command. |
| NEG-002 | Vendedor/Responsable | Registrar propuesta y contrapropuesta sin editar las anteriores. | commercial-service | Immutable proposal test. |
| RES-001 | Vendedor/Responsable | Registrar una Reservation confirmada sin gestionar pagos. | commercial-service | Reservation lifecycle. |
| TRX-001 | Responsable | Crear Transaction desde reserva o cierre directo. | commercial-service | Transaction command. |
| TRX-002 | Responsable | Marcar operación cerrada o cancelada con resultado. | commercial-service | State validation. |
| TRX-003 | Sistema | Publicar hitos para actualizar pipeline y métricas. | commercial-service | Event contract + projection. |

## Modelo mínimo

~~~text
Visit
- visitId
- propertyId
- listingId
- requirementId?
- visitorPartyIds[]
- responsibleUserId
- occurredAt
- outcome
- notes?

Negotiation
- negotiationId
- requirementId?
- listingId
- participantPartyIds[]
- status: OPEN | ACCEPTED | REJECTED | WITHDRAWN | CLOSED
- proposals[]

Proposal
- proposalId
- sequence
- proposedByPartyId
- termsSnapshot
- createdAt
- response?

Reservation
- reservationId
- listingId
- negotiationId?
- acceptedProposalId?
- participantPartyIds[]
- reservedValue?
- currency?
- validFrom
- expiresAt?
- status: DRAFT | CONFIRMED | EXPIRED | CANCELLED | FULFILLED

Transaction
- transactionId
- operationTypeCode
- propertyId
- listingId?
- reservationId?
- participantRoles[]
- agreedTerms
- closedAt?
- finalValue?
- currency?
- outcome: OPEN | CLOSED | CANCELLED
~~~

## Interfaces

- Commands: RecordCompletedVisit, OpenNegotiation, SubmitProposal,
  CounterProposal, AcceptProposal, CreateReservation, ConfirmReservation,
  OpenTransaction, CloseTransaction y CancelTransaction.
- Queries: GetVisit, GetNegotiation, GetReservation, GetTransaction y
  GetCommercialDossier.
- Events v1: VisitCompleted, NegotiationOpened, ProposalSubmitted,
  ProposalAccepted, ReservationConfirmed, TransactionOpened,
  TransactionClosed y TransactionCancelled.
- Consume: RequirementReference, ListingReference, PartyReference y
  CommercialStage.
- Produce: referencias de hitos para CommercialPipelineItem y analytics.

## Reglas

- Visit requiere occurredAt; no tiene ScheduleItem, disponibilidad,
  reprogramación, reminder ni estado futuro.
- Proposal emitida es inmutable; una contrapropuesta crea otra secuencia.
- Reservation no equivale a pago, seña imputada, deuda ni comprobante.
- Transaction cerrada registra fecha y valor final cuando la operación utiliza
  valores monetarios.
- Una Transaction cancelada conserva sus relaciones e historia.
- Solo las operaciones válidas pueden emitir TransactionClosed.
- No se instancia WorkflowTemplate, Task, approval, Notification ni
  ManagedAgreement.
- El cierre actualiza el pipeline por evento; commercial-service no escribe la
  colección de analytics.

## Criterios de aceptación

- [ ] Se registra una visita pasada y aparece relacionada al proceso.
- [ ] Se crea una negociación con una propuesta y una contrapropuesta.
- [ ] No se puede editar una propuesta ya emitida.
- [ ] Una reserva confirmada no crea pagos ni deuda.
- [ ] Una Transaction cerrada exige fecha y finalValue si corresponde.
- [ ] La cancelación no elimina la negociación ni la reserva.
- [ ] Los eventos actualizan el detalle del pipeline sin acceso cruzado a Mongo.

## Overrides POC

- Notas de visita son texto simple; no audio, grabación ni transcripción.
- Se permite un adapter in-memory para pruebas de eventos.
- No se implementan pagos, alquiler administrado, documentos ni firma.

## Definition of Done

- [ ] Visit, Negotiation, Proposal, Reservation y Transaction persistidos.
- [ ] Invariantes de inmutabilidad y cierre testeadas.
- [ ] Eventos contractuales y proyección integrados.
- [ ] UI de hechos ocurridos sin agenda futura.

## Evidencia requerida

1. Timeline de visita → propuesta → reserva → cierre.
2. Test de propuesta inmutable.
3. Test de reserva sin Payment.
4. Evento TransactionClosed recibido por la proyección.

# V2-DMD-001 — Requirement, CaptationCase, Valuation y Mandate mínimo

- **Ola:** 3 — Núcleo inmobiliario
- **Estado:** TODO
- **Dependencias:** V2-PTY-001, V2-PRP-001
- **UCs:** DMD-001, DMD-002, DMD-003, DMD-004, CAP-001, CAP-002, CAP-003
- **Owner:** demand-service y supply-service
- **Write zone:** Requirement en demand-service; CaptationCase, Valuation y CommercialMandate mínimo en supply-service

## Resultado esperado

El sistema distingue la demanda de un interesado de la captación de un
inmueble. Una Party puede tener varias necesidades activas; una captación puede
avanzar desde contacto con propietario hasta una oferta comercial sin
convertirse artificialmente en una entidad Opportunity.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| DMD-001 | Vendedor | Crear Requirement para compra o alquiler de una Party. | demand-service | Requirement persistido. |
| DMD-002 | Vendedor | Modificar criterios y conservar operación, presupuesto y ubicación. | demand-service | Update test. |
| DMD-003 | Vendedor | Mantener varios Requirements independientes para una Party. | demand-service | Query con dos necesidades. |
| DMD-004 | Vendedor | Consultar detalle de la necesidad y sus listings seleccionados. | demand-service | Query contract. |
| CAP-001 | Vendedor | Abrir CaptationCase con propietario y Property candidata. | supply-service | CaptationCase persistido. |
| CAP-002 | Vendedor | Registrar Valuation histórica y expectativa del propietario. | supply-service | Nueva versión sin sobrescritura. |
| CAP-003 | Vendedor/Responsable | Registrar Mandate mínimo y convertir captación en Listing habilitable. | supply-service | Lifecycle test. |

## Modelo mínimo

~~~text
Requirement
- requirementId
- seekerPartyIds
- operationTypeCode
- propertyTypeCode?
- locationCriteria?
- financialCriteria?
- selectedListingIds[]
- responsibleUserId?
- originCode?
- commercialProgress
- status: DRAFT | ACTIVE | SATISFIED | CANCELLED | EXPIRED
- version

CaptationCase
- captationCaseId
- contactPartyIds
- candidatePropertyId?
- responsibleUserId?
- originCode?
- status: IDENTIFIED | CONTACTING | VALUATION_PENDING | MANDATE_NEGOTIATION | CAPTURED | LOST
- valuationRefs[]
- mandateRef?
- commercialProgress
- version

Valuation
- valuationId
- propertyId
- performedBy
- valuationDate
- value
- currency
- ownerExpectedValue?
- confidence?
- version

CommercialMandate
- mandateId
- propertyId
- grantingPartyIds
- authorizedOperationTypeCodes[]
- validFrom
- validUntil?
- status: DRAFT | ACTIVE | EXPIRED | REVOKED
~~~

## Interfaces

- Commands: CreateRequirement, UpdateRequirement, ActivateRequirement,
  OpenCaptationCase, UpdateCaptationCase, IssueValuation,
  GrantCommercialMandate.
- Queries: GetRequirementDetail, SearchRequirements,
  GetCaptationDetail y SearchCaptations.
- Events v1: RequirementCreated, RequirementUpdated,
  RequirementActivated, CaptationCaseOpened, ValuationIssued,
  CommercialMandateActivated, CaptationCaptured y CaptationLost.
- Consume: PartyReference, PropertyReference, OperationType, PropertyType y
  CommercialOrigin.
- Produce: RequirementReference, CaptationReference y progress events para
  V2-MAT-001 y V2-PIPE-001.

## Reglas

- Inquiry automática no es necesaria: la carga manual crea directamente
  Requirement o CaptationCase según la intención.
- Una Party puede tener N Requirements activos sin duplicarse.
- No se copian perfiles completos de Party o Property dentro del aggregate.
- Valuation emitida es histórica; una corrección genera otra versión.
- La captación no equivale a Listing hasta que se cumplen las condiciones
  mínimas de la política local.
- La operación puede ser alquiler comercial, pero esta task no crea
  ManagedAgreement, cronograma, deuda ni Payment.

## Criterios de aceptación

- [ ] Una Party crea dos Requirements independientes.
- [ ] Requirement acepta datos mínimos y muestra criterios opcionales sin
  bloquear la captura.
- [ ] CaptationCase vincula propietario y Property candidata.
- [ ] Dos Valuations conservan su historia y no se sobrescriben.
- [ ] Un Mandate ACTIVE habilita la transición definida para Listing.
- [ ] Los comandos rechazan Party o Property inexistentes y registran actor.
- [ ] No se crea una colección Opportunity.

## Overrides POC

- Los valores monetarios se almacenan como decimal/money value object, no
  float.
- La demo puede usar un usuario, propietario y Property seed.
- No se implementan documentos de mandato, compliance, firma ni proveedor
  externo.

## Definition of Done

- [ ] Requirement y CaptationCase persistidos en sus owners.
- [ ] Valuation/Mandate mínimo y versionado.
- [ ] Tests de múltiples necesidades, historia y referencias.
- [ ] Contratos y eventos actualizados.

## Evidencia requerida

1. Dos Requirements de una misma Party.
2. CaptationCase → Valuation → Mandate.
3. Test de versionado de Valuation.
4. Búsqueda sin Opportunity como fuente.

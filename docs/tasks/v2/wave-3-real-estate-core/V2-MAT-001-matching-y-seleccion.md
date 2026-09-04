# V2-MAT-001 — Matching explicable y selección de Listings

- **Ola:** 3 — Núcleo inmobiliario
- **Estado:** TODO
- **Dependencias:** V2-DMD-001, V2-PRP-001
- **UCs:** MAT-001, MAT-002, MAT-003, MAT-004
- **Owner:** matching-service
- **Write zone:** matching-service y selección de listings dentro de Requirement; no modifica Property ni Listing

## Resultado esperado

El vendedor puede consultar qué Listings son compatibles con un Requirement,
ver por qué cada candidato encaja y seleccionar o descartar propiedades sin
modificar los datos fuente.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| MAT-001 | Sistema | Generar candidatos por operación, tipo y ubicación. | matching-service | MatchCase query. |
| MAT-002 | Sistema | Calcular score determinístico con criterios duros y blandos. | matching-service | Unit tests de scoring. |
| MAT-003 | Vendedor | Ver explicación y datos que provocan el score. | matching-service | Response contract. |
| MAT-004 | Vendedor | Seleccionar o descartar Listing para Requirement. | demand-service vía command | State/event test. |

## Modelo mínimo

~~~text
MatchCase
- matchId
- requirementId
- listingId
- eligibility: ELIGIBLE | SOFT_MATCH | INELIGIBLE
- score
- hardCriteriaResults[]
- softCriteriaResults[]
- explanation[]
- calculatedAt
- scoringPolicyVersion
- status: PRESENTED | SELECTED | DISCARDED | STALE
- feedbackReasonCode?
~~~

## Interfaces

- Query: GetMatchesForRequirement y GetMatchDetail.
- Commands: SelectListingForRequirement y DiscardMatch.
- Events v1: MatchCalculated, MatchPresented, MatchSelected,
  MatchDiscarded y MatchInvalidated.
- Consume: RequirementActivated/Updated y ListingActivated/Updated.
- Produce: selectedListingIds update command para demand-service.

## Reglas

- Un criterio REQUIRED incompatible no puede ser compensado por preferencias.
- El score explica entradas y versión; no es una etiqueta subjetiva.
- Matching no altera Property, Listing ni Party.
- No se usa ML ni se llama a un motor externo.
- Un cambio de Requirement o Listing marca el match stale y permite recalcular.

## Criterios de aceptación

- [ ] Un Listing que cumple criterios obligatorios queda elegible.
- [ ] Uno que falla un criterio obligatorio queda inelegible aunque tenga
  preferencias coincidentes.
- [ ] La respuesta incluye explicación legible.
- [ ] Seleccionar un match actualiza solo el Requirement y deja evento.
- [ ] Actualizar una fuente invalida el resultado anterior.

## Overrides POC

- Scoring determinístico in-memory o MongoDB; no se agrega OpenSearch.
- Candidatos limitados a índices MongoDB de la POC.

## Definition of Done

- [ ] Reglas de scoring unit testeadas.
- [ ] Query/commands y eventos contractualmente validados.
- [ ] Selección y descarte no invaden owners.
- [ ] No se agregó ML ni integración externa.

## Evidencia requerida

1. Comparación de dos Listings con explicación.
2. Test de criterio requerido.
3. Flujo de selección persistida en Requirement.
4. Evento de invalidación después de modificar una fuente.

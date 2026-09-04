# V2-ANA-001 — Métricas, estadísticas y dashboard con lineage

- **Ola:** 5 — Historial, métricas y estadísticas
- **Estado:** TODO
- **Dependencias:** V2-PIPE-001, V2-COM-001, V2-ACT-001
- **UCs:** ANA-001, ANA-002, ANA-003, ANA-004, ANA-005
- **Owner:** analytics-service
- **Write zone:** MetricDefinition, MetricObservation, read models de dashboard y queries analíticas

## Resultado esperado

El Responsable Comercial puede consultar métricas y estadísticas útiles del
CRM inmobiliario, con fórmulas versionadas, período, dimensiones y referencias
de lineage. El dashboard no depende de agenda, tareas, SLA, notificaciones,
alquileres o pagos.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| ANA-001 | Responsable | Ver volumen de procesos abiertos por etapa, estado, responsable y origen. | analytics-service | Dashboard/query. |
| ANA-002 | Responsable | Ver conversiones y tiempos entre etapas/hitos del pipeline. | analytics-service | Metric observations. |
| ANA-003 | Responsable | Ver actividad registrada por usuario, tipo y período. | analytics-service | Aggregation test. |
| ANA-004 | Responsable | Ver performance de Property/Listing: activos, visitas, negociaciones y cierres. | analytics-service | Property metrics. |
| ANA-005 | Responsable | Consultar fórmula, versión y fuentes de una métrica. | analytics-service | Lineage response. |

## Métricas V2

- procesos abiertos por CommercialStage y pipelineKind;
- oportunidades DEMAND/SUPPLY ganadas y perdidas;
- conversión Requirement → Match → Visit → Negotiation → Reservation →
  Transaction;
- tiempo medio desde alta hasta cierre cuando existan fechas suficientes;
- actividades por tipo, usuario y período;
- Listings activos, pausados, cerrados y visitas por Listing;
- valor estimado versus valor final cuando ambos existan;
- motivos de pérdida por cantidad y período;
- Requirements activos y demanda sin Listing seleccionado.

No se implementan métricas de alquiler, cobranza, tareas, agenda, SLA,
notificaciones ni sucursales.

## Modelo mínimo

~~~text
MetricDefinition
- metricCode
- semanticDefinition
- formulaVersion
- sourceEvents[]
- dimensions[]
- effectiveFrom

MetricObservation
- metricCode
- period
- dimensions
- value
- availability: KNOWN | UNKNOWN
- sourceProjectionVersion
- lineageRefs[]
- calculatedAt
~~~

## Interfaces

- Queries: GetPipelineDashboard, GetActivityStatistics,
  GetPropertyPerformance y ExplainMetric.
- Events consumidos: todos los eventos de Party, Listing, Requirement,
  CaptationCase, Match, Visit, Negotiation, Reservation, Transaction, Activity
  y StageChanged que estén definidos en V2.
- Events no se convierten en fuente mutable; analytics mantiene proyecciones.
- No se expone exportación de información.

## Reglas

- Toda métrica debe tener definición, fórmula/version y lineage.
- UNKNOWN significa que faltan datos o no aplica; no se convierte en cero.
- Cero significa que la consulta fue válida y no hubo ocurrencias.
- Los filtros respetan permisos del Responsable Comercial y del Vendedor.
- Las proyecciones pueden reconstruirse y no se escriben desde la UI.
- Un dashboard no crea ni modifica registros de negocio.

## Criterios de aceptación

- [ ] El dashboard muestra procesos por etapa con filtros de período,
  responsable, pipelineKind y origen.
- [ ] Se calcula al menos una conversión entre cada hito disponible del
  pipeline.
- [ ] Actividades y listings pueden analizarse por período.
- [ ] ExplainMetric devuelve fórmula, versión, eventos fuente y lineage.
- [ ] Una métrica sin datos devuelve UNKNOWN y no cero.
- [ ] No aparecen cards de alquiler, pagos, agenda, tareas o notificaciones.

## Overrides POC

- MongoDB es el read store; no se agrega warehouse ni base columnar.
- Proyecciones se pueden recalcular desde eventos/fixtures en tests.
- No se implementa Management Copilot conversacional en esta task; la IA se
  define en V2-AI-001.

## Definition of Done

- [ ] Definitions y observations versionadas.
- [ ] Dashboard y queries de métricas.
- [ ] Tests de cálculos, permisos, UNKNOWN/cero y reconstrucción.
- [ ] Lineage visible y documentado.

## Evidencia requerida

1. Captura del dashboard con filtros.
2. Response de ExplainMetric.
3. Test UNKNOWN versus cero.
4. Test de reconstrucción de una proyección.

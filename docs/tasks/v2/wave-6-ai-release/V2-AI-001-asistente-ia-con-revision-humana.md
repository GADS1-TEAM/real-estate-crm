# V2-AI-001 — Asistente IA revisable sobre el historial del CRM

- **Ola:** 6 — IA y entrega
- **Estado:** TODO
- **Dependencias:** V2-ANA-001, V2-ACT-001, V2-UX-001
- **UCs:** AI-001, AI-002, AI-003, AI-004
- **Owner:** automation-ai-service reducido a IA
- **Write zone:** AgentDecision, adapter de modelo, endpoint/UI de asistencia; no modifica aggregates de negocio automáticamente

## Resultado esperado

El usuario puede pedir un análisis de una oportunidad visible y recibir un
resumen estructurado, una prioridad sugerida y datos faltantes detectados a
partir de información ya registrada. Puede revisar, aceptar o descartar la
salida; la IA no cambia etapas, no crea tareas, no envía mensajes y no
consulta MongoDB directamente.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| AI-001 | Vendedor/Responsable | Solicitar resumen de una oportunidad y su historial. | automation-ai-service | Query + provider port. |
| AI-002 | IA | Sugerir prioridad y datos faltantes con evidencia. | automation-ai-service | Structured output test. |
| AI-003 | Usuario | Revisar, aceptar, editar o descartar la sugerencia. | automation-ai-service | Review state. |
| AI-004 | Sistema | Auditar modelo, confianza, evidencia y resultado de revisión. | automation-ai-service | AgentDecision. |

## Modelo mínimo

~~~text
AgentDecision
- decisionId
- pipelineItemId
- requestedByUserId
- agentType: COMMERCIAL_ASSISTANT
- modelRef
- inputEvidenceRefs[]
- summary
- suggestedPriority: LOW | MEDIUM | HIGH
- missingInformation[]
- confidence
- policyVersion
- reviewStatus: PROPOSED | ACCEPTED | EDITED | REJECTED
- reviewedByUserId?
- reviewedAt?
- createdAt
~~~

## Interfaces

- Query: GetAuthorizedPipelineContext.
- Command: RequestCommercialInsight y ReviewAgentDecision.
- Port: IModelGateway con input estructurado y output JSON validable.
- Event v1: AIDecisionProposed, AIDecisionReviewed.
- Consume: CommercialPipelineItem, Activity, StageHistory,
  MatchCase, Visit, Negotiation, Reservation, Transaction y metric summaries
  mediante APIs/read models autorizados.
- Produce: AgentDecision y pantalla de revisión.

## Reglas

- El LLM nunca recibe una conexión o query raw a MongoDB.
- El contexto se limita al pipelineItem y scope del usuario que solicita.
- La respuesta debe referenciar evidencia; si faltan datos, declara
  missingInformation y no inventa valores.
- Aceptar la sugerencia solo cambia reviewStatus; no cambia etapa, estado,
  responsable, Party, Listing ni Transaction.
- No se generan Task, Notification, email, WhatsApp, agenda ni follow-up.
- Una falla o timeout del proveedor se muestra como error recuperable y no
  cambia el estado del CRM.
- No se persiste chain-of-thought; solo salida operativa, confianza y
  referencias de evidencia.

## Criterios de aceptación

- [ ] La IA resume un pipeline con actividades y stage history reales.
- [ ] La respuesta identifica al menos una evidencia por conclusión.
- [ ] Un usuario puede rechazar y luego consultar la decisión rechazada.
- [ ] Aceptar o editar no modifica el aggregate fuente.
- [ ] Un usuario sin permiso no puede solicitar contexto de otro registro.
- [ ] La prueba de arquitectura demuestra que automation-ai-service no accede
  a MongoDB de otro service.
- [ ] No hay envío ni creación de tareas/notificaciones como efecto lateral.

## Overrides POC

- El proveedor se configura mediante adapter y variables locales no
  versionadas.
- Unit tests usan un fake IModelGateway determinístico.
- La funcionalidad puede mostrar estado no disponible si no hay credencial; el
  fake solo se usa en tests y no como fallback silencioso productivo.

## Definition of Done

- [ ] Feature IA integrada a un flujo existente.
- [ ] Salida estructurada, evidencia y revisión humana persistidas.
- [ ] Tests de permisos, error, no mutación y auditabilidad.
- [ ] UI documentada en V2-UX-001.

## Evidencia requerida

1. Solicitud y respuesta IA sobre una oportunidad real de fixture.
2. Revisión aceptada, editada y rechazada.
3. Test que prueba que el estado de negocio no cambia.
4. Evidencia de contexto autorizado sin acceso directo a Mongo.

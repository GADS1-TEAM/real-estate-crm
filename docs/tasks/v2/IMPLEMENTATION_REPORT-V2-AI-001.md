# V2-AI-001 - Asistente IA con revisión humana (HITL)

## Resumen
Se implementó el servicio `automation-ai-service` para la gestión de decisiones de IA mediante un esquema "Human In The Loop" (HITL).

## Cambios realizados
- `RealEstateCrm.Contracts`: Se agregó `AI/AiContracts.cs` con los registros `AgentDecisionV1`, `AnalyzeOpportunityRequestV1` y `ReviewDecisionRequestV1`.
- `AutomationAiService`:
  - **Dominio**: Se implementó el agregado `AgentDecision` y el puerto `IModelGateway`.
  - **Infraestructura**: Se proveyó `FakeModelGateway` como implementación determinista para tests/desarrollo y `AgentDecisionRepository` empleando el driver de MongoDB (`crm_automation_ai`).
  - **API**: Se creó `AiAssistantController` con los endpoints `/api/v1/ai/opportunities/{pipelineItemId}/analyze` y `/api/v1/ai/decisions/{decisionId}/review`.

## Validaciones
- El servicio respeta las restricciones arquitectónicas de V2 y no accede a bases de datos ni drivers de otros microservicios.
- 0 errores o advertencias en la compilación.

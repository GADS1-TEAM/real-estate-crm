# W4-CLS-05 — Omitir/solicitar/aprobar excepciones y avanzar workflow

**Dependencias:** `W4-CLS-04`, `W1-ORG-02`  
**Casos de uso:** `CLS-017`, `CLS-018`, `CLS-019`, `CLS-020`

- `CLS-017`: manager con permiso omite Task con razón/audit; `WorkflowExceptionApproved`.
- `CLS-018`: agente solicita omisión; `WorkflowExceptionRequested`.
- `CLS-019`: manager/director aprueba o rechaza según scope/policy; `WorkflowExceptionApproved/Rejected`.
- `CLS-020`: avanzar etapa semántica solo con invariantes aplicables satisfechas; `TransactionStageChanged`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Reglas
Excepción nunca elimina evidencia del requisito. Etapas visuales configurables no cambian semántica core.

## DoD
- [ ] Permisos + approvals testeados.
- [ ] Advance bloquea únicamente invariantes vigentes.

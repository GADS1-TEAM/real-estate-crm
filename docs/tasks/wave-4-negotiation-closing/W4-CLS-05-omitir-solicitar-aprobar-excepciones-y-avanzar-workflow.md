# W4-CLS-05 — Omitir/solicitar/aprobar excepciones y avanzar workflow

**Dependencias:** `W4-CLS-04`, `W1-ORG-02`  
**Casos de uso:** `CLS-017`, `CLS-018`, `CLS-019`, `CLS-020`

- `CLS-017`: manager con permiso omite Task con razón/audit; `WorkflowExceptionApproved`.
- `CLS-018`: agente solicita omisión; `WorkflowExceptionRequested`.
- `CLS-019`: manager/director aprueba o rechaza según scope/policy; `WorkflowExceptionApproved/Rejected`.
- `CLS-020`: avanzar etapa semántica solo con invariantes aplicables satisfechas; `TransactionStageChanged`.

## Reglas
Excepción nunca elimina evidencia del requisito. Etapas visuales configurables no cambian semántica core.

## DoD
- [ ] Permisos + approvals testeados.
- [ ] Advance bloquea únicamente invariantes vigentes.

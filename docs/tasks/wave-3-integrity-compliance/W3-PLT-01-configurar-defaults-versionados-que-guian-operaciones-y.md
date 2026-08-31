# W3-PLT-01 — Configurar defaults versionados que guían operaciones y compliance

**Dependencias:** `W1-PLT-01`, `W3-DOC-02`  
**Casos de uso:** `PLT-009`, `PLT-010`, `PLT-011`

## Casos de uso

- `PLT-009` Configurar `WorkflowTemplate` base por operación/etapa con tareas, dependencias y reglas de excepción; evento `WorkflowTemplatePublished`.
- `PLT-010` Definir `DocumentRequirement` por operación, Party role, etapa, jurisdicción y versión de pack; evento `DocumentRequirementPublished`.
- `PLT-011` Versionar reglas que abren/satisfacen `ComplianceCase` sin editar casos históricos; evento `CompliancePolicyPublished`.

## Reglas

Defaults se versionan y las instancias conservan la versión aplicada. Platform Admin configura artefactos; no edita casos operativos directamente.

## DoD
- [ ] Versionado y aplicación contextual testeados.
- [ ] Históricos no cambian al publicar nueva versión.
- [ ] Permisos backoffice/auditoría.

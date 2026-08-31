# W3-PLT-02 — Definir políticas y automatizaciones starter del pack

**Dependencias:** `W1-PLT-01`  
**Casos de uso:** `PLT-012`, `PLT-013`

## Casos de uso

- `PLT-012` Crear/versionar `CommissionPolicy` default que tenants pueden sobrescribir dentro de límites; evento `CommissionPolicyPublished`.
- `PLT-013` Crear `AutomationPolicy` starter para respuesta, SLA, recordatorios y routing con autonomía predeterminada; evento `AutomationPolicyPublished`.

## Reglas

Starter policies deben funcionar sin setup, ser editables y conservar histórico/versionado.

## DoD
- [ ] Publicación/versionado testeados.
- [ ] Tenant override no muta default global.
- [ ] Auditoría de cambios.

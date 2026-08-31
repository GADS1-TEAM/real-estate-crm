# W3-ORG-01 — Excepciones de permisos y configuración por tenant/sucursal

**Dependencias:** `W1-ORG-02`, `W1-PLT-01`  
**Casos de uso:** `ORG-005`, `ORG-006`, `ORG-010`, `ORG-011`

## Objetivo

Soportar permisos excepcionales y configuración organizacional sin romper el modelo de rol + scope.

## Reglas

- overrides de usuario explícitos, auditados, con vigencia cuando corresponda;
- reglas/configuración pueden variar por unidad organizacional dentro de límites del pack;
- resolución de permisos debe explicar role/scope/override efectivo;
- ninguna excepción se implementa duplicando usuarios o branches.

## DoD
- [ ] UCs del archivo fuente/Task Board cubiertos.
- [ ] Overrides grant/revoke/expiry testeados.
- [ ] Configuración por unidad no filtra datos fuera de scope.

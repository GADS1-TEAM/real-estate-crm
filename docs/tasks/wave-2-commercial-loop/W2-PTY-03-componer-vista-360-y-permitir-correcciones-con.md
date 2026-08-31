# W2-PTY-03 — Componer vista 360 y permitir correcciones con historial

**Dependencias:** `W2-PTY-02`  
**Casos de uso:** `PTY-010`, `PTY-015`

## Casos de uso

- `PTY-010` Consultar `Party360`: identidad, roles, actividad, Requirements, captaciones, operaciones, alquileres y próximos pasos. Owner del read model: analytics-copilot-service.
- `PTY-015` Corregir dato de Party con provenance/historial; `party-service`; evento `PartyIdentityAttributesChanged` cuando afecta identidad.

## Reglas

`Party360` es proyección reconstruible, nunca fuente de verdad. Correcciones se realizan contra el servicio owner, no contra la proyección.

## DoD
- [ ] Vista compuesta autorizada por scope.
- [ ] Corrección auditable.
- [ ] Proyección tolera consistencia eventual explícita.

# W3-VIS-03 — Crear, confirmar, iniciar, completar y registrar no-show

**Dependencias:** `W3-VIS-01`, `W2-MAT-02`  
**Casos de uso:** `VIS-007`, `VIS-008`, `VIS-009`, `VIS-012`, `VIS-013`

## Casos de uso

- `VIS-007` Crear `Visit` asociada a Listing/Property, participantes y ScheduleItem; `VisitScheduled`.
- `VIS-008` Confirmar asistencia esperada; `VisitConfirmed`.
- `VIS-009` Marcar inicio real y habilitar notas/media contextuales; `VisitStarted`.
- `VIS-012` Completar con resultado mínimo; `VisitCompleted`.
- `VIS-013` Registrar no-show para métricas/seguimiento; `VisitNoShow`.

## Reglas

Visit soporta múltiples participantes/agentes aunque sea excepcional. Completar no exige feedback exhaustivo; enrichment posterior permitido.

## DoD
- [ ] Lifecycle y transiciones inválidas testeadas.
- [ ] Participantes múltiples soportados.
- [ ] Resultado/no-show alimentan read models.

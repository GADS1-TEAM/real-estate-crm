# W3-SUP-03 — Cerrar captación ganada/perdida y dormir/reactivar casos

**Dependencias:** `W3-SUP-02`  
**Casos de uso:** `SUP-013`, `SUP-014`, `SUP-015`

## Casos de uso

- `SUP-013` Marcar captación `CAPTURED` cuando existe autorización suficiente y habilitar Listing; `CaptationCaptured`.
- `SUP-014` Cerrar perdida con motivo estructurado/texto sin exigir formulario largo; `CaptationLost`.
- `SUP-015` Dormir/reactivar por fecha/evento para no ensuciar pipeline; `CaptationDormant` / `CaptationReactivated`.

## Reglas

Cerrar/dormir preserva métricas e historia. Quick close debe ser posible con motivo mínimo.

## DoD
- [ ] Lifecycle probado.
- [ ] Activación de siguiente paso solo al quedar capturada.
- [ ] Métricas/resultados conservados.

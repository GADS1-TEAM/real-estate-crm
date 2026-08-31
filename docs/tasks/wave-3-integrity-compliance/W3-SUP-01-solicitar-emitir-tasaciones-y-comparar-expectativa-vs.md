# W3-SUP-01 — Solicitar/emitir tasaciones y comparar expectativa vs valoración

**Dependencias:** `W2-SUP-01`, `W3-PRP-01`  
**Casos de uso:** `SUP-005`, `SUP-006`, `SUP-007`, `SUP-008`

## Casos de uso

- `SUP-005` Solicitar `Valuation`, asociarla a inmueble/captación y coordinar responsable; `ValuationRequested`.
- `SUP-006` Emitir tasación con valor, moneda, metodología, comparables, confidence, expectativa titular y docs; `ValuationIssued`.
- `SUP-007` Tasación rural con valor/hectárea y variables productivas/comparables rurales; `ValuationIssued`.
- `SUP-008` Comparar tasación vs expectativa mediante read model para captación/comercialización.

## Reglas

Valuation es entidad formal e histórica. Nunca reemplazarla por `estimatedPrice` dentro de Property.

## DoD
- [ ] Urbana/rural soportadas.
- [ ] Historial de tasaciones preservado.
- [ ] Brecha y confidence visibles.

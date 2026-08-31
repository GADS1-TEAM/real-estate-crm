# W4-CLS-03 — Crear/confirmar reserva, registrar seña y resolver expiración/cancelación

**Dependencias:** `W4-CLS-02`, `W3-DOC-01`  
**Casos de uso:** `CLS-009`, `CLS-010`, `CLS-011`, `CLS-012`

- `CLS-009`: crear `Reservation` separada de Proposal aceptada; `ReservationCreated`.
- `CLS-010`: confirmar y habilitar siguiente workflow; `ReservationConfirmed`.
- `CLS-011`: registrar evidencia/monto de seña sin convertir closing en contabilidad; `ReservationFundsRecorded`.
- `CLS-012`: expirar/cancelar preservando resultado; `ReservationExpired`/`ReservationCancelled`.

## Reglas
Reservation tiene lifecycle/documentos/condiciones propios.

## DoD
- [ ] Lifecycle + funds evidence testeados.
- [ ] No mezclar Reservation con Payment ledger de alquileres.

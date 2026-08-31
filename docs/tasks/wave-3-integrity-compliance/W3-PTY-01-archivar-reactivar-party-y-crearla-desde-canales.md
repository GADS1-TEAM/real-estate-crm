# W3-PTY-01 — Archivar/reactivar Party y crearla desde canales de forma automática

**Dependencias:** `W1-PTY-01`, `W2-INT-01`  
**Casos de uso:** `PTY-012`, `PTY-013`, `PTY-016`

## Casos de uso

- `PTY-012` Archivar Party sin borrar evidencia/relaciones; `PartyArchived`.
- `PTY-013` Reactivar Party histórica que retoma contacto sin duplicarla; `PartyReactivated`.
- `PTY-016` Crear Party provisional desde canal cuando existe mínimo identificable y resolver duplicados asíncronamente; `PartyRegistered`.

## Reglas

Archive es lifecycle, no delete. Creación automática debe conservar source/provenance y no esperar Identity Resolution.

## DoD
- [ ] Archive/reactivate testeados.
- [ ] Channel creation idempotente.
- [ ] Identity Resolution se dispara por evento.

# W3-PRP-01 — Derechos/intereses, registros legales e historia significativa

**Dependencias:** `W2-PRP-01`, `W3-DOC-01`  
**Casos de uso:** `PRP-006`, `PRP-007`, `PRP-008`, `PRP-009`

## Casos de uso

- `PRP-006` Registrar `PropertyInterest`: dominio, usufructo, nuda propiedad, fideicomiso u otro derecho/interés con participación/vigencia; `PropertyInterestAdded`.
- `PRP-007` Finalizar interés preservando historia; `PropertyInterestEnded`.
- `PRP-008` Registrar matrícula, partida, nomenclatura, gravámenes u otros `PropertyLegalRecord`; `PropertyLegalRecordChanged`.
- `PRP-009` Registrar reforma, superficie, subdivisión u otro cambio de negocio significativo; `PropertyChanged`.

## Reglas

No modelar “ownerId” simple. Derechos/intereses son extensibles y temporales. Historia significativa separada del audit técnico.

## DoD
- [ ] Múltiples intereses/tipos/porcentajes soportados.
- [ ] Vencimiento/historia testeados.
- [ ] Documentos se vinculan por IDs, no binarios embebidos.

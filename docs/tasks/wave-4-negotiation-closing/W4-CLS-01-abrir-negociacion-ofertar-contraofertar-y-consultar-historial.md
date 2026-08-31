# W4-CLS-01 — Abrir negociación, ofertar/contraofertar y consultar historial

**Dependencias:** `W3-VIS-03`  
**Casos de uso:** `CLS-001`, `CLS-002`, `CLS-003`, `CLS-008`

- `CLS-001`: crear `Negotiation` sobre Listing + Parties; `NegotiationOpened`.
- `CLS-002`: registrar `Proposal` con términos, condiciones, vigencia y proponente; `ProposalSubmitted`.
- `CLS-003`: contraoferta crea nueva Proposal vinculada; nunca sobrescribe anterior.
- `CLS-008`: consultar secuencia completa con actor/provenance.

## Reglas
Historial append-style, términos complejos reutilizan `CommercialTerms`; tenant/scope y auditoría obligatorios.

## DoD
- [ ] Offer/counteroffer/history testeados.
- [ ] No existe mutación destructiva de propuestas.

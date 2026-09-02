# W4-CLS-01 — Abrir negociación, ofertar/contraofertar y consultar historial

**Dependencias:** `W3-VIS-03`  
**Casos de uso:** `CLS-001`, `CLS-002`, `CLS-003`, `CLS-008`

- `CLS-001`: crear `Negotiation` sobre Listing + Parties; `NegotiationOpened`.
- `CLS-002`: registrar `Proposal` con términos, condiciones, vigencia y proponente; `ProposalSubmitted`.
- `CLS-003`: contraoferta crea nueva Proposal vinculada; nunca sobrescribe anterior.
- `CLS-008`: consultar secuencia completa con actor/provenance.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Reglas
Historial append-style, términos complejos reutilizan `CommercialTerms`; tenant/scope y auditoría obligatorios.

## DoD
- [ ] Offer/counteroffer/history testeados.
- [ ] No existe mutación destructiva de propuestas.

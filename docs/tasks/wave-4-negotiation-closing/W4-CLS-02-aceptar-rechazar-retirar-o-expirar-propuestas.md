# W4-CLS-02 — Aceptar, rechazar, retirar o expirar propuestas

**Dependencias:** `W4-CLS-01`  
**Casos de uso:** `CLS-004`, `CLS-005`, `CLS-006`, `CLS-007`

- `CLS-004`: aceptar Proposal vigente y congelar términos base; `ProposalAccepted`.
- `CLS-005`: rechazar preservando métricas; `ProposalRejected`.
- `CLS-006`: retirar por proponente si estado/policy permite; `ProposalWithdrawn`.
- `CLS-007`: expirar automáticamente por fecha; `ProposalExpired` + notificación.

## Reglas
Transiciones formales/idempotentes; no borrar Proposal.

## DoD
- [ ] Todas las transiciones válidas/inválidas testeadas.
- [ ] Expiración automática idempotente.

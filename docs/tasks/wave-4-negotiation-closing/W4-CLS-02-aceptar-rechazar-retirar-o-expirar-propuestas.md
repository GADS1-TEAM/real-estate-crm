# W4-CLS-02 — Aceptar, rechazar, retirar o expirar propuestas

**Dependencias:** `W4-CLS-01`  
**Casos de uso:** `CLS-004`, `CLS-005`, `CLS-006`, `CLS-007`

- `CLS-004`: aceptar Proposal vigente y congelar términos base; `ProposalAccepted`.
- `CLS-005`: rechazar preservando métricas; `ProposalRejected`.
- `CLS-006`: retirar por proponente si estado/policy permite; `ProposalWithdrawn`.
- `CLS-007`: expirar automáticamente por fecha; `ProposalExpired` + notificación.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Reglas
Transiciones formales/idempotentes; no borrar Proposal.

## DoD
- [ ] Todas las transiciones válidas/inválidas testeadas.
- [ ] Expiración automática idempotente.

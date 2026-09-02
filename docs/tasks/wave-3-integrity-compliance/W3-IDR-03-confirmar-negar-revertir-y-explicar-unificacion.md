# W3-IDR-03 — Confirmar, negar, revertir y explicar unificación

**Dependencias:** `W3-IDR-02`  
**Casos de uso:** `IDR-007`, `IDR-008`, `IDR-009`, `IDR-010`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`, `dotnet-adversarial-testing`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `IDR-007` Ejecutivo autorizado confirma misma identidad; unificación no destructiva; `IdentityMatchConfirmed`.
- `IDR-008` Confirmar identidades distintas y conservar evidencia para evitar propuesta repetida; `IdentityDistinctConfirmed`.
- `IDR-009` Revertir canonicalización incorrecta con policy de referencias y conservar ambas decisiones; `IdentityResolutionReversed`.
- `IDR-010` Consultar score, evidencia, algoritmo, actor y decisión que justificaron resolución.

## Reglas

Nunca borrar Parties origen como efecto del merge. Canonicalización y aliases/referencias deben poder reconstruirse y revertirse.

## DoD
- [ ] Confirm/distinct/reverse testeados.
- [ ] Historial explicable.
- [ ] Permisos fuertes y auditoría.

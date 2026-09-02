# W3-IDR-02 — Calcular score, auto-resolver alta confianza o solicitar revisión

**Dependencias:** `W3-IDR-01`  
**Casos de uso:** `IDR-004`, `IDR-005`, `IDR-006`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`, `dotnet-adversarial-testing`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `IDR-004` Calcular score explicable con evidencia ponderada + versión de algoritmo; `IdentityMatchScored`.
- `IDR-005` Auto-resolver solo sobre umbral seguro y sin conflictos, usando unificación no destructiva; `IdentityAutoResolved`.
- `IDR-006` Si es ambiguo/conflictivo, crear revisión humana; `IdentityReviewRequested`.

## Reglas

No usar porcentaje naïve sin ponderación. DNI/pasaporte/identificadores fuertes pesan distinto de nombre/dirección. Thresholds configurables/versionados. Merge siempre reversible/auditable.

## DoD
- [ ] Casos auto/review/separate testeados.
- [ ] Score y evidence visibles a auditor autorizado.
- [ ] Idempotencia ante eventos repetidos.

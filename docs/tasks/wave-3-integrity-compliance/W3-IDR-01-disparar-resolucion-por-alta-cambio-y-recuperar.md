# W3-IDR-01 — Disparar resolución por alta/cambio y recuperar candidatos

**Dependencias:** `W2-PTY-02`  
**Casos de uso:** `IDR-001`, `IDR-002`, `IDR-003`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`, `dotnet-adversarial-testing`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `IDR-001` Tras `PartyRegistered`, abrir `IdentityResolutionCase` de forma asíncrona sin bloquear alta; `IdentityResolutionStarted`.
- `IDR-002` Reevaluar solo al cambiar atributos relevantes de identidad; no por notas/tags; `IdentityResolutionStarted`.
- `IDR-003` Buscar candidatos por identificadores exactos y matching fuzzy/probabilístico. POC: MongoDB detrás de port, no search-service desplegado; `IdentityCandidatesFound`.

## Reglas

Identity Resolution consume eventos de Party; no modifica Party directamente salvo mediante contrato explícito. Candidate retrieval tenant-scoped.

## DoD
- [ ] Alta no espera deduplicación.
- [ ] Cambios irrelevantes no disparan resolución.
- [ ] Candidatos reproducibles/testeados.

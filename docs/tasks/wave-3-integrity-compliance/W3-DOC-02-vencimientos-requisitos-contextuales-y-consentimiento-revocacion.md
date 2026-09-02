# W3-DOC-02 — Vencimientos, requisitos contextuales y consentimiento/revocación

**Dependencias:** `W3-DOC-01`, `W1-PLT-01`  
**Casos de uso:** `DOC-005`, `DOC-006`, `DOC-012`, `DOC-013`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `aspnetcore-config-and-secrets`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `DOC-005` Detectar expiración efectiva de documento y emitir `DocumentExpired`.
- `DOC-006` Mostrar requisitos documentales actuales/futuros/recomendados mediante `DocumentRequirement` + `CompletenessProfile`; consulta sin evento artificial.
- `DOC-012` Registrar `ConsentGrant` con finalidad, versión y vigencia; `ConsentGranted`.
- `DOC-013` Revocar consentimiento para tratamientos futuros preservando evidencia histórica; `ConsentRevoked`.

## Reglas

Requisitos vienen de packs/policies versionados. No bloquear captura inicial por requisitos de una etapa futura. Consentimiento es específico y auditable.

## DoD
- [ ] Vencimientos/requisitos testeados por fecha/etapa.
- [ ] Grant/revoke consent auditables.
- [ ] UI explica qué es requerido ahora y por qué.

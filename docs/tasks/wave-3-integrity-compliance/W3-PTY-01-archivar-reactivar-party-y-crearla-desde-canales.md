# W3-PTY-01 — Archivar/reactivar Party y crearla desde canales de forma automática

**Dependencias:** `W1-PTY-01`, `W2-INT-01`  
**Casos de uso:** `PTY-012`, `PTY-013`, `PTY-016`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

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

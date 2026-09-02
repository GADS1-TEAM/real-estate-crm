# W3-VIS-02 — Sincronizar Google y Outlook mediante adapters

**Dependencias:** `W3-VIS-01`  
**Casos de uso:** `VIS-003`, `VIS-004`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `aspnetcore-outgoing-http`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `VIS-003` Sincronizar Google Calendar con mapping idempotente; `CalendarSyncSucceeded` / `CalendarSyncFailed`.
- `VIS-004` Sincronizar Outlook Calendar con el mismo port y adapter independiente; mismos eventos.

## Reglas

Dominio no conoce proveedores. Fallas externas no corrompen `ScheduleItem`; retries controlados e idempotentes.

## DoD
- [ ] Adapter contract común.
- [ ] Tests de create/update/delete/retry.
- [ ] Estado de sync visible sin bloquear agenda interna.

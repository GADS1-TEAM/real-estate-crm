# W3-VIS-01 — Crear, consultar disponibilidad, reprogramar y cancelar citas

**Dependencias:** `W1-ORG-02`  
**Casos de uso:** `VIS-001`, `VIS-002`, `VIS-005`, `VIS-006`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `aspnetcore-outgoing-http`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `VIS-001` Crear `ScheduleItem` para tasación, firma, seguimiento u otro evento; `ScheduleItemCreated`.
- `VIS-002` Consultar disponibilidad interna + calendarios conectados según permisos.
- `VIS-005` Reprogramar con motivo y notificación; `ScheduleItemRescheduled`.
- `VIS-006` Cancelar sin borrar historial y sincronizar adapters; `ScheduleItemCancelled`.

## Reglas

Scheduling modela agenda; Visit sigue siendo aggregate comercial separado. Calendar providers detrás de adapters.

## DoD
- [ ] Conflictos/disponibilidad testeados.
- [ ] Reprogramar/cancelar idempotente.
- [ ] Scope de agenda respetado.

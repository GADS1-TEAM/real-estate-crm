# W3-VIS-02 — Sincronizar Google y Outlook mediante adapters

**Dependencias:** `W3-VIS-01`  
**Casos de uso:** `VIS-003`, `VIS-004`

## Casos de uso

- `VIS-003` Sincronizar Google Calendar con mapping idempotente; `CalendarSyncSucceeded` / `CalendarSyncFailed`.
- `VIS-004` Sincronizar Outlook Calendar con el mismo port y adapter independiente; mismos eventos.

## Reglas

Dominio no conoce proveedores. Fallas externas no corrompen `ScheduleItem`; retries controlados e idempotentes.

## DoD
- [ ] Adapter contract común.
- [ ] Tests de create/update/delete/retry.
- [ ] Estado de sync visible sin bloquear agenda interna.

# W2-SUP-03 — Activar, pausar, reanudar y cerrar un Listing

**Dependencias:** `W2-SUP-02`  
**Casos de uso:** `SUP-020`, `SUP-021`, `SUP-022`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `SUP-020` Activar Listing cuando cumple mínimo/policy; evento `ListingActivated` y habilitación de matching/sindicación configurada.
- `SUP-021` Pausar/reanudar temporalmente sin destruir historia; eventos `ListingPaused`, `ListingResumed`.
- `SUP-022` Cerrar/retirar por resultado, expiración o decisión del titular; eventos `ListingClosed` / `ListingWithdrawn`.

## Reglas

Lifecycle formal y auditable. Cerrar no borra métricas ni relación con la Property. Activación solo exige mínimo contextual, no perfil “perfecto”.

## DoD
- [ ] Transiciones válidas/invalidas testeadas.
- [ ] Eventos idempotentes vía Outbox.
- [ ] Read models reflejan lifecycle.

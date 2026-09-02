# W1-ORG-01 — Crear una inmobiliaria y dejarla lista para operar con defaults

**Dependencias:** `FND-005`, `FND-006`  
**Casos de uso:** `ORG-001`, `ORG-002`, `ORG-014`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Objetivo

Implementar onboarding plug-and-play: alta de organización, estructura organizacional mínima y aplicación versionada de defaults argentinos.

## Casos de uso

- `ORG-001` Crear organización: `Organization`, `Capability`, `PackManifest`; servicios `access-service` y `platform-config-service`; eventos `OrganizationCreated`, `PackApplied`.
- `ORG-002` Crear estructura organizacional: `OrganizationalUnit`; `access-service`; evento `OrganizationalUnitCreated`.
- `ORG-014` Onboarding plug and play: organización, unidad default y configuración inicial; evento `OrganizationReady`.

## UX

Pedir solo lo necesario para comenzar; aplicar defaults y permitir configuración avanzada posteriormente.

## Criterios de aceptación

- [ ] Una organización puede quedar operativa sin configurar catálogos o workflows manualmente.
- [ ] El pack aplicado queda versionado y auditable.
- [ ] La estructura soporta múltiples sucursales/equipos.
- [ ] La autorización queda activa desde el alta.

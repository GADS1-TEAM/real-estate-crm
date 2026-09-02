# W1-PTY-01 — Party mínima, contacto, búsqueda y completitud

**Dependencias:** `FND-005`, `FND-006`  
**Casos de uso:** `PTY-001`, `PTY-005`, `PTY-011`, `PTY-014`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Objetivo

Permitir trabajar con una Party desde el primer dato útil sin exigir perfil completo y sin perder capacidad de enriquecerla después.

## Casos de uso

- `PTY-001` Registrar Party mínima: `Party`, `ContactPoint`; `party-service`; evento `PartyRegistered`.
- `PTY-005` Agregar contacto: email/teléfono/WhatsApp con procedencia/verificación; evento `PartyIdentityAttributesChanged`.
- `PTY-011` Buscar Party: nombre/contacto/documento dentro del scope. POC: índices MongoDB detrás de un search port; no desplegar `search-service`.
- `PTY-014` Ver completitud: `CompletenessProfile` derivado; mostrar qué información falta y qué valor habilita sin bloquear.

## Reglas

- una Party no se duplica por rol comercial;
- `UNKNOWN` no equivale a falso/cero;
- datos enriquecidos son opcionales hasta que una acción concreta los necesite;
- toda búsqueda respeta tenant/scope;
- `PartyRegistered` y cambios de identidad habilitan Identity Resolution de forma asíncrona.

## Criterios de aceptación

- [ ] Nombre + un contacto suficiente puede crear una Party.
- [ ] La misma Party puede enriquecerse sin crear otra entidad por rol.
- [ ] Búsqueda funciona con índices MongoDB POC.
- [ ] Completitud sugiere; no bloquea captura mínima.
- [ ] Existe test explícito de aislamiento entre tenants.

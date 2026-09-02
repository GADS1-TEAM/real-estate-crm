# W2-PRP-01 — Modelar edificios/campos/unidades y analizar rendimiento agregado

**Dependencias:** `W1-PRP-01`  
**Casos de uso:** `PRP-004`, `PRP-005`, `PRP-012`, `PRP-014`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `PRP-004` Crear jerarquía desarrollo→torre→unidad o campo→lote→instalación mediante referencias entre Properties; evento `PropertyRelationshipChanged`.
- `PRP-005` Asociar accesorios como cochera/baulera/galpón sin embutir aggregates independientes.
- `PRP-012` Consultar `Property360` con estructura, intereses, captaciones, listings, actividad y operaciones.
- `PRP-014` Analizar rendimiento agregado de edificio/desarrollo/campo mediante `PropertyGroupPerformance`.

## Reglas

No crear documentos Mongo gigantes. Jerarquía por IDs; cada Property conserva lifecycle/concurrencia independiente.

## DoD
- [ ] Jerarquía y accesorios testeados.
- [ ] No hay ciclos inválidos si la relación lo prohíbe.
- [ ] Read models reconstruibles.

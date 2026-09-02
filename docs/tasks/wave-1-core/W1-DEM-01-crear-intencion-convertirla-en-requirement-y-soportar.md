# W1-DEM-01 — Inquiry, Requirement y necesidades simultáneas

**Dependencias:** `W1-PTY-01`  
**Casos de uso:** `DEM-001`, `DEM-002`, `DEM-006`, `DEM-007`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Objetivo

Modelar intención comercial sin crear un Lead permanente y permitir múltiples Requirements concurrentes por Party.

## Casos de uso

- `DEM-001` Crear Inquiry: señal comercial todavía no clasificada; `Inquiry`; evento `InquiryCreated`.
- `DEM-002` Calificar Inquiry como Requirement: preservar vínculo/provenance; `InquiryQualified`, `RequirementCreated`.
- `DEM-006` Crear Requirement mínimo: registrar necesidad con datos mínimos, sin exigir criterios completos; evento `RequirementCreated`.
- `DEM-007` Mantener múltiples Requirements por Party: vivienda, inversión, campo, local u otros objetivos de manera independiente.

## Reglas

- `Interaction` no implica nuevo proceso comercial.
- `Inquiry` es transitorio; no es sinónimo de Party ni de Requirement.
- una misma Party puede mantener N Requirements activos;
- captura mínima y progressive disclosure;
- no duplicar datos de Party dentro del aggregate Requirement más allá de snapshots contractuales necesarios.

## Criterios de aceptación

- [ ] Crear Inquiry desde una señal comercial.
- [ ] Calificarla sin perder provenance.
- [ ] Crear Requirement con información mínima.
- [ ] Una Party puede tener varias búsquedas simultáneas independientes.
- [ ] Eventos incluyen tenant/actor/correlation metadata.

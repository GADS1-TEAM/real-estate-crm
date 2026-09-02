# W2-DEM-01 — Resolver Inquiry como captación, caso existente o no comercial

**Dependencias:** `W1-DEM-01`, `W2-SUP-01`  
**Casos de uso:** `DEM-003`, `DEM-004`, `DEM-005`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `DEM-003` **Calificar Inquiry como captación** (Agente/IA): Derivar intención del propietario hacia CaptationCase sin crear un Lead genérico. Servicios: demand-service, supply-service. Objetos: Inquiry, CaptationCase. Eventos: InquiryQualified, CaptationCaseOpened.
- `DEM-004` **Vincular Inquiry a caso existente** (Agente/IA): Cerrar la Inquiry como continuidad de Requirement/Captation/Transaction ya existente. Servicios: demand-service. Objetos: Inquiry. Eventos: InquiryQualified.
- `DEM-005` **Cerrar Inquiry no comercial** (Agente/IA): Cerrar contacto informativo/administrativo sin inflar pipeline comercial. Servicios: demand-service. Objetos: Inquiry. Eventos: InquiryClosed.

## Reglas

- Aplicar `AGENTS.md`, `ARCHITECTURE.md` y decisiones POC.
- Owner único de datos; cross-context por contratos/eventos/read models.
- `tenantId` obligatorio; sin acceso a colecciones ajenas.
- Datos enriquecidos opcionales salvo necesidad real.
- BFF sin lógica de dominio; UI task-driven.
- Eventos versionados + Outbox/Inbox cuando aplique.

## DoD

- [ ] UC completos y testeados.
- [ ] Aislamiento tenant/autorización.
- [ ] Contratos/eventos y observabilidad actualizados.
- [ ] PR acotado.

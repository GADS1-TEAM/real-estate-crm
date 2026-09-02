# W2-DEM-02 — Modelar must-have, preferido, indiferente/desconocido y confirmación

**Dependencias:** `W1-DEM-01`  
**Casos de uso:** `DEM-008`, `DEM-009`, `DEM-010`, `DEM-011`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `DEM-008` **Agregar criterio obligatorio** (Agente/IA): Registrar condición REQUIRED con valor, confidence y fuente. Servicios: demand-service. Objetos: RequirementCriterion. Eventos: RequirementChanged.
- `DEM-009` **Agregar criterio preferido** (Agente/IA): Registrar preferencia que suma score pero no excluye candidatos. Servicios: demand-service. Objetos: RequirementCriterion. Eventos: RequirementChanged.
- `DEM-010` **Registrar indiferencia/desconocido** (Agente/IA): Distinguir explícitamente INDIFFERENT de UNKNOWN para no inventar preferencias. Servicios: demand-service. Objetos: RequirementCriterion. Eventos: RequirementChanged.
- `DEM-011` **Confirmar criterio con cliente** (Agente/Cliente): Marcar un criterio extraído por IA como confirmado, actualizando confidence/provenance. Servicios: demand-service. Objetos: RequirementCriterion. Eventos: RequirementCriterionConfirmed.

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

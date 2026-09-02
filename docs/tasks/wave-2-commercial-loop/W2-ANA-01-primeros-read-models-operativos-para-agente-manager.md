# W2-ANA-01 — Primeros read models operativos para agente/manager y embudo

**Dependencias:** `W2-SUP-03`, `W2-DEM-03`, `W2-MAT-02`  
**Casos de uso:** `ANA-001`, `ANA-002`, `ANA-003`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `cqrs-read-models-projections`, `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `ANA-001` **Ver AgentToday** (Agente): Obtener lista priorizada de acciones de hoy derivada de SLA, agenda, scores, workflows y eventos. Servicios: analytics-copilot-service. Objetos: AgentToday. Eventos: —.
- `ANA-002` **Ver ManagerCockpit** (Manager/Director): Detectar qué está trabado, enfriándose, sin responder, en mora o con oportunidad prioritaria por scope. Servicios: analytics-copilot-service. Objetos: ManagerCockpit. Eventos: —.
- `ANA-003` **Ver embudo comercial** (Manager): Analizar Inquiry→Requirement/Captation→Visit→Proposal→Reservation→Close con calidad de dato explícita. Servicios: analytics-copilot-service. Objetos: MetricObservation. Eventos: —.

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

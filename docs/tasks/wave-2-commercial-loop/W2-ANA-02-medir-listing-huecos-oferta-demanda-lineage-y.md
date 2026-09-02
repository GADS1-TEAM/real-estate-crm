# W2-ANA-02 — Medir Listing, huecos oferta-demanda, lineage y UNKNOWN vs cero

**Dependencias:** `W2-ANA-01`  
**Casos de uso:** `ANA-005`, `ANA-007`, `ANA-008`, `ANA-010`, `ANA-011`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `cqrs-read-models-projections`, `mongodb-document-modeling`, `mongodb-dotnet-driver`, `event-driven-outbox-inbox`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Casos de uso

- `ANA-005` **Analizar rendimiento de Listing** (Agente/Manager): Ver días activo, inquiries, parties únicas, matches, presentaciones, visitas, propuestas y cambios de precio. Servicios: analytics-copilot-service. Objetos: ListingPerformance. Eventos: —.
- `ANA-007` **Detectar demanda sin oferta** (Manager): Encontrar Requirements activos sin Listing compatible y agregarlos por zona/tipo/presupuesto. Servicios: analytics-copilot-service. Objetos: ManagerCockpit. Eventos: —.
- `ANA-008` **Detectar oferta sin demanda** (Manager): Encontrar Listings activos con baja demanda/matching y sugerir precio/captación/estrategia. Servicios: analytics-copilot-service. Objetos: ListingPerformance. Eventos: —.
- `ANA-010` **Rastrear lineage de una métrica** (Manager/Auditor): Abrir una métrica y navegar hasta observaciones/eventos/datos fuente que la componen. Servicios: analytics-copilot-service. Objetos: MetricDefinition, MetricObservation. Eventos: —.
- `ANA-011` **Distinguir cero de dato no informado** (Sistema/Usuario): Representar calidad/completitud para no interpretar ausencia de carga como cero real. Servicios: analytics-copilot-service. Objetos: CompletenessProfile, MetricObservation. Eventos: —.

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

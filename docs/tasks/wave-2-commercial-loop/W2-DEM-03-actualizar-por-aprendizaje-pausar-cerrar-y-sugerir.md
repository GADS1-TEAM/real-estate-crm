# W2-DEM-03 — Actualizar por aprendizaje, pausar/cerrar y sugerir enriquecimiento

**Dependencias:** `W2-DEM-02`  
**Casos de uso:** `DEM-012`, `DEM-013`, `DEM-014`, `DEM-015`

## Casos de uso

- `DEM-012` **Modificar Requirement por aprendizaje** (Agente/IA): Actualizar criterios a partir de feedback de matches/visitas sin ocultar la fuente del cambio. Servicios: demand-service. Objetos: Requirement. Eventos: RequirementChanged.
- `DEM-013` **Pausar/reactivar Requirement** (Agente/Sistema): Sacar temporalmente una búsqueda del matching activo y reactivarla por fecha/evento. Servicios: demand-service. Objetos: Requirement. Eventos: RequirementPaused/Reactivated.
- `DEM-014` **Cerrar Requirement** (Agente/Sistema): Cerrar por satisfacción, abandono u otro motivo conservando métricas y resultado. Servicios: demand-service. Objetos: Requirement. Eventos: RequirementClosed.
- `DEM-015` **Mostrar oportunidad de enriquecimiento** (Sistema): Indicar qué pocos datos adicionales mejorarían matching o seguimiento sin bloquear. Servicios: analytics-copilot-service. Objetos: CompletenessProfile. Eventos: —.

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

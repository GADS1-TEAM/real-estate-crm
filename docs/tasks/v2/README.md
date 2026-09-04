# Backlog V2 del CRM inmobiliario

Este es el backlog vigente para el trabajo práctico. Reemplaza como plan de
ejecución al backlog amplio de `docs/tasks/`, que se conserva como referencia
histórica y no debe ejecutarse para este alcance.

## Cómo usarlo

1. Leer el [plan maestro](../../implementation/IMPLEMENTATION_MASTER_PLAN.md) y el [ADR de alcance](../../adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md).
2. Elegir una task `TODO` del [board V2](TASK_BOARD.md) respetando sus dependencias.
3. Leer `AGENTS.md`, la task, las secciones relacionadas de `README.md` y `ARCHITECTURE.md` y las consignas de `docs/consignas/`.
4. Implementar únicamente la zona de escritura declarada en la task; cualquier cambio de ownership, contrato o alcance requiere reabrir el ADR.
5. Adjuntar la evidencia de tests, contrato, autorización, telemetría y aceptación indicada por la task.

## Waves

- `wave-0-scope-design/` — alcance, modelo y entrada del diseño de Claude Design.
- `wave-1-foundation/` — esqueleto técnico, contratos, persistencia, infraestructura y observabilidad.
- `wave-2-access-catalogs/` — acceso, Party y catálogos comerciales obligatorios.
- `wave-3-real-estate-core/` — Property, Listing, demanda, captación, valuación y matching.
- `wave-4-commercial-progression/` — oportunidad como fachada, embudo y progresión inmobiliaria.
- `wave-5-history-analytics/` — actividades, historial, métricas y estadísticas.
- `wave-6-ai-release/` — IA revisable, búsqueda, regresión y entrega.

## Límites no negociables

No implementar multi-tenant/`tenantId`, agenda, disponibilidad, workflow,
tasks, notificaciones, envío o sincronización de email/WhatsApp, portales,
integraciones, administración de alquileres, pagos, comisiones, mantenimiento
ni una entidad/colección fuente `Opportunity`. Las actividades de correo o
WhatsApp son solo registros manuales de hechos ocurridos.

La fachada `/opportunities` puede existir en el BFF para el lenguaje de la
consigna, pero sus comandos deben resolverse en `Requirement` o
`CaptationCase` y sus hitos propietarios.


# Template de task ejecutable

> Las tasks de este repositorio son unidades de ejecución. El detalle de dominio se completa leyendo únicamente la task, `README.md`, `ARCHITECTURE.md`, `AGENTS.md`, `docs/implementation/POC_TECH_DECISIONS.md` y los contratos de dependencias directas.

## Identidad

- **Task ID:** `<ID>`
- **Ola:** `<wave>`
- **Estado:** `TODO | READY | IN_PROGRESS | BLOCKED | REVIEW | DONE | DEFERRED`
- **Dependencias:** `<IDs>`
- **UCs:** `<UC IDs>`
- **Owner:** `<service/bounded context>`

## Resultado esperado

Qué capacidad observable debe existir al terminar.

## Alcance trazable

Para cada UC indicar actor, comportamiento, entidad/objeto owner y eventos/contratos afectados.

## Reglas de ejecución

- Respetar ownership de datos por bounded context y Aggregate Root.
- Ningún servicio lee o escribe colecciones de otro servicio.
- `tenantId`, autorización efectiva y actor/correlation/causation metadata cuando aplique.
- MongoDB detrás de ports; índices guiados por queries reales.
- RabbitMQ + Outbox/Inbox/idempotencia cuando haya integración asíncrona.
- BFF compone experiencia, no decide reglas de dominio.
- UX task-driven, captura mínima y progressive disclosure.
- Contratos públicos/eventos versionados y compatibles o breaking change explícito.

## Overrides POC

Registrar aquí cualquier decisión que difiera de la arquitectura objetivo: mobile/telefonía/search/asset service diferidos, adapter local, mock, etc.

## Definition of Done

- [ ] Todos los UCs listados están implementados y trazables.
- [ ] Build/lint/typecheck/tests aplicables en verde.
- [ ] Unit/integration/contract/adversarial tests según los bordes tocados.
- [ ] Tenant isolation, autorización, observabilidad y manejo de errores validados.
- [ ] Contratos/eventos/examples y documentación afectados actualizados.
- [ ] PR limitado a esta task o desviaciones explícitamente justificadas.

## Evidencia requerida en el PR

1. UCs implementados.
2. Endpoints/comandos/eventos modificados.
3. Tests ejecutados y resultado.
4. Evidencia de UI cuando aplique.
5. Assumptions, riesgos y follow-ups fuera de scope.

# W4-COMI-02 — Calcular comisión, splits, ajustes y estimación

**Ola:** 4 — Negociación y cierre  
**Estado inicial:** `TODO`  
**Dependencias:** `W4-COMI-01`, `W4-CLS-04`  
**Casos de uso:** `COMI-003`, `COMI-004`, `COMI-005`, `COMI-006`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado esperado

Calcular comisión, splits, ajustes y estimación.

## Casos de uso incluidos

| ID | Caso | Actor | Qué debe resolver | Superficie | Servicio(s) | Entidades/objetos | Evento(s) |
|---|---|---|---|---|---|---|---|
| `COMI-003` | **Calcular comisión de operación** | Sistema | Evaluar policy versionada contra Transaction cerrada y generar CommissionCalculation explicable. | Background | commission-service | CommissionCalculation, CommissionPolicy | CommissionCalculated |
| `COMI-004` | **Calcular splits** | Sistema | Distribuir honorario entre captador, vendedor, sucursal, gerente, referido u otros roles configurados. | Background | commission-service | CommissionSplit | CommissionCalculated |
| `COMI-005` | **Ajustar cálculo excepcional** | Usuario autorizado | Aplicar corrección/override con motivo y auditoría sin alterar policy histórica. | CRM Web | commission-service | CommissionCalculation | CommissionAdjusted |
| `COMI-006` | **Consultar comisión estimada** | Agente/Manager | Simular honorarios e incentivos antes del cierre sin generar asiento definitivo. | CRM Web | commission-service | CommissionPolicy | — |

## Ownership

- **Servicios propietarios:** `commission-service`.
- El owner del aggregate/colección es el único que puede mutar sus datos.
- Integraciones entre bounded contexts mediante contratos, APIs, eventos o read models; nunca lectura directa de colecciones ajenas.

## Contexto obligatorio antes de implementar

1. Esta task.
2. `README.md` para lenguaje ubicuo e invariantes de los objetos involucrados.
3. `ARCHITECTURE.md` para boundaries, servicios y decisiones vigentes.
4. `docs/implementation/POC_TECH_DECISIONS.md`.
5. Contratos producidos por las dependencias directas.
6. `docs/tasks/TASK_TEMPLATE.md` y `AGENTS.md` para checklist, DoD y evidencia de PR.

## Reglas de implementación

- Captura mínima y progressive disclosure: no convertir enriquecimiento en campos obligatorios sin invariante real.
- `tenantId` obligatorio en datos tenant-scoped e aislamiento probado explícitamente.
- MongoDB por aggregate root; referencias por ID entre aggregates independientes.
- Outbox para eventos ligados a cambios de estado; consumers idempotentes/Inbox cuando haya side effects.
- Propagar actor, `tenantId`, `correlationId` y `causationId`.
- BFF compone experiencia; no contiene reglas de dominio.
- No introducir Redis, OpenSearch, Kubernetes, warehouse, mobile o telefonía salvo que esta task lo permita expresamente.
- La PR debe limitarse a esta task o justificar cualquier desviación.

## Definition of Done

Aplicar íntegramente el DoD, estrategia de pruebas, observabilidad, seguridad, documentación y evidencia de PR definidos en `docs/tasks/TASK_TEMPLATE.md`. Cada UC listado arriba debe quedar verificablemente cubierto.

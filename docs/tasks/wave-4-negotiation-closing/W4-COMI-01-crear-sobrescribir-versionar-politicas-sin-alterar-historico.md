# W4-COMI-01 — Crear/sobrescribir/versionar políticas sin alterar histórico

**Ola:** 4 — Negociación y cierre  
**Estado inicial:** `TODO`  
**Dependencias:** `W3-PLT-02`, `W1-ORG-02`  
**Casos de uso:** `COMI-001`, `COMI-002`, `COMI-008`

## Resultado esperado

Crear/sobrescribir/versionar políticas sin alterar histórico.

## Casos de uso incluidos

| ID | Caso | Actor | Qué debe resolver | Superficie | Servicio(s) | Entidades/objetos | Evento(s) |
|---|---|---|---|---|---|---|---|
| `COMI-001` | **Crear policy de comisión del tenant** | Director/Admin | Definir reglas propias a partir del default, con alcance organización/unidad/operación/rol y vigencia. | CRM Web | commission-service | CommissionPolicy | CommissionPolicyPublished |
| `COMI-002` | **Sobrescribir policy por sucursal** | Director | Aplicar incentivo/regla diferente a unidad específica sin duplicar toda la configuración. | CRM Web | commission-service | CommissionPolicy | CommissionPolicyPublished |
| `COMI-008` | **Versionar política sin afectar histórico** | Admin | Publicar nueva CommissionPolicy para operaciones futuras preservando versión usada en cálculos previos. | CRM Web/Platform Admin | commission-service | CommissionPolicy | CommissionPolicyPublished |

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

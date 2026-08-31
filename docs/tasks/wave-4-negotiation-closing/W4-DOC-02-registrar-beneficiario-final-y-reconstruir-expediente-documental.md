# W4-DOC-02 — Registrar beneficiario final y reconstruir expediente documental

**Ola:** 4 — Negociación y cierre  
**Estado inicial:** `TODO`  
**Dependencias:** `W4-DOC-01`  
**Casos de uso:** `DOC-011`, `DOC-014`

## Resultado esperado

Registrar beneficiario final y reconstruir expediente documental.

## Casos de uso incluidos

| ID | Caso | Actor | Qué debe resolver | Superficie | Servicio(s) | Entidades/objetos | Evento(s) |
|---|---|---|---|---|---|---|---|
| `DOC-011` | **Registrar beneficiario final** | Usuario autorizado | Guardar declaración/versiones de beneficiarios finales para Party/operación cuando corresponda. | CRM Web | documents-compliance-service | BeneficialOwnershipDeclaration | BeneficialOwnershipDeclared |
| `DOC-014` | **Auditar expediente documental** | Auditor/Manager | Reconstruir documentos, versiones, validaciones, actores y requisitos aplicados a una operación. | CRM Web/Platform Admin | documents-compliance-service, audit-service | Document, ComplianceCase | — |

## Ownership

- **Servicios propietarios:** `documents-compliance-service`, `audit-service`.
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

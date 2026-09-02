# W4-DOC-01 — Instanciar y completar cumplimiento con excepciones auditadas

**Ola:** 4 — Negociación y cierre  
**Estado inicial:** `TODO`  
**Dependencias:** `W3-DOC-02`, `W3-PLT-01`  
**Casos de uso:** `DOC-007`, `DOC-008`, `DOC-009`, `DOC-010`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `aspnetcore-config-and-secrets`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado esperado

Instanciar y completar cumplimiento con excepciones auditadas.

## Casos de uso incluidos

| ID | Caso | Actor | Qué debe resolver | Superficie | Servicio(s) | Entidades/objetos | Evento(s) |
|---|---|---|---|---|---|---|---|
| `DOC-007` | **Abrir ComplianceCase** | Sistema/Usuario | Instanciar controles versionados cuando una operación/etapa lo requiere. | CRM Web/Background | documents-compliance-service | ComplianceCase | ComplianceCaseOpened |
| `DOC-008` | **Satisfacer requisito de compliance** | Usuario/Sistema | Vincular evidencia o decisión válida a un requisito puntual manteniendo lineage. | CRM Web/Background | documents-compliance-service | ComplianceCase, Document | ComplianceRequirementSatisfied |
| `DOC-009` | **Solicitar excepción de compliance** | Agente | Registrar que un requisito no puede cumplirse por vía normal y pedir aprobación según policy. | CRM Web | documents-compliance-service | ComplianceCase | ComplianceExceptionRequested |
| `DOC-010` | **Completar ComplianceCase** | Sistema/Compliance Ops | Cerrar el caso cuando todos los controles aplicables están satisfechos o exceptuados válidamente. | CRM Web/Background | documents-compliance-service | ComplianceCase | ComplianceCaseCompleted |

## Ownership

- **Servicios propietarios:** `documents-compliance-service`.
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

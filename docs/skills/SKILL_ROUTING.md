# Ruteo de skills — qué carga un agente y cuándo

Un agente **no carga las 28 skills**. Carga el núcleo transversal más las skills de
los bordes que su task realmente toca.

Este documento es la fuente de verdad de esa decisión. Las skills viven en
[`.github/skills/`](../../.github/skills/); los gaps y el estado de cada una, en
[`SKILL_GAPS.md`](SKILL_GAPS.md).

> **Regla de contexto.** Si una task necesita más de ~6 skills, probablemente sea
> demasiado grande o su boundary esté mal definido. Ver
> [`EXECUTION_STRATEGY.md`](../implementation/EXECUTION_STRATEGY.md).

## 1. Núcleo transversal — siempre

Estas cuatro se cargan en toda task que produzca código de backend, sin excepción.

| Skill | Por qué siempre |
|---|---|
| [`aspnetcore-microservice-orchestrator`](../../.github/skills/aspnetcore-microservice-orchestrator/SKILL.md) | Meta-skill: indexa las demás y arbitra conflictos entre reglas. |
| [`ddd-hexagonal-architecture`](../../.github/skills/ddd-hexagonal-architecture/SKILL.md) | Decide dónde vive cada cosa. Se aplica antes que cualquier decisión de infraestructura. |
| [`multitenancy-authorization`](../../.github/skills/multitenancy-authorization/SKILL.md) | Todo dato de negocio es de un tenant. Olvidarse no rompe nada visible: filtra de menos. |
| [`aspnetcore-security-owasp-baseline`](../../.github/skills/aspnetcore-security-owasp-baseline/SKILL.md) | Baseline de seguridad para todo código expuesto a la red. |

Jerarquía ante conflicto entre skills: **seguridad > correctitud > performance >
ergonomía**. Y por encima de todas: `README.md`, `ARCHITECTURE.md` y `AGENTS.md`.

## 2. Por borde tocado

Se suman según lo que la task realmente construya.

| Si la task… | Cargar |
|---|---|
| Define o modifica un **aggregate**, entidad o value object | ya cubierto por el núcleo |
| **Persiste** algo por primera vez o cambia la forma de un documento | `mongodb-document-modeling` |
| Escribe un **repositorio** o cualquier código contra MongoDB | `mongodb-dotnet-driver` |
| **Publica o consume eventos** de integración | `event-driven-outbox-inbox` |
| Toca **topología, publisher o consumer** de RabbitMQ | `rabbitmq-dotnet` + `aspnetcore-messaging` |
| Expone o modifica **endpoints REST** o el BFF | `aspnetcore-rest-layer` |
| Recibe **datos externos**: body, query, respuesta upstream, mensaje | `dotnet-parsing-and-validation` |
| Toca **login, tokens, sesión o el realm** | `oidc-keycloak-aspnetcore` |
| Construye un **read model, dashboard o métrica** | `cqrs-read-models-projections` |
| Llama a **otro servicio por HTTP** | `aspnetcore-outgoing-http` |
| Toca **configuración, secretos o variables de entorno** | `aspnetcore-config-and-secrets` |
| Toca **DI, middleware o el pipeline** | `aspnetcore-di-and-middleware-pipeline` |
| Agrega **logs, métricas, trazas o manejo de errores** | `aspnetcore-error-and-observability` |
| Tiene **concurrencia, background services o estado compartido** | `dotnet-async-and-concurrency` + `dotnet-thread-safety-and-shared-state` |
| Escribe o amplía **tests** | `dotnet-unit-testing` + `dotnet-adversarial-testing` |
| Tiene un **hot path medido** o problemas de memoria | `dotnet-performance-and-memory` |
| Produce o actualiza **documentación o diagramas** | `common-repo-documentation` + `common-mermaid-diagrams` |
| Expone **API pública** que otros consumen | `dotnet-code-documentation-xmldoc` |
| Necesita **discovery previo** por no ser trivial | `prp-feature-discovery-dotnet` |

### Frontend

| Si la task… | Cargar |
|---|---|
| Toca `crm-web` o `platform-admin-web` | `nextjs-frontend-architecture` |
| Diseña o implementa un **formulario, pantalla o journey** | `crm-ux-quick-capture` |

En tasks de frontend, el núcleo transversal de backend **no aplica**: se cargan estas dos más `multitenancy-authorization` cuando la vista depende del scope del actor.

## 3. Ola 0 — mapeo concreto

La primera tanda de agentes. Núcleo transversal implícito en todas.

| Task | Skills adicionales |
|---|---|
| `FND-001` estructura del repositorio | `common-repo-documentation` |
| `FND-002` CI y calidad | `dotnet-unit-testing` |
| `FND-003` contratos compartidos | `dotnet-parsing-and-validation`, `event-driven-outbox-inbox`, `aspnetcore-rest-layer`, `dotnet-code-documentation-xmldoc` |
| `FND-004` infraestructura local | `aspnetcore-config-and-secrets`, `rabbitmq-dotnet`, `oidc-keycloak-aspnetcore`, `mongodb-dotnet-driver` |
| `FND-005` shells web + BFF | `aspnetcore-rest-layer`, `oidc-keycloak-aspnetcore`, `aspnetcore-di-and-middleware-pipeline`, `nextjs-frontend-architecture`, `crm-ux-quick-capture` |
| `FND-006` seguridad y tenancy | `oidc-keycloak-aspnetcore`, `aspnetcore-di-and-middleware-pipeline`, `dotnet-adversarial-testing` |
| `FND-007` observabilidad y resiliencia | `aspnetcore-error-and-observability`, `aspnetcore-outgoing-http`, `dotnet-async-and-concurrency` |
| `FND-008` harness de integración | `dotnet-unit-testing`, `dotnet-adversarial-testing`, `mongodb-dotnet-driver`, `rabbitmq-dotnet` |

## 4. Ola 1 — mapeo concreto

| Task | Skills adicionales |
|---|---|
| `W1-ORG-01` organización y onboarding | `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox` |
| `W1-ORG-02` usuarios, roles y permisos | `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `oidc-keycloak-aspnetcore`, `dotnet-adversarial-testing` |
| `W1-PLT-01` pack argentino y capabilities | `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer` |
| `W1-PTY-01` Party mínima | `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation` |
| `W1-PRP-01` Property mínima | `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox` |
| `W1-DEM-01` intención y Requirement | `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox` |

De la Ola 2 en adelante, derivar con la tabla de la sección 2. Cuando una task se
toma, su archivo debe declarar sus skills en la sección **Skills aplicables** del
[`TASK_TEMPLATE.md`](../tasks/TASK_TEMPLATE.md).

## 5. Skills que no se cargan

| Skill | Motivo |
|---|---|
| `aspnetcore-database-access-efcore` | **Archivada.** Enseña EF Core relacional; la persistencia es MongoDB. Ver [`_archived/README.md`](../../.github/skills/_archived/README.md). |

## 6. Estado de las skills heredadas

Las seis skills que arrastraban convenciones del arquetipo corporativo del que
fueron heredadas (`PaasControllerBase`, `IResponseBuilder`, `meta-data-error`, env
vars EPA, APIM) fueron **reescritas** contra las convenciones propias del proyecto,
una vez que [`ADR-006`](../adr/006-contrato-de-error-publico.md) fijó el contrato de
error.

No queda ninguna referencia al arquetipo ajeno. Se cargan sin advertencias.

## 7. Cuando falta una skill

Si la task necesita conocimiento que ninguna skill cubre:

1. no improvisar una convención nueva en silencio;
2. resolver con la opción más simple compatible con `ARCHITECTURE.md`;
3. documentarla en el PR;
4. anotar el gap en [`SKILL_GAPS.md`](SKILL_GAPS.md) como follow-up.

Si la decisión toca ownership, contrato público o una invariante core, no es un gap
de skill: es un [ADR](../adr/README.md) y la task se frena.

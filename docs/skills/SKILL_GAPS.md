# Skills — cobertura actual y gaps del CRM

Este documento no crea skills nuevas. Define qué conocimiento ya está cubierto por `.github/skills/` y qué skills específicas conviene diseñar antes de implementar los slices correspondientes.

## Skills existentes que se reutilizan

El repositorio ya dispone de una base .NET útil para:

- REST / ASP.NET Core;
- DI y middleware;
- async y concurrencia;
- thread safety;
- validación;
- HTTP saliente;
- mensajería genérica;
- configuración y secretos;
- observabilidad y errores;
- OWASP;
- performance/memoria;
- unit/adversarial testing;
- documentación XML;
- documentación de repositorio;
- Mermaid;
- discovery/PRP;
- orquestación de agentes.

Estas skills deben conservarse y aplicarse cuando correspondan. `README.md`, `ARCHITECTURE.md` y `AGENTS.md` tienen prioridad cuando una skill heredada asume un stack diferente al definido para este CRM.

## Alineación de skills heredadas

Las skills y agentes provienen de otro proyecto, con arquetipo corporativo `epa-net-paas`, .NET 8 y EF Core. Se hizo una primera pasada de alineación:

- versiones actualizadas a .NET 10 / ASP.NET Core 10 y C# 14;
- `epa-net-paas` removido de los `description` del frontmatter, para que las skills dejen de activarse por contexto ajeno;
- `aspnetcore-database-access-efcore` movida a [`.github/skills/_archived/`](../../.github/skills/_archived/README.md): enseña EF Core relacional y sus triggers coinciden con los de cualquier task de datos, lo que empujaba al anti-patrón de modelar MongoDB como SQL;
- los agentes apuntan a `AGENTS.md` como fuente de verdad del stack.

### Deuda saldada

Las seis skills cuyo cuerpo describía convenciones del arquetipo ajeno fueron **reescritas** una vez que [`ADR-006`](../adr/006-contrato-de-error-publico.md) definió el contrato de error propio:

| Skill | Qué reemplazó al arquetipo |
|---|---|
| `aspnetcore-rest-layer` | `ControllerBase` estándar y `problem+json` con `code` estable |
| `aspnetcore-error-and-observability` | Excepciones tipadas propias, logging sin PII, OpenTelemetry |
| `aspnetcore-di-and-middleware-pipeline` | `AddCrmPlatform()` / `UseCrmPlatform()` del building block propio |
| `aspnetcore-security-owasp-baseline` | BFF en vez de APIM, CORS por configuración tipada |
| `aspnetcore-outgoing-http` | Typed clients con resiliencia y adapters detrás de puertos |
| `aspnetcore-config-and-secrets` | Options tipadas con `ValidateOnStart()` |

También se limpiaron las menciones residuales en las otras skills. **El repositorio ya no contiene ninguna referencia al arquetipo `epa-net-paas`.**

## Skills específicas faltantes

### 1. `mongodb-document-modeling` — ✅ escrita

Vive en [`.github/skills/mongodb-document-modeling/`](../../.github/skills/mongodb-document-modeling/SKILL.md).

**Necesaria antes de:** `W1-PTY-01`, `W1-PRP-01`, `W1-DEM-01`.

Debe cubrir:

- aggregate root → documento/colección;
- embed vs reference;
- límites de tamaño y crecimiento;
- índices compuestos y parciales;
- `tenantId` como parte de filtros/índices;
- optimistic concurrency;
- atomicidad por documento;
- modelado temporal e historial selectivo;
- evolución/versionado de documentos;
- anti-patrones de modelar MongoDB como SQL sin joins.

### 2. `mongodb-dotnet-driver` — ✅ escrita

Vive en [`.github/skills/mongodb-dotnet-driver/`](../../.github/skills/mongodb-dotnet-driver/SKILL.md).

Decisiones fijadas al escribirla: driver **3.11.1 exacta** gestionada centralmente, LINQ3 sin proyecciones client-side, `Guid` binario `Standard` (subtype 4), MongoDB Server 8.x.

**Necesaria antes de:** primer repositorio productivo MongoDB.

Debe cubrir:

- `MongoClient` lifetime;
- collection configuration;
- serializers/conventions;
- `CancellationToken`;
- projections;
- index bootstrap;
- sessions/transacciones solo cuando realmente hacen falta;
- filtros obligatorios por tenant;
- testing con MongoDB real/Testcontainers;
- manejo de errores/retries sin duplicar writes.

### 3. `ddd-hexagonal-architecture` — ✅ escrita

Vive en [`.github/skills/ddd-hexagonal-architecture/`](../../.github/skills/ddd-hexagonal-architecture/SKILL.md).

**Necesaria antes de:** FND-001/FND-003 y todo servicio de dominio.

Debe cubrir:

- bounded contexts;
- aggregate roots, entities y value objects;
- invariantes;
- application commands/queries;
- domain events vs integration events;
- ports/adapters;
- dependency rule;
- ownership entre contextos;
- prohibición de importar modelos internos de otro servicio;
- cuándo una abstracción pertenece al dominio y cuándo es infraestructura.

### 4. `event-driven-outbox-inbox` — ✅ escrita

Vive en [`.github/skills/event-driven-outbox-inbox/`](../../.github/skills/event-driven-outbox-inbox/SKILL.md).

Decisiones fijadas al escribirla: outbox en **colección separada** escrita en la misma transacción que el aggregate (única excepción sancionada a la regla de una escritura por operación), relay como `BackgroundService`, inbox con índice único por `(tenantId, messageId)`, y retención de **7 días por TTL** en outbox e inbox. Requiere MongoDB como replica set: anotado en `FND-004`.

**Necesaria antes de:** primer evento cross-service.

Complementa `aspnetcore-messaging` con las decisiones específicas del proyecto:

- transactional outbox con MongoDB;
- publisher confiable;
- Inbox/deduplicación;
- idempotencia;
- correlation/causation IDs;
- retries y DLQ;
- event contracts versionados;
- compatibilidad hacia atrás;
- payload mínimo y minimización de PII;
- replay/rebuild de proyecciones.

### 5. `rabbitmq-dotnet` — ✅ escrita

Vive en [`.github/skills/rabbitmq-dotnet/`](../../.github/skills/rabbitmq-dotnet/SKILL.md).

Decisiones fijadas al escribirla: `RabbitMQ.Client` **7.x** (API totalmente async, `IChannel` reemplaza a `IModel`), exchange **topic por servicio publicador** (`<servicio>.events`), routing key `<aggregate>.<Evento>.v<N>`, queue por consumidor, DLQ por queue, publisher confirms y ack manual obligatorios, prefetch 20.

**Necesaria antes de:** FND-004/FND-008 o primer producer/consumer real.

Debe cubrir:

- RabbitMQ.Client/.NET;
- exchanges, queues y bindings;
- publisher confirms;
- manual ack;
- prefetch/backpressure;
- DLQ;
- retry topology;
- graceful shutdown;
- connection/channel lifetime;
- naming conventions por entorno;
- observabilidad.

Debe reutilizar las reglas agnósticas de `aspnetcore-messaging`, no duplicarlas.

### 6. `cqrs-read-models-projections` — ✅ escrita

Vive en [`.github/skills/cqrs-read-models-projections/`](../../.github/skills/cqrs-read-models-projections/SKILL.md).

Decisiones fijadas al escribirla: CQRS **selectivo** (read model solo cuando una query no puede resolverse en el servicio propietario), proyecciones alimentadas solo por eventos, projectors idempotentes con checkpoint, rebuild en colección nueva con cambio de puntero, y **prohibición explícita de contar `UNKNOWN` como cero** con denominador y excluidos visibles en toda métrica.

**Necesaria antes de:** `W2-ANA-01`, Party360/Property360 y dashboards.

Debe cubrir:

- cuándo CQRS aporta valor y cuándo no;
- proyecciones event-driven;
- eventual consistency explícita;
- checkpoints;
- idempotencia de projector;
- rebuild seguro;
- lineage de métricas;
- UNKNOWN vs cero;
- read models como derivados, nunca fuente de verdad.

### 7. `multitenancy-authorization` — ✅ escrita

Vive en [`.github/skills/multitenancy-authorization/`](../../.github/skills/multitenancy-authorization/SKILL.md).

Decisiones fijadas al escribirla: `tenantId` derivado **solo** del contexto autenticado, permisos como acción + recurso + scope resueltos por `access-service`, `404` en vez de `403` ante recurso ajeno, overrides con motivo/aprobador/vigencia, caché con TTL corto, y **cuatro tests obligatorios** por task que toque datos de negocio.

**Necesaria antes de:** FND-006.

Debe cubrir:

- `tenantId` derivado del contexto autenticado;
- no confiar en tenant enviado por body/query;
- RBAC + organizational scope + ownership + overrides;
- filtros de MongoDB por tenant;
- cache de autorización;
- BOLA/IDOR;
- pruebas de aislamiento;
- service-to-service actor context;
- auditoría de overrides y reasignaciones.

### 8. `oidc-keycloak-aspnetcore` — ✅ escrita

Vive en [`.github/skills/oidc-keycloak-aspnetcore/`](../../.github/skills/oidc-keycloak-aspnetcore/SKILL.md).

Decisiones fijadas al escribirla: Keycloak **26.7.x** (mínimo 26.7.2 por CVEs), **un realm de plataforma** con el tenant como claim, Authorization Code + PKCE terminado en el **BFF** con cookie `HttpOnly` (los tokens nunca llegan al navegador), validación de emisor/audiencia/firma/vigencia en cada servicio, realm versionado como código, y cero reglas de negocio dentro de Keycloak.

**Necesaria antes de:** FND-005/FND-006.

Debe cubrir:

- Authorization Code + PKCE para web;
- validación JWT en BFF/backend;
- configuración Keycloak POC;
- claims mínimos;
- separación autenticación vs autorización de negocio;
- refresh/session handling;
- logout;
- no codificar reglas organizacionales complejas en Keycloak.

## Skills frontend — ✅ escritas

| Skill | Cubre |
|---|---|
| [`nextjs-frontend-architecture`](../../.github/skills/nextjs-frontend-architecture/SKILL.md) | Next.js 16 Active LTS, App Router, TypeScript estricto, datos contra el BFF, estado, sesión sin tokens en el navegador, testing |
| [`crm-ux-quick-capture`](../../.github/skills/crm-ux-quick-capture/SKILL.md) | Journeys en vez de CRUD, captura mínima, progressive disclosure, tratamiento de `UNKNOWN`, completitud que sugiere, sugerencias explicables, accesibilidad |

Se escribieron sin esperar el set React/Next.js externo: las decisiones que importan
(hablar solo con el BFF, ningún token en el browser, captura mínima) salen de
`ARCHITECTURE.md` §4 y §14 y de [`ADR-005`](../adr/005-keycloak-realm-unico-y-patron-bff.md),
no de un set genérico.

Si más adelante aparece el set existente, se revisa contra estas dos para evitar
duplicaciones.

## Orden sugerido para crear skills

1. ~~`ddd-hexagonal-architecture`~~ ✅
2. ~~`mongodb-document-modeling`~~ ✅
3. ~~`mongodb-dotnet-driver`~~ ✅
4. ~~`event-driven-outbox-inbox`~~ ✅
5. ~~`rabbitmq-dotnet`~~ ✅
6. ~~`multitenancy-authorization`~~ ✅
7. ~~`oidc-keycloak-aspnetcore`~~ ✅
8. ~~`cqrs-read-models-projections`~~ ✅
9. ~~set React/Next.js~~ ✅ (`nextjs-frontend-architecture`, `crm-ux-quick-capture`)

## Regla de autoría

Las nuevas skills deben mantener la misma estructura que las existentes:

- frontmatter con `name` y `description` con triggers claros;
- Objetivo;
- Cuándo activar / cuándo NO activar;
- Decisiones del proyecto;
- MUST / MUST NOT;
- SHOULD / SHOULD NOT;
- anti-patrones;
- ejemplos correctos/incorrectos;
- checklist antes de entregar;
- conexiones con otras skills.

Evitar una mega-skill de “arquitectura”. Cada skill debe tener un trigger suficientemente específico y reglas accionables.

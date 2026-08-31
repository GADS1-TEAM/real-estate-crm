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

## Skills específicas faltantes

### 1. `mongodb-document-modeling`

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

### 2. `mongodb-dotnet-driver`

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

### 3. `ddd-hexagonal-architecture`

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

### 4. `event-driven-outbox-inbox`

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

### 5. `rabbitmq-dotnet`

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

### 6. `cqrs-read-models-projections`

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

### 7. `multitenancy-authorization`

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

### 8. `oidc-keycloak-aspnetcore`

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

## Skills frontend

No se crean todavía. Se incorporarán cuando se agregue al repositorio el set React/Next.js existente y se pueda revisar su estructura para evitar duplicaciones.

Como mínimo habrá que verificar cobertura de:

- Next.js/React architecture;
- TypeScript;
- state/data fetching;
- forms y validation;
- accessibility;
- testing;
- design system;
- progressive disclosure / quick capture;
- seguridad frontend/OIDC.

## Orden sugerido para crear skills

1. `ddd-hexagonal-architecture`
2. `mongodb-document-modeling`
3. `mongodb-dotnet-driver`
4. `event-driven-outbox-inbox`
5. `rabbitmq-dotnet`
6. `multitenancy-authorization`
7. `oidc-keycloak-aspnetcore`
8. `cqrs-read-models-projections`
9. revisar/agregar set React/Next.js

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

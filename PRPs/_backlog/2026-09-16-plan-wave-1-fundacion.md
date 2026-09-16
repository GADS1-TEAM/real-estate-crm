# Plan de ejecución — Wave 1 (Fundación) · CRM Inmobiliario V2

> **Para:** Claude Sonnet ejecutando en **Claude Code** sobre la PC del desarrollador.
> **Alcance:** `V2-FND-001` → `V2-FND-002` + `V2-FND-003`. Nada más.
> **Fecha:** 2026-09-16 · **Estado:** borrador, requiere Paso 0 aprobado.

---

## Paso 0 — Gate humano (Sonnet NO arranca hasta que esto esté cerrado)

El grupo, no el agente, confirma por escrito (comentario en el PR o mensaje en el canal del equipo):

- [ ] **PR de rescate mergeado:** `chore/rescate-skills-agentes-neutrales` está en `main`.
- [ ] **V2-SCP-001 cerrado:** se acepta el alcance de `docs/adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md` y del plan maestro.
- [ ] **Precedencia de tenancy aclarada:** `ADR-001` gana sobre las menciones a `tenantId` que siguen en `AGENTS.md`, `ARCHITECTURE.md` y `docs/implementation/POC_TECH_DECISIONS.md`. **Sin tenantId, sin organizationId.**
- [ ] **Estructura de servicios confirmada:** se mantienen los 11 servicios + 2 BFF que lista `V2-FND-001` (o el grupo abre un ADR para cambiarlo antes de arrancar).

Si alguno no está tildado: **frenar**.

---

## 1. Entorno requerido (en la PC, no en Cowork)

| Herramienta | Uso | Verificación |
|---|---|---|
| .NET 10 SDK | solution, servicios, BFF, tests | `dotnet --version` → 10.x |
| Node 22 + npm | `apps/crm-web`, `apps/platform-admin-web` | `node -v` |
| Docker Desktop | Compose de FND-003 | `docker compose version` |
| git + acceso al remoto | ramas y PRs | `git fetch origin` |
| Claude Code | ejecución del agente | `/model sonnet` |

Si falta algo: reportar qué falta y frenar. No simular builds.

---

## 2. Orden de lectura de contexto (en este orden, nada más)

1. `docs/adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md`
2. `docs/implementation/IMPLEMENTATION_MASTER_PLAN.md`
3. `docs/tasks/v2/README.md` y `docs/tasks/v2/TASK_BOARD.md`
4. **El archivo de la task asignada** en `docs/tasks/v2/wave-1-foundation/`
5. `AGENTS.md` — leer aplicando el override de tenancy del Paso 0
6. `docs/implementation/POC_TECH_DECISIONS.md` — mismo override
7. `ARCHITECTURE.md` — **solo** §2, §5, §7, §8, §13 y §18

**No leer** `docs/tasks/wave-*` (backlog histórico), `README.md` completo (5.500 líneas), ni `design_handoff_*` salvo que la task lo pida.

**Precedencia ante contradicción:** ADR-001 > plan maestro > task V2 > AGENTS.md > POC_TECH_DECISIONS > ARCHITECTURE > skills > código existente.

---

## 3. Guardrails globales (no negociables)

- Sin `tenantId`, `organizationId`, sucursales ni filtros por organización.
- Sin aggregate, colección ni entidad `Opportunity`.
- Sin agenda, Task, workflow, notificaciones, envío de email/WhatsApp, portales, integraciones, alquileres, pagos, comisiones, mantenimiento.
- Sin Redis/Valkey, OpenSearch, Kafka, Kubernetes, MinIO ni otra base de datos.
- No crear servicios fuera de la lista de `V2-FND-001`.
- No modificar: `README.md`, `ARCHITECTURE.md`, `AGENTS.md`, `docs/adr/`, `docs/implementation/`, `docs/tasks/wave-*`, `design_handoff_*`.
- **`apps/crm-web` y `apps/platform-admin-web` YA EXISTEN** (V2-UX-001 DONE). No regenerarlas, no reemplazarlas, no subir versión de Next.js (hoy 15.5.x). Solo integrarlas a los comandos de build documentados.
- Un PR = una task. Nada fuera de la write zone de la task.

---

## 4. Política de skills

**Se pueden aplicar** (verificadas neutrales):
`dotnet-async-and-concurrency`, `dotnet-performance-and-memory`, `dotnet-thread-safety-and-shared-state`, `dotnet-unit-testing`, `dotnet-adversarial-testing`, `dotnet-code-documentation-xmldoc`, `prp-feature-discovery-dotnet`, `common-repo-documentation`, `common-mermaid-diagrams`.

**NO aplicar como reglas** (contienen decisiones no tomadas por la V2):
`aspnetcore-rest-layer`, `aspnetcore-security-owasp-baseline`, `aspnetcore-error-and-observability`, `aspnetcore-di-and-middleware-pipeline`, `aspnetcore-outgoing-http`, `aspnetcore-config-and-secrets`, `aspnetcore-messaging`, `aspnetcore-microservice-orchestrator`, `aspnetcore-database-access-efcore`, y cualquier skill de MongoDB, RabbitMQ, outbox, CQRS, Keycloak, Next.js o multitenancy que aparezca.

Si la task necesita una de esas áreas: resolver con la **opción más simple compatible con la task V2**, documentarla en el PR como "decisión local", y si es una de las decisiones de la sección 5, **frenar y preguntar**.

**No usar `/dev`**: depende de subagentes que todavía no están en el repo. Ejecutar directo.

---

## 5. Decisiones que Sonnet NO toma (frenar y preguntar)

| Decisión | Aparece en |
|---|---|
| Tipo y representación de IDs públicos (Guid/string/ObjectId, serialización) | FND-002 |
| Outbox: colección separada con transacción (requiere Mongo replica set) vs. embebido en el aggregate | FND-002 / FND-003 |
| Mongo standalone vs. replica set en Compose | FND-003 |
| Topología RabbitMQ (exchanges, routing keys, colas, DLQ) | FND-002 / FND-003 |
| Dónde viven los tokens OIDC (cookie en BFF vs. front) y flujo de login | FND-002 |
| Catálogo de excepciones y `code` de error más allá de "ProblemDetails v1 estable" | FND-002 |
| Fijar versiones exactas de driver Mongo / cliente RabbitMQ / Keycloak | FND-002 / FND-003 |
| Monolito modular vs. 11 deployables | FND-001 (ya resuelto en Paso 0) |

Formato de la consulta: opciones (2–3), pros/contras en una línea, recomendación. Esperar respuesta.

**Sí puede decidir** (y documentar en el PR): nombres de proyectos/carpetas dentro de la write zone, librería de test de arquitectura, estructura de `Directory.Build.props`, scripts de comandos locales.

---

## 6. Ciclo por task (igual para las tres)

1. `git checkout main && git pull`
2. Rama: `feat/v2-fnd-00X-<slug>`
3. Leer contexto (sección 2).
4. **Antes de escribir código**, presentar en ≤10 líneas: qué toca y qué no, write zone, contratos que consume/produce, skills aplicadas, criterios de aceptación como tests. **Esperar OK del humano.**
5. Implementar solo la write zone.
6. Ejecutar build/tests/lint reales y guardar la salida.
7. Escribir `docs/tasks/v2/wave-1-foundation/IMPLEMENTATION_REPORT-V2-FND-00X.md` con: UCs cubiertos, archivos, comandos y resultados, decisiones locales, supuestos, follow-ups.
8. Agregar al final del archivo de la task una sección **Handoff** (qué quedó, qué falta, cómo verificar) para que otra IA pueda continuar.
9. Commit(s) Conventional Commits, push, PR. En el PR: task, UCs, evidencia, decisiones locales.
10. **No** marcar la task DONE en el board: lo hace un humano al mergear.

---

## 7. Tasks

### 7.1 V2-FND-001 — Esqueleto (SOLO, sin paralelo)

**Write zone:** solution, `services/`, `bffs/`, `contracts/`, `building-blocks/`, `tests/` (arquitectura). En `apps/` solo scripts/README de build.

**Entregables:**
- Solution .NET 10 que compila vacía.
- Boundaries: `access-service`, `party-service`, `property-service`, `supply-service`, `demand-service`, `matching-service`, `commercial-service`, `activity-service`, `analytics-service`, `platform-config-service`, `automation-ai-service`; `operations-bff`, `platform-admin-bff`.
- Capas hexagonales por servicio (dominio sin referencias a infraestructura).
- `contracts/` y `building-blocks/` vacíos pero compilables.
- Test de arquitectura que **falla** si: un proyecto de dominio referencia MongoDB/RabbitMQ/Keycloak/ASP.NET, o un servicio referencia otro servicio.
- Comando documentado que instala y construye `crm-web` y `platform-admin-web` **sin modificar su código**.
- README de desarrollo con comandos de install/build/test.

**Aceptación (de la task):** `dotnet build` OK · build de ambas webs OK · test de dependencias en verde y probado en rojo con una referencia prohibida · sin `tenantId`/`organizationId` ni carpetas de módulos excluidos.

**Evidencia:** salida de `dotnet build`, build de webs, árbol de proyectos, test de arquitectura.

### 7.2 V2-FND-002 — Contratos, auth y persistencia (paralelo con 7.3)

**Depende de:** FND-001 mergeado. **Write zone:** `contracts/`, `building-blocks/`, adapters de auth/persistencia/mensajería, tests contractuales.

**Entregables (según la task):** ProblemDetails v1, `Page<T>` v1, `ExecutionContext` v1 **sin tenant**; `EventEnvelope` v1 (`eventId, name, version, occurredAt, actorId, correlationId, causationId, aggregateId, payload`); ports `IAuthenticationPort`, `IRepository<T>`, `IUnitOfWork`, `IOutbox`, `IInbox`, `IEventPublisher`, `IEventConsumer`; adapter Keycloak (userId, displayName, email, roles); fake auth para unit tests.

**Frenar en:** IDs, estrategia de outbox, topología RabbitMQ, manejo de tokens, catálogo de errores (sección 5).

**Aceptación:** contrato incompatible rompe contract test · evento serializa/deserializa con actor y correlación · sin filtro tenant en repositorios · segundo consumo del mismo `eventId` no repite efecto · token inválido → ProblemDetails estable.

### 7.3 V2-FND-003 — Compose, CI y observabilidad (paralelo con 7.2)

**Depende de:** FND-001 mergeado. **Write zone:** `docker-compose*`, `infra/`, `.github/workflows/`, config de observabilidad, scripts de test. **No tocar** `.github/skills` ni `.github/agents`.

**Entregables:** Compose con mongo, rabbitmq (management) y keycloak con realm local de desarrollo y secretos ficticios; `.env.example`; `/health/live` y `/health/ready`; workflow CI que corre build/test/lint de .NET y de ambas webs y falla ante un test roto; logs estructurados + OpenTelemetry con `correlationId`/`actorId`; test de sanitización (sin password, token, documento ni texto de actividad en logs).

**Coordinación con 7.2:** si FND-002 todavía no definió el envelope/ExecutionContext, usar un stub mínimo y marcarlo como follow-up. **Frenar en:** replica set vs. standalone, versiones fijas.

**Aceptación:** `docker compose up` desde cero · health diferencia vivo de listo · CI local = CI remoto · `correlationId` de BFF a consumer · escaneo de logs limpio.

---

## 8. Condiciones de frenado (cualquier task)

Frenar, reportar y esperar si:
- aparece `tenantId`, `organizationId` u `Opportunity` como necesidad;
- hace falta tocar un archivo fuera de la write zone o de la lista prohibida (sección 3);
- surge una decisión de la sección 5;
- falta una herramienta del entorno;
- un build/test no se puede ejecutar de verdad;
- la task exige cambiar ownership, contrato público o una invariante (eso es ADR, y lo abre un humano).

---

## 9. Prompt para pegar en Claude Code (Sonnet)

```text
Vas a ejecutar UNA task del CRM inmobiliario V2: <V2-FND-00X>.

Leé primero PRPs/_backlog/2026-09-16-plan-wave-1-fundacion.md y seguilo al pie de la letra:
- verificá el Paso 0 y el entorno (secciones 0 y 1); si algo falta, frená;
- cargá el contexto en el orden de la sección 2 y nada más;
- respetá guardrails (3), política de skills (4) y decisiones prohibidas (5);
- seguí el ciclo de la sección 6: antes de escribir código mostrame tu resumen y esperá mi OK.

No uses /dev. No marques la task como DONE. Ante cualquier condición de la sección 8, frená y preguntame.
```

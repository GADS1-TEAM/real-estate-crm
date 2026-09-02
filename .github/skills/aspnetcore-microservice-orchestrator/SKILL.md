---
name: aspnetcore-microservice-orchestrator
description: |
  Meta-skill SIEMPRE ACTIVO al trabajar en un microservicio ASP.NET Core. No define
  reglas propias: indexa los demás skills del set .NET, decide cuáles se activan para
  una tarea, en qué orden, y cómo resolver conflictos entre ellos. Contexto:
  microservicios ASP.NET Core 10 de este repositorio; el stack obligatorio lo define
  AGENTS.md.
  Triggers: cualquier tarea de feature, fix, refactor o test en un microservicio
  .NET/ASP.NET Core; también cuando el agente arranca conversación nueva sin saber
  qué skills aplican. Palabras clave: "microservicio ASP.NET Core", "arquitectura
  del servicio .NET", "qué skill aplica", "conflicto entre reglas",
  "implementá", "agregá", "refactor", "armemos", "test", "fix", "diseñemos",
  "necesito", "endpoint", "consumer", "producer", "Controller", "IHttpClientFactory",
  "IOptions", "Minimal API", "ASP.NET Core".
  Garantiza que el agente: (1) corre el discovery (T-1) antes de escribir código
  no trivial; (2) activa los skills de borde correctos según las capas que toca;
  (3) aplica siempre los skills transversales; (4) resuelve conflictos con la
  jerarquía: seguridad > correctitud > performance > ergonomía. NO desactivar nunca
  dentro de un microservicio ASP.NET Core; sí es irrelevante en scripts CLI offline
  o tooling de build sin entrada de red.
---

# ASP.NET Core Microservice Orchestrator (meta)

## Objetivo

Este skill es el **índice y árbitro** del set .NET. Cuando llega un pedido, define
qué skills se activan, en qué orden se aplican, y cómo se resuelven los conflictos
entre ellos. No agrega reglas nuevas: orquesta las existentes.

Resuelve el problema de "tengo 16 skills, ¿cuáles aplican acá?" sin obligar al
dev a recordarlas todas.

Contexto de arquitectura: microservicios ASP.NET Core 10 sobre el arquetipo
`epa-net-paas`. Los skills cubren la superficie completa del arquetipo: capa REST,
DI y pipeline, concurrencia, thread safety, performance, validación, HTTP saliente,
acceso a datos con EF Core, mensajería, observabilidad, configuración, seguridad OWASP,
testing unitario y testing adversarial.

## Cuándo activar

Siempre que se trabaje sobre un microservicio ASP.NET Core bajo `epa-net-paas`:

- Implementar feature, fix, refactor, agregar tests, hacer review.
- Diseñar un endpoint REST, un service, un repository, un consumer/producer de mensajería.
- Modificar configuración, seguridad, o el bootstrap de la aplicación (`Program.cs`).

## Cuándo NO activar

- Scripts CLI puramente offline (sin red, sin DB, sin input no confiable).
- Tooling de build / generación de docs / migraciones one-off.
- Cambios cosméticos (typos, formato) que no tocan lógica.

## Inventario de skills del set

| ID   | Skill                                                                                        | Capa que cubre                                                              |
| ---- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| T-1  | [`prp-feature-discovery-dotnet`](../prp-feature-discovery-dotnet/SKILL.md)                   | Discovery + generación del PRP antes de implementar                         |
| T-2  | [`ddd-hexagonal-architecture`](../ddd-hexagonal-architecture/SKILL.md)                       | Dónde vive cada cosa: aggregates, capas, ownership, eventos                 |
| N-1  | [`aspnetcore-rest-layer`](../aspnetcore-rest-layer/SKILL.md)                                 | Controllers, endpoints, IResponseBuilder, formato meta-data-error           |
| N-2  | [`aspnetcore-di-and-middleware-pipeline`](../aspnetcore-di-and-middleware-pipeline/SKILL.md) | DI, lifetimes, pipeline de middleware, AddPaaS/UsePaas                      |
| N-3  | [`dotnet-async-and-concurrency`](../dotnet-async-and-concurrency/SKILL.md)                   | async/await, CancellationToken, Task.WhenAll, SemaphoreSlim                 |
| N-4  | [`dotnet-thread-safety-and-shared-state`](../dotnet-thread-safety-and-shared-state/SKILL.md) | Estado compartido, ConcurrentDictionary, IHttpContextAccessor en singletons |
| N-5  | [`dotnet-performance-and-memory`](../dotnet-performance-and-memory/SKILL.md)                 | Span/Memory, ArrayPool, boxing, GC de .NET                                  |
| N-6  | [`dotnet-parsing-and-validation`](../dotnet-parsing-and-validation/SKILL.md)                 | Validación en bordes, DataAnnotations, FluentValidation                     |
| N-7  | [`aspnetcore-outgoing-http`](../aspnetcore-outgoing-http/SKILL.md)                           | IHttpClientFactory, named clients EPA, Polly, APIM                          |
| N-8  | [`mongodb-document-modeling`](../mongodb-document-modeling/SKILL.md)                         | Documento vs aggregate, embed/reference, índices, concurrencia optimista    |
| N-8b | [`mongodb-dotnet-driver`](../mongodb-dotnet-driver/SKILL.md)                                 | Driver 3.11.1, cliente singleton, serialización, LINQ3, Testcontainers      |
| N-9  | [`aspnetcore-messaging`](../aspnetcore-messaging/SKILL.md)                                   | Mensajería agnóstica, idempotencia, DLQ. Broker del proyecto: RabbitMQ      |
| N-9b | [`event-driven-outbox-inbox`](../event-driven-outbox-inbox/SKILL.md)                         | Outbox transaccional, relay, inbox, idempotencia, versionado de eventos     |
| N-10 | [`aspnetcore-error-and-observability`](../aspnetcore-error-and-observability/SKILL.md)       | Tipos de log EPA, MASKED_DATA, Jaeger OTLP, excepciones tipadas             |
| N-11 | [`aspnetcore-config-and-secrets`](../aspnetcore-config-and-secrets/SKILL.md)                 | IOptions, appsettings, env vars EPA, secretos                               |
| N-12 | [`aspnetcore-security-owasp-baseline`](../aspnetcore-security-owasp-baseline/SKILL.md)       | OWASP Top 10 — transversal, siempre activo                                  |
| N-13 | [`dotnet-unit-testing`](../dotnet-unit-testing/SKILL.md)                                     | xUnit, Moq, WebApplicationFactory                                           |
| N-14 | [`dotnet-adversarial-testing`](../dotnet-adversarial-testing/SKILL.md)                       | Inputs hostiles, fallas de dependencias                                     |
| N-15 | [`dotnet-code-documentation-xmldoc`](../dotnet-code-documentation-xmldoc/SKILL.md)           | Documentación XML doc de API pública                                        |
| N-0  | (este skill)                                                                                 | Meta-skill: indexa y arbitra los demás                                      |

## Skills transversales (siempre activos)

Los siguientes skills aplican a casi toda tarea que toca código de red o de negocio:

| Skill                                       | Razón                                                                                                                  |
| ------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| N-4 `dotnet-thread-safety-and-shared-state` | Todo servicio registrado como Singleton puede ser accedido concurrentemente; `IHttpContextAccessor` es un caso crítico |
| N-10 `aspnetcore-error-and-observability`   | Todo código de negocio necesita logging estructurado según tipos EPA, MASKED_DATA y trazas OTLP                        |
| N-11 `aspnetcore-config-and-secrets`        | Toda configuración de recursos debe estar externalizada con IOptions, validada y sin hardcodear secretos               |
| N-12 `aspnetcore-security-owasp-baseline`   | Todo endpoint expuesto a la red es superficie de ataque                                                                |

## Skills de borde (se activan según la capa que toca la tarea)

| Si la tarea toca…                                           | Activar        |
| ----------------------------------------------------------- | -------------- |
| Endpoint REST (crear/modificar un Controller o Minimal API) | N-1, N-6, N-12 |
| DI / lifetimes / pipeline de middleware / `Program.cs`      | N-2            |
| Operaciones async, fan-out o cancelación                    | N-3, N-4       |
| Performance / GC / `Span<T>` / `ArrayPool`                  | N-5            |
| Validación de input externo                                 | N-6            |
| Llamadas HTTP salientes a otros servicios o APIM            | N-7, N-3       |
| Acceso a base de datos con EF Core                          | N-8            |
| Mensajes (producer o consumer)                              | N-9, N-6       |
| Logging, errores, trazas, métricas                          | N-10           |
| Configuración externalizada o secretos                      | N-11           |
| Tests unitarios                                             | N-13           |
| Tests adversariales                                         | N-14           |

## Decisiones del proyecto

- **Discovery (T-1) es el paso 0** para todo trabajo no trivial. Sin PRP aprobado
  en `_in-progress/`, no se escribe código de feature.
- Los **skills transversales** (N-4, N-10, N-11, N-12) se consideran activos por
  defecto en cualquier tarea de código de red.
- El **orchestrator NO define reglas propias.** Si una guideline no existe en
  ningún skill del set, no se inventa acá: se agrega al skill correspondiente.
- La **jerarquía de conflictos** resuelve casos donde dos skills empujan en
  direcciones opuestas.

## Reglas obligatorias

### MUST

1. **MUST correr T-1 (prp-feature-discovery-dotnet) primero** ante un pedido no
   trivial. Sin PRP en `_in-progress/`, no avanzar con código de feature.

2. **MUST activar siempre los skills transversales** al escribir código de red:
   N-4, N-10, N-11, N-12. No son opcionales.

3. **MUST mapear capas → skills antes de implementar.** Para cada archivo o
   clase a crear, identificar qué capa toca y activar el skill de borde:
   - Controller o Minimal API endpoint nuevo → N-1, N-6, N-12.
   - Llamada HTTP saliente → N-7.
   - Query a DB con EF Core → N-8.
   - Consumer/producer de mensajería → N-9, N-6.
   - Operación async → N-3, N-4.
   - Servicio con DI / middleware → N-2.

4. **MUST aplicar la jerarquía de conflictos** cuando dos skills empujan en
   direcciones distintas:
   - **Seguridad (N-12) gana siempre.** Ninguna optimización justifica romper
     una regla MUST de security-baseline.
   - **Correctitud > performance.** Si una optimización (N-5, N-3) compromete
     correctitud (N-6, N-4), gana correctitud.
   - **Idempotencia > throughput.** N-9 dice "consumer idempotente siempre"; N-3
     puede sugerir paralelizar, pero solo si la idempotencia está garantizada.

5. **MUST documentar qué skills se activaron** en el plan de implementación del
   PRP para que el evaluator sepa qué reglas verificar.

### MUST NOT

6. **MUST NOT desactivar N-12 (security-baseline)** para simplificar una feature.
   Si una regla de seguridad bloquea el diseño, se cambia el diseño.

7. **MUST NOT aplicar N-5 (performance) preventivamente** sin evidencia de
   bottleneck. La regla de "evidencia primero" de N-5 prevalece.

8. **MUST NOT mezclar acceso a DB (N-8) con lógica de HTTP saliente (N-7) en el
   mismo método sin una capa de service que orqueste.** Mantener separación de
   concerns: repository solo DB, client solo HTTP, service orquesta.

## Checklist de activación por tarea

Antes de implementar, completar mentalmente o en el PRP:

- [ ] ¿Hay PRP aprobado? → T-1 completado.
- [ ] ¿Toca REST? → N-1 + N-6 + N-12.
- [ ] ¿Toca DI/pipeline? → N-2.
- [ ] ¿Toca async/concurrencia? → N-3 + N-4.
- [ ] ¿Toca performance/memoria? → N-5 solo con evidencia.
- [ ] ¿Toca parsing/validación? → N-6.
- [ ] ¿Toca HTTP saliente? → N-7 + N-3.
- [ ] ¿Toca EF Core? → N-8.
- [ ] ¿Toca mensajería? → N-9.
- [ ] N-10, N-11, N-12 siempre activos en código de red.
- [ ] ¿Requiere tests? → N-13 y N-14 según riesgo.
- [ ] ¿Agrega/modifica API pública? → N-15.

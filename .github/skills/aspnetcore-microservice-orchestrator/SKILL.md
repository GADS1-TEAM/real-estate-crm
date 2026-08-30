---
name: aspnetcore-microservice-orchestrator
description: |
  Meta-skill SIEMPRE ACTIVO al trabajar en un microservicio ASP.NET Core de Banco
  . No define reglas propias: indexa los demás skills del set .NET, decide
  cuáles se activan para una tarea, en qué orden, y cómo resolver conflictos entre
  ellos. Contexto: microservicios ASP.NET Core 8 con arquetipo epa-net-paas.
  Triggers: cualquier tarea de feature, fix, refactor o test en un microservicio
  .NET/ASP.NET Core; también cuando el agente arranca conversación nueva sin saber
  qué skills aplican. Palabras clave: "microservicio ASP.NET Core", "arquitectura
  del servicio .NET", "qué skill aplica", "epa-net-paas", "conflicto entre reglas",
  "implementá", "agregá", "refactor", "armemos", "test", "fix", "diseñemos",
  "necesito", "endpoint", "consumer", "producer", "Controller", "IHttpClientFactory",
  "EF Core", "DbContext", "IOptions", "Minimal API", "ASP.NET Core".
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

Contexto de arquitectura: microservicios ASP.NET Core 8 sobre el arquetipo
`epa-net-paas` de . Los skills cubren la superficie completa del
arquetipo: capa REST, DI y pipeline, concurrencia, thread safety, performance,
validación, HTTP saliente, acceso a datos con EF Core, mensajería, observabilidad,
configuración, seguridad OWASP, testing unitario y testing adversarial.

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
| N-1  | [`aspnetcore-rest-layer`](../aspnetcore-rest-layer/SKILL.md)                                 | Controllers, endpoints, IResponseBuilder, formato meta-data-error           |
| N-2  | [`aspnetcore-di-and-middleware-pipeline`](../aspnetcore-di-and-middleware-pipeline/SKILL.md) | DI, lifetimes, pipeline de middleware, AddPaaS/UsePaas                      |
| N-3  | [`dotnet-async-and-concurrency`](../dotnet-async-and-concurrency/SKILL.md)                   | async/await, CancellationToken, Task.WhenAll, SemaphoreSlim                 |
| N-4  | [`dotnet-thread-safety-and-shared-state`](../dotnet-thread-safety-and-shared-state/SKILL.md) | Estado compartido, ConcurrentDictionary, IHttpContextAccessor en singletons |
| N-5  | [`dotnet-performance-and-memory`](../dotnet-performance-and-memory/SKILL.md)                 | Span/Memory, ArrayPool, boxing, GC de .NET                                  |
| N-6  | [`dotnet-parsing-and-validation`](../dotnet-parsing-and-validation/SKILL.md)                 | Validación en bordes, DataAnnotations, FluentValidation                     |
| N-7  | [`aspnetcore-outgoing-http`](../aspnetcore-outgoing-http/SKILL.md)                           | IHttpClientFactory, named clients EPA, Polly, APIM                          |
| N-8  | [`aspnetcore-database-access-efcore`](../aspnetcore-database-access-efcore/SKILL.md)         | EF Core 8, DbContext scoped, N+1, AsNoTracking                              |
| N-9  | [`aspnetcore-messaging`](../aspnetcore-messaging/SKILL.md)                                   | Mensajería (stack no confirmado), idempotencia, DLQ                         |
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
     no puede sacrificarlo por performance.
   - **Validación > flexibilidad.** N-6 gana sobre "aceptemos lo que venga".

5. **MUST cerrar con tests** (N-13 unit + N-14 adversarial) toda feature antes
   de pedir review.

6. **MUST registrar en el PRP qué skills aplicaron.** La sección "Skills
   aplicables" del PRP debe completarse con la lista real, no genérica.

7. **MUST, al modificar funciones o código existente, descubrir la
   documentación relacionada antes de entregar** — README de módulo/subcarpeta,
   `docs/testing.md`, XML doc comments de lo tocado, diagramas Mermaid — y
   actualizarla en el mismo cambio si el comportamiento documentado cambió. No
   asumir que "ya está bien": buscarla explícitamente (`common-repo-documentation`,
   `common-mermaid-diagrams`, `dotnet-code-documentation-xmldoc` (N-15)).

8. **MUST, al agregar, modificar o eliminar tests, ubicar y actualizar
   `docs/testing.md`** (o la sección de testing del README si el paquete no
   tiene carpeta `docs/`) en el mismo cambio: conteo de tests, casos cubiertos
   y no cubiertos.

### MUST NOT

9. **MUST NOT escribir código de implementación sin haber pasado por T-1**
   para tareas no triviales.

10. **MUST NOT desactivar skills transversales** por "es un caso simple".
    Seguridad y observabilidad aplican siempre en código de red.

11. **MUST NOT inventar nuevas reglas en este skill.** Si una guideline no
    existe en ningún skill del set, no se inventa acá.

## Comparación con el set Java

| ID .NET | Skill .NET                              | ID Java | Skill Java                             | Diferencia clave                                                                                                               |
| ------- | --------------------------------------- | ------- | -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| T-1     | `prp-feature-discovery-dotnet`          | T-1     | `prp-feature-discovery-java`           | Mismo flujo; template del PRP adaptado al arquetipo epa-net-paas vs stack Spring                                        |
| N-1     | `aspnetcore-rest-layer`                 | J-1     | `springboot-rest-layer`                | IResponseBuilder + formato meta-data-error vs `@ControllerAdvice` + `ResponseEntity`; Minimal APIs vs `@RestController`        |
| N-2     | `aspnetcore-di-and-middleware-pipeline` | J-2     | `springboot-di-and-bean-lifecycle`     | `AddScoped/Singleton/Transient` + `Program.cs` vs `@Component/@Bean` + `@PostConstruct`; `AddPaaS/UsePaas` no existe en Spring |
| N-3     | `dotnet-async-and-concurrency`          | J-3     | `java-async-and-concurrency`           | `async/await` + `CancellationToken` vs `CompletableFuture/@Async`; .NET no necesita `TaskExecutor` explícito                   |
| N-4     | `dotnet-thread-safety-and-shared-state` | J-4     | `java-thread-safety-and-shared-state`  | `IHttpContextAccessor` en singletons es el riesgo .NET más frecuente; Java tiene `ThreadLocal`                                 |
| N-5     | `dotnet-performance-and-memory`         | J-5     | `java-jvm-performance-and-gc`          | `Span<T>/Memory<T>/ArrayPool` vs virtual threads y G1GC; GC .NET y GC JVM tienen perfiles distintos                            |
| N-6     | `dotnet-parsing-and-validation`         | J-6     | `java-parsing-and-validation`          | FluentValidation / DataAnnotations vs Bean Validation (JSR-380); ambos validan en el borde                                     |
| N-7     | `aspnetcore-outgoing-http`              | J-7     | `springboot-outgoing-http`             | `IHttpClientFactory` + named clients EPA vs `WebClient/RestClient`; Polly es equivalente a Resilience4j                        |
| N-8     | `aspnetcore-database-access-efcore`     | J-8     | `springboot-database-access-jpa`       | EF Core 8 + `AsNoTracking` vs JPA/Spring Data; `DbContext` scoped vs `EntityManager` gestionado por Spring                     |
| N-9     | `aspnetcore-messaging`                  | J-9     | `springboot-messaging-kafka`           | Stack de mensajería .NET no confirmado; Java usa Kafka + Avro + Confluent Schema Registry                                      |
| N-10    | `aspnetcore-error-and-observability`    | J-10    | `springboot-error-and-observability`   | Tipos de log EPA + Jaeger OTLP vs SLF4J/Logback + MDC + Micrometer; misma semántica de observabilidad                          |
| N-11    | `aspnetcore-config-and-secrets`         | J-11    | `springboot-config-and-secrets`        | `IOptions<T>` + `appsettings.json` + env vars EPA vs `@ConfigurationProperties` + Vault; similar patrón fail-fast              |
| N-12    | `aspnetcore-security-owasp-baseline`    | J-12    | `springboot-security-owasp-baseline`   | ASP.NET Core auth/authz middleware vs Spring Security + OAuth2 + `@PreAuthorize`; misma cobertura OWASP Top 10                 |
| N-13    | `dotnet-unit-testing`                   | J-13    | `springboot-unit-testing`              | xUnit + Moq + `WebApplicationFactory` vs JUnit 5 + Mockito + `@WebMvcTest/@DataJpaTest`; ambos tienen slices de test           |
| N-14    | `dotnet-adversarial-testing`            | J-14    | `java-adversarial-testing`             | Mismos principios; librerías distintas (FsCheck/AutoFixture vs jqwik/Hypothesis)                                               |
| N-0     | `aspnetcore-microservice-orchestrator`  | J-0     | `springboot-microservice-orchestrator` | Mismo rol de meta-skill; arquetipo epa-net-paas vs stack Spring del banco                                                      |

## Checklist antes de terminar

- [ ] T-1 (PRP) corrió y está aprobado en `_in-progress/`.
- [ ] Los skills transversales (N-4, N-10, N-11, N-12) se aplicaron.
- [ ] Cada capa tocada tiene su skill de borde activado.
- [ ] El PRP registra qué skills aplicaron.
- [ ] Tests unitarios (N-13) y adversariales (N-14) están completos.
- [ ] Documentación sincronizada: si cambió comportamiento, `docs/testing.md`/README de
      testing refleja el conteo y casos actuales; XML doc comments de lo tocado están
      al día; READMEs de módulo y diagramas Mermaid impactados fueron revisados y
      actualizados.

## Conexiones con otros skills

- **Upstream:** T-1 (`prp-feature-discovery-dotnet`) — debe correr antes de que
  este orchestrator active los skills de implementación.
- **Transversales siempre activos:** N-4, N-10, N-11, N-12.
- **Skills de borde por capa:** N-1 a N-9, N-13, N-14 según la tabla de la sección
  "Skills de borde".
- **Referencia cruzada:** `springboot-microservice-orchestrator` (J-0) — equivalente
  para microservicios Java; la tabla de comparación de este skill es la fuente de
  verdad del mapeo .NET ↔ Java.

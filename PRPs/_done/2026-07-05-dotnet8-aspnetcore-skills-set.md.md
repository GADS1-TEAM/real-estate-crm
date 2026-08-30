# Set de skills .NET 8 / ASP.NET Core para microservicios

## Metadata

| Campo                       | Valor                                                   |
| --------------------------- | ------------------------------------------------------- |
| Fecha                       | 2026-07-05                                              |
| Autor                       | Copilot                                                 |
| Estado                      | done                                                    |
| Última actualización        | 2026-08-06                                              |
| Refleja implementación real | SÍ                                                      |
| Ticket                      | N/A                                                     |
| PRPs relacionados           | N/A                                                     |
| Skills aplicables           | local-customization-orchestrator, local-skill-authoring |

---

## Contexto y problema

`starter-ai` tiene sets para BFF (Node.js/NestJS) y Java (Spring Boot).
Los microservicios .NET del banco usan ASP.NET Core 8 con el arquetipo interno
`epa-net-paas` (NuGet en Nexus de ). Sin skills específicos para este stack,
el agente no tiene guías concretas sobre: el arquetipo EPA y su pipeline
AddPaaS/UsePaas, los tipos de log propios (ISSUE_LOG, APPLICATION_DEFAULT, REQUEST,
RESPONSE, OUTGOING_RESPONSE), el formato de respuesta meta-data-error, las
excepciones tipadas del arquetipo, named clients con credenciales APIM, tracing con
Jaeger OTLP/gRPC, ni las convenciones de PaasControllerBase y IResponseBuilder.

## Objetivo

Crear un set de 17 skills para microservicios ASP.NET Core 8 con el arquetipo
`epa-net-paas` que siga la estructura y densidad de reglas de los sets BFF y Java.

## Scope

### Entra

- Crear `skills/dotnet/` con 17 subdirectorios y sus SKILL.md.
- Un orchestrator meta-skill que indexe los 16 skills técnicos + discovery.
- Registrar el preset `dotnet` en `presets.config.mjs`.
- Actualizar `README.md` con la sección del preset dotnet.

### NO entra

- Modificar skills de BFF, React, React Toolkit ni Java.
- Crear agents para .NET (PRP separado).
- Cubrir .NET MAUI, Blazor, WinForms.
- Migrar repos .NET existentes.

## Inventario de skills

| ID   | Skill .NET                            | Equivalente Java                     |
| ---- | ------------------------------------- | ------------------------------------ |
| T-1  | prp-feature-discovery-dotnet          | prp-feature-discovery-java           |
| N-0  | aspnetcore-microservice-orchestrator  | springboot-microservice-orchestrator |
| N-1  | aspnetcore-rest-layer                 | springboot-rest-layer                |
| N-2  | aspnetcore-di-and-middleware-pipeline | springboot-di-and-bean-lifecycle     |
| N-3  | dotnet-async-and-concurrency          | java-async-and-concurrency           |
| N-4  | dotnet-thread-safety-and-shared-state | java-thread-safety-and-shared-state  |
| N-5  | dotnet-performance-and-memory         | java-jvm-performance-and-gc          |
| N-6  | dotnet-parsing-and-validation         | java-parsing-and-validation          |
| N-7  | aspnetcore-outgoing-http              | springboot-outgoing-http             |
| N-8  | aspnetcore-database-access-efcore     | springboot-database-access-jpa       |
| N-9  | aspnetcore-messaging                  | springboot-messaging-kafka           |
| N-10 | aspnetcore-error-and-observability    | springboot-error-and-observability   |
| N-11 | aspnetcore-config-and-secrets         | springboot-config-and-secrets        |
| N-12 | aspnetcore-security-owasp-baseline    | springboot-security-owasp-baseline   |
| N-13 | dotnet-unit-testing                   | springboot-unit-testing              |
| N-14 | dotnet-adversarial-testing            | java-adversarial-testing             |

## Decisiones de diseño

- Prefijos: `aspnetcore-*` para skills que dependen del framework; `dotnet-*` para
  skills que aplican a cualquier C#/.NET (concurrencia, performance, testing).
- `epa-net-paas` es el "target" de los skills relevantes; la sección "Estado actual
  vs target" referencia el arquetipo y sus convenciones (v1.1.8).
- N-9 (messaging) tiene mayor incertidumbre: el README del arquetipo no menciona
  mensajería; se cubren patrones genéricos + Kafka Confluent .NET.
- `IHttpContextAccessor` en singletons es el equivalente .NET del ThreadLocal mal
  usado en Java: N-4 lo cubre como anti-patrón.
- `CancellationToken` obligatorio en toda operación async que toca I/O: N-3 MUST.

## Consideraciones de seguridad

- Sin credenciales reales (APP_ID/APP_KEY, tokens) en los skills.
- N-12 cubre OWASP Top 10 en contexto .NET/C#.
- MASKED_DATA del arquetipo es la primera defensa contra PII en logs; N-10 la refuerza.

## Plan de implementación

1. Crear los 15 skills técnicos (T-1 y N-1 a N-14), excepto N-0.
2. Crear N-0 con la tabla de inventario completa.
3. Registrar el preset `dotnet` en `presets.config.mjs`.
4. Actualizar `README.md`.
5. Validar con local-customization-evaluator.

## Implementación real

Implementado en una sesión previa: los 17 skills existen en `skills/dotnet/`, el preset
`dotnet` está registrado en `presets.config.mjs`, y `README.md` tiene la sección
completa del preset dotnet. Este archivo se movió a `_done/` para reflejar el estado
real (estaba mal ubicado en `_backlog/`).

## Criterios de aceptación

- [x] 17 carpetas bajo `skills/dotnet/` con SKILL.md válido.
- [x] Cada SKILL.md tiene frontmatter con triggers específicos de .NET/C#/EPA.
- [x] Los skills N-1, N-7, N-10, N-11 referencian explícitamente `epa-net-paas`.
- [x] aspnetcore-microservice-orchestrator tiene tabla de inventario completa.
- [x] aspnetcore-security-owasp-baseline marcado como transversal siempre activo.
- [x] `presets.config.mjs` registra el preset `dotnet` con 16 skills.
- [x] `README.md` tiene la sección del preset dotnet.

## Riesgos identificados

| Riesgo                                | Prob. | Impacto | Mitigación                                     |
| ------------------------------------- | ----- | ------- | ---------------------------------------------- |
| ORM no confirmado (EF Core vs Dapper) | Alta  | Medio   | N-8 para EF Core; anotar ajuste si usan Dapper |
| Mensajería no mencionada en README    | Alta  | Bajo    | N-9 patrones genéricos + Kafka Confluent .NET  |
| Framework de testing no confirmado    | Media | Bajo    | xUnit + Moq estándar de facto                  |
| Versión del arquetipo puede cambiar   | Baja  | Medio   | Skills referencian v1.1.8 con nota de versión  |

# Decisiones técnicas cerradas para la POC

## Objetivo

La POC debe validar dominio, journeys y contratos con código robusto, pero con **costo de infraestructura cero y operación mínima**. La arquitectura lógica sigue preparada para evolucionar; la infraestructura inicial no intenta anticipar escala inexistente.

## Decisiones

| Tema | Decisión POC | Regla de evolución |
|---|---|---|
| Backend y BFF | .NET 10 / ASP.NET Core | Stack único salvo necesidad técnica explícita. |
| Front web | Next.js + React + TypeScript | `crm-web` y `platform-admin-web` separados. |
| Mobile | Diferido | No implementar ni elegir framework ahora. |
| Runtime | Docker Compose | Kubernetes/OpenShift solo cuando despliegue/escala lo justifique. |
| MongoDB | MongoDB Community, una instancia compartida | Owner exclusivo de colecciones por servicio; `tenantId` obligatorio. |
| Mensajería | RabbitMQ Community | Mantener contratos versionados para poder cambiar broker. |
| Schema registry | Ninguno | Schemas versionados como código + validación CI. |
| Auth | Keycloak Community / OIDC | Un realm de plataforma en POC; autorización de negocio en `access-service`. |
| Caché | `IMemoryCache` | Redis/Valkey solo con múltiples réplicas, locks distribuidos o métrica que lo justifique. |
| Search/geo | MongoDB indexes, `$text`, `2dsphere` | Motor dedicado como proyección futura. |
| Binarios | Filesystem local vía `IObjectStorage` | Reemplazable por MinIO/S3-compatible. |
| Observabilidad | OpenTelemetry + logs estructurados | Collector/dashboard externo solo si hace falta. |
| Analytics | Proyecciones MongoDB | Sin warehouse/columnar en POC. |
| Offline | Draft/autosave local básico | Sin commands sensibles offline. |
| Telefonía | Diferida | Puerto conceptual, sin adapter productivo. |

## Topología mínima

`docker compose up` debe poder levantar como máximo la infraestructura base siguiente:

1. MongoDB Community.
2. RabbitMQ Community con management UI.
3. Keycloak Community.
4. Los BFF/servicios .NET y frontends necesarios para el slice actual.

Los work packages no pueden agregar Redis, OpenSearch, Kafka/Redpanda, Schema Registry, MinIO, Kubernetes ni otra base de datos sin justificar un caso de uso actual y registrar la decisión.

## Multi-tenancy

- Misma instancia MongoDB.
- Mismas aplicaciones/servicios desplegados para todos los tenants.
- `tenantId` se deriva del contexto autenticado y forma parte de filtros/índices.
- Cada servicio accede únicamente a sus colecciones.
- Compartir infraestructura física no autoriza acoplamiento de datos.

## Autenticación vs autorización

Keycloak responde **quién es el usuario** y emite tokens OIDC. `access-service` responde **qué puede hacer dentro de la inmobiliaria**, considerando rol, unidad organizacional, ownership y overrides. No se codifican reglas comerciales complejas dentro de Keycloak.

## Búsqueda

No existe `search-service` desplegado inicialmente. Cada contexto resuelve sus búsquedas con MongoDB detrás de un port propio. Si matching/full-text/geo deja de cumplir SLO, se crea una proyección dedicada sin cambiar commands ni aggregates.

## Cache

`IMemoryCache` es una optimización local descartable. Ningún caso de uso depende de que exista. La introducción de caché distribuida requiere una razón observable, no una suposición de escala futura.

# AGENTS.md — CRM Inmobiliarias

## Precedencia V2 (leer primero)

Este repo implementa la **versión reducida V2**. Donde este archivo, `README.md` o `ARCHITECTURE.md` contradigan lo siguiente, **prevalece esto**:

1. `docs/adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md` (alcance).
2. El plan de la wave en curso en `PRPs/_backlog/` (el más reciente manda) y la task en `docs/tasks/v2/`.
3. Los `IMPLEMENTATION_REPORT-*.md` de tasks mergeadas (sección "Contratos publicados").

En V2 **no existe** `tenantId`/`organizationId` (una sola inmobiliaria) ni `Opportunity` como entidad: toda mención a tenant más abajo no aplica. Las skills viven en `.github/skills/<skill>/SKILL.md` y se usan solo las que habilita el plan de la wave.


## Fuentes de verdad

1. `README.md` — dominio.
2. `ARCHITECTURE.md` — arquitectura y casos de uso.
3. work package actual — alcance de implementación.

No reinterpretar decisiones globales desde código existente si contradicen esas fuentes.

## Regla principal

Implementá **solo** el work package asignado. No optimices ni refactorices áreas no relacionadas.

## Arquitectura obligatoria

- DDD + microservicios por capacidad de negocio.
- Hexagonal dentro de cada servicio.
- Una instancia MongoDB Community compartida en POC; ownership exclusivo de colecciones por servicio/contexto + `tenantId`.
- Event-driven con RabbitMQ Community + Outbox/Inbox.
- CQRS selectivo/read models.
- BFF por experiencia, implementados en .NET 10 / ASP.NET Core.
- Backend/BFF en .NET 10 / ASP.NET Core.
- Infra POC con Docker Compose.
- Keycloak Community para OIDC/OAuth2.
- `IMemoryCache` primero; sin Redis/Valkey salvo necesidad medida.
- Búsqueda MongoDB nativa en V1; sin OpenSearch.
- Sin microfrontends V1.
- Sin Event Sourcing global.
- Mobile, telefonía, Kubernetes/OpenShift y data warehouse diferidos.

## Prohibiciones

- No consultar DB de otro servicio.
- No compartir entidades de dominio vía `packages/*`.
- No inventar campos obligatorios que el dominio define opcionales.
- No convertir `UNKNOWN` en falso/cero.
- No crear microservicios nuevos sin ADR aprobado.
- No cambiar eventos públicos de forma incompatible.
- No permitir acceso directo del LLM a DB.
- No editar MongoDB desde Platform Admin.

## Antes de programar

1. Listar UCs del paquete.
2. Listar aggregates/entidades owner.
3. Listar APIs/eventos consumidos/producidos.
4. Confirmar write zones.
5. Escribir criterios/tests de aceptación.

## Definition of Done

- casos de uso del paquete cubiertos;
- tests unit/application/integration/contract según aplique;
- tenant/authorization test;
- telemetría básica;
- errores públicos estables;
- documentación de contrato actualizada;
- no hay cambios fuera de scope;
- `IMPLEMENTATION_REPORT.md` del paquete.

## UX

Simple by default, powerful by exception. Quick capture primero. Progressive disclosure. Bloquear solo cuando la acción actual requiere realmente el dato.

## Escalamiento

Abrir ADR request antes de cambiar ownership, invariante, microservicio, datastore, consistencia cross-service o contrato público.

# V2-FND-001 — Crear el esqueleto ejecutable de la POC

- **Ola:** 1 — Fundación ejecutable
- **Estado:** TODO
- **Dependencias:** V2-SCP-001
- **UCs:** FOUND-001, FOUND-002, FOUND-003, FOUND-004
- **Owner:** foundation de plataforma
- **Write zone:** solution, apps, bffs, services, contracts, building-blocks y tests de arquitectura

## Resultado esperado

El repositorio deja de ser únicamente documental y contiene una solution .NET
10, los servicios por capacidad del alcance V2, los BFF, las aplicaciones web,
contracts, building-blocks y tests mínimos. Todo compila aunque las
capacidades de negocio todavía sean shells.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| FOUND-001 | Desarrollo | Crear solution y proyectos .NET 10 con dependencias dirigidas. | Foundation | dotnet build exitoso. |
| FOUND-002 | Desarrollo | Crear crm-web y platform-admin-web como shells Next.js/React/TypeScript. | Frontend | npm/pnpm build exitoso. |
| FOUND-003 | Desarrollo | Crear boundaries para access, party, property, supply, demand, matching, commercial, activity, analytics, platform-config y automation-ai. | Foundation | Test de arquitectura. |
| FOUND-004 | Desarrollo | Documentar comandos de instalación, ejecución y test local. | Foundation | README de desarrollo. |

## Interfaces

- Consume: ADR-001, plan maestro V2 y POC_TECH_DECISIONS.md.
- Produce: proyectos con namespaces y nombres estables para V2-FND-002 y las
  tasks de dominio.
- Nombres mínimos de deployables/límites: access-service,
  party-service, property-service, supply-service, demand-service,
  matching-service, commercial-service, activity-service, analytics-service,
  platform-config-service, automation-ai-service, operations-bff,
  platform-admin-bff, crm-web y platform-admin-web.

## Reglas

- No crear un servicio por entidad ni un proyecto para capacidades fuera de
  scope.
- No agregar Organization, tenancy, rental-service, scheduling-service,
  notification-service, syndication-service o maintenance-service al slice
  V2.
- El dominio no referencia MongoDB, RabbitMQ, Keycloak ni proveedores externos.
- Los BFF no acceden a MongoDB.
- La estructura debe permitir ejecutar un vertical slice independiente sin
  compartir aggregates.

## Criterios de aceptación

- [ ] dotnet build compila la solution vacía/base.
- [ ] Las dos webs instalan y construyen con un comando documentado.
- [ ] Un test falla si un proyecto de dominio referencia infraestructura o el
  proyecto de otro servicio.
- [ ] Las rutas de desarrollo distinguen crm-web de platform-admin-web.
- [ ] No aparecen tenantId, organizationId ni carpetas de módulos excluidos en
  la estructura base.

## Overrides POC

- MongoDB, RabbitMQ y Keycloak se conectan en tasks posteriores.
- Las páginas iniciales son shells sin datos ficticios usados como source of
  truth.
- El diseño final de Claude Design queda para V2-UX-001.

## Definition of Done

- [ ] Solution y frontends compilan.
- [ ] Test de arquitectura y reglas de dependencia en verde.
- [ ] Comandos de desarrollo documentados.
- [ ] PR limitado al esqueleto.

## Evidencia requerida

1. Salida de dotnet build.
2. Salida de build del frontend.
3. Árbol de proyectos.
4. Resultado del test de dependencias.

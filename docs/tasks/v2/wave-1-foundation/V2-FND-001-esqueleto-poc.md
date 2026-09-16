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

## Handoff

**Qué quedó:** `RealEstateCrm.slnx` compila (51 proyectos: 11 servicios × 4 capas
+ 2 BFF × 2 capas + `contracts` + `building-blocks` + 1 test de arquitectura).
`dotnet build`/`dotnet test` en verde. `apps/crm-web` y `apps/platform-admin-web`
instalan y buildean con `apps/scripts/build-webs.sh`/`.ps1` sin tocar su código.
Detalle completo, comandos y decisiones locales en
`IMPLEMENTATION_REPORT-V2-FND-001.md` (esta misma carpeta).

**Qué falta:** todo lo de negocio. `contracts/` y `building-blocks/` están
vacíos (compilables, sin contenido); ningún servicio tiene entidades,
commands, queries ni adapters; ningún `Api` tiene endpoints reales ni
healthchecks; no hay Compose, CI ni conexión a MongoDB/RabbitMQ/Keycloak.

**Cómo verificar:**

```bash
dotnet build RealEstateCrm.slnx
dotnet test RealEstateCrm.slnx
bash apps/scripts/build-webs.sh
```

El test de arquitectura (`tests/RealEstateCrm.ArchitectureTests`) debe dar
2/2 en verde. Si se agrega una `ProjectReference` de un `Domain` a cualquier
otro proyecto, o de un servicio a otro servicio, el mismo test debe fallar
(se verificó manualmente durante esta task; ver reporte de implementación
para el detalle exacto de la prueba en rojo).

**Para continuar (V2-FND-002 / V2-FND-003):** los namespaces y nombres de
proyecto ya son estables (`<Servicio>.Domain/.Application/.Infrastructure/.Api`,
`<Bff>.Application/.Api`). No renombrar sin abrir ADR. `V2-FND-002` y
`V2-FND-003` dependen de este merge a `main` antes de arrancar en paralelo.

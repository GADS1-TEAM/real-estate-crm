# FND-005 — Crear crm-web, platform-admin-web y sus BFFs .NET 10

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-001`, `FND-003`

## Resultado esperado

Crear los shells de las dos experiencias web y los BFFs que aislarán a los frontends de la topología interna. Mobile queda diferido.

## Entregables

- [ ] `operations-bff` en ASP.NET Core .NET 10.
- [ ] `platform-admin-bff` en ASP.NET Core .NET 10.
- [ ] shell de `crm-web` Next.js/React/TypeScript.
- [ ] shell de `platform-admin-web` Next.js/React/TypeScript.
- [ ] login OIDC contra Keycloak.
- [ ] navegación, manejo global de errores y cliente HTTP base.
- [ ] primitives mínimos orientados a quick capture/progressive disclosure.

## Criterios de aceptación

- [ ] Ambas webs autentican contra Keycloak.
- [ ] Cada web llama exclusivamente a su BFF para operaciones de producto.
- [ ] Existe una pantalla autenticada placeholder por aplicación.
- [ ] Ningún BFF accede directamente a MongoDB.

## DoD

- [ ] build web + .NET en verde;
- [ ] auth happy/error path testeados;
- [ ] screenshot de ambas superficies.

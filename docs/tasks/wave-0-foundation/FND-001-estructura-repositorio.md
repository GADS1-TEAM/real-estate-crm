# FND-001 — Crear el esqueleto del repositorio, workspaces, convenciones de nombres y reglas de dependencia

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** ninguna

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `common-repo-documentation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado esperado

Crear el esqueleto del repositorio, workspaces, convenciones de nombres y reglas de dependencia.

## Contexto mínimo para el agente

1. Este archivo.
2. `ARCHITECTURE.md`.
3. `README.md` para conceptos relevantes.
4. `docs/implementation/POC_TECH_DECISIONS.md`.

## Entregables

- [ ] Crear solución .NET 10 y estructura del monorepo.
- [ ] Crear `apps/crm-web` y `apps/platform-admin-web` como placeholders Next.js/TypeScript.
- [ ] Crear convenciones para `services`, `bffs`, `building-blocks`, `contracts`, `tests` y `docs`.
- [ ] Agregar `.editorconfig`, reglas de dependencias y scripts raíz reproducibles.
- [ ] Documentar cómo levantar, compilar y testear sin infraestructura global salvo Docker y SDKs.

## Criterios de aceptación

- [ ] `dotnet build` compila la solución vacía/base.
- [ ] Los frontends instalan y construyen desde scripts raíz.
- [ ] Ningún proyecto de dominio referencia infraestructura de otro servicio.
- [ ] README de desarrollo permite clonar y compilar en una máquina limpia.

## Definition of Done

- [ ] Build/formato en verde.
- [ ] Tests del paquete en verde.
- [ ] Documentación sincronizada.
- [ ] PR limitado a esta task.

## Evidencia requerida en el PR

- estructura creada;
- comandos de build/test ejecutados;
- decisiones locales tomadas;
- follow-ups fuera de scope.

## Regla de escalamiento

Pedir decisión únicamente si aparece una contradicción que cambie contrato público, ownership de datos o una invariante core. Para detalles locales reversibles, elegir la opción más simple compatible y documentarla.

# FND-002 — Automatizar lint, typecheck, tests, build, convenciones de commit y checks de PR

**Ola:** 0 — Fundación  
**Estado inicial:** `TODO`  
**Dependencias:** `FND-001`

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `dotnet-unit-testing`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Resultado esperado

Automatizar lint, typecheck, tests, build, convenciones de commit y checks de PR.

## Contexto mínimo

- este archivo;
- `ARCHITECTURE.md`;
- `docs/implementation/POC_TECH_DECISIONS.md`;
- salida de `FND-001`.

## Entregables

- [ ] Pipeline CI para build/test de .NET y frontend.
- [ ] Formato/lint/typecheck y validación de contratos.
- [ ] Test de arquitectura/dependencias prohibidas.
- [ ] Checks mínimos de PR y convención de commits sin tooling innecesario.

## Criterios de aceptación

- [ ] Un PR roto falla automáticamente.
- [ ] Una dependencia arquitectónica prohibida falla CI.
- [ ] CI no requiere servicios pagos.
- [ ] Cada check puede ejecutarse localmente.

## DoD

- [ ] pipeline reproducible;
- [ ] documentación de comandos locales;
- [ ] tests del pipeline/fixtures donde corresponda;
- [ ] PR acotado a CI/calidad.

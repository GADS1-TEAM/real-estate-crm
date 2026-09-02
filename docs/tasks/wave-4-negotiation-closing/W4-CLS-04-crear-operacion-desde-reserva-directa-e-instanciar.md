# W4-CLS-04 — Crear operación desde reserva/directa e instanciar/completar checklist

**Dependencias:** `W4-CLS-03`, `W3-PLT-01`  
**Casos de uso:** `CLS-013`, `CLS-014`, `CLS-015`, `CLS-016`

- `CLS-013`: abrir `Transaction` desde Reservation + términos/roles/workflow versionado; `TransactionOpened`.
- `CLS-014`: registrar Transaction mínima directa para inmobiliaria de baja disciplina con comprador + vendedor + Property + términos básicos; `TransactionOpened`.
- `CLS-015`: instanciar Tasks/checklist según operación/etapa/pack; `TaskCreated`.
- `CLS-016`: completar Task con evidencia según definición; `TaskCompleted`.

## Skills aplicables

Derivadas de [`SKILL_ROUTING.md`](../../skills/SKILL_ROUTING.md).

- **Núcleo transversal (siempre):** `aspnetcore-microservice-orchestrator`, `ddd-hexagonal-architecture`, `multitenancy-authorization`, `aspnetcore-security-owasp-baseline`
- **Por borde tocado:** `mongodb-document-modeling`, `mongodb-dotnet-driver`, `aspnetcore-rest-layer`, `event-driven-outbox-inbox`, `dotnet-parsing-and-validation`

Sus MUST y MUST NOT son condición de aceptación de esta task.

## Reglas
El camino mínimo directo es first-class. Checklist guía, pero solo bloquea cuando la etapa/policy lo exige.

## DoD
- [ ] Flujo completo y flujo mínimo directo testeados.
- [ ] Versión de workflow queda fijada en la operación.

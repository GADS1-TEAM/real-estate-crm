# W4-CLS-04 — Crear operación desde reserva/directa e instanciar/completar checklist

**Dependencias:** `W4-CLS-03`, `W3-PLT-01`  
**Casos de uso:** `CLS-013`, `CLS-014`, `CLS-015`, `CLS-016`

- `CLS-013`: abrir `Transaction` desde Reservation + términos/roles/workflow versionado; `TransactionOpened`.
- `CLS-014`: registrar Transaction mínima directa para inmobiliaria de baja disciplina con comprador + vendedor + Property + términos básicos; `TransactionOpened`.
- `CLS-015`: instanciar Tasks/checklist según operación/etapa/pack; `TaskCreated`.
- `CLS-016`: completar Task con evidencia según definición; `TaskCompleted`.

## Reglas
El camino mínimo directo es first-class. Checklist guía, pero solo bloquea cuando la etapa/policy lo exige.

## DoD
- [ ] Flujo completo y flujo mínimo directo testeados.
- [ ] Versión de workflow queda fijada en la operación.

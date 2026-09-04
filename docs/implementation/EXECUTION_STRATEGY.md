# Estrategia de ejecución paralela

> **Documento histórico.** Para el trabajo práctico vigente usar [`IMPLEMENTATION_MASTER_PLAN.md`](IMPLEMENTATION_MASTER_PLAN.md) y [`../tasks/v2/TASK_BOARD.md`](../tasks/v2/TASK_BOARD.md). Los IDs `FND-*`, `W1-*` y las olas mencionadas debajo pertenecen al backlog amplio anterior y no deben ejecutarse para V2.

## Unidad de asignación

Una task = un vertical slice pequeño. Un agente no recibe un microservicio entero ni una ola completa.

## Tres carriles

1. **Contracts:** define primero API/event contracts y mocks cuando hay consumidores paralelos.
2. **Implementation:** cada agente implementa solo las zonas de escritura de su task.
3. **Integration:** se mergea, corre contract/integration tests y recién entonces se desbloquean dependientes.

## Cuándo dos tasks pueden correr en paralelo

Pueden correr en paralelo cuando:

- no existe dependencia directa no satisfecha;
- no modifican el mismo aggregate propietario;
- no necesitan cambiar el mismo contrato público simultáneamente;
- sus zonas de escritura no se superponen materialmente.

Una dependencia puede considerarse satisfecha antes de terminar toda la implementación si el **contrato está aprobado, versionado y existe un stub/mock contractual**.

## Primera ejecución recomendada

1. `FND-001` solo.
2. En paralelo: `FND-002`, `FND-003`, `FND-004` una vez que existe el repo.
3. Luego en paralelo: `FND-005`, `FND-006`, `FND-007`; `FND-008` cuando contratos e infraestructura estén disponibles.
4. Con Ola 0 en verde: `W1-ORG-01`, `W1-PLT-01`, `W1-PTY-01`, `W1-PRP-01` en paralelo.
5. `W1-DEM-01` cuando el contrato de Party esté estable.

## Límite de contexto

Un agente debería trabajar con:

- 1 task file;
- 1–4 casos de uso;
- definiciones de las entidades de esa task;
- contratos directos de dependencias;
- decisiones POC.

Si necesita comprender dos bounded contexts completos para hacer una task, la task está demasiado grande o el boundary está mal definido.

## Regla de PR

Un PR debe corresponder a una task. Si descubre trabajo adicional, se crea follow-up; no se absorbe silenciosamente salvo cambio trivial indispensable.

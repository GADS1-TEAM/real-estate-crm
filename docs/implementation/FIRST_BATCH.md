# Primera tanda de agentes

No conviene lanzar 8 agentes desde cero sobre un repositorio vacío. La primera tanda se ejecuta en tres pasos.

## Paso 1 — un solo agente

### `FND-001` — Estructura del repositorio

Objetivo: crear el carril sobre el que escribirán todos los demás. Hasta mergear esta task, ningún otro agente escribe código productivo.

## Paso 2 — tres agentes en paralelo

Una vez mergeado `FND-001`:

- **Agente A:** `FND-002` — CI/calidad.
- **Agente B:** `FND-003` — contratos compartidos.
- **Agente C:** `FND-004` — infraestructura local Docker Compose.

No comparten ownership material.

## Paso 3 — hasta cuatro agentes en paralelo

Cuando contratos e infraestructura estén suficientemente estables:

- **Agente D:** `FND-005` — shells Web + BFF.
- **Agente E:** `FND-006` — seguridad y tenancy.
- **Agente F:** `FND-007` — observabilidad/resiliencia.
- **Agente G:** `FND-008` — harness de integración, cuando `FND-002/003/004` estén contractualmente disponibles.

## Primera tanda de negocio

Con Ola 0 en verde, lanzar en paralelo:

- `W1-ORG-01` — organización y onboarding.
- `W1-PLT-01` — pack argentino/capabilities/catálogos.
- `W1-PTY-01` — Party mínima.
- `W1-PRP-01` — Property mínima.

Luego `W1-DEM-01` cuando el contrato de `Party` esté estable.

## Regla de corte

Si una task necesita cambiar el contrato de otra task que está simultáneamente en progreso, no se resuelve por merge conflict: se congela el contrato, se acuerda la versión y recién después continúan ambos agentes.

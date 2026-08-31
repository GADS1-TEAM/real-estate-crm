# W2-MAT-03 — Reaccionar a nueva oferta/demanda y explicar el score

**Dependencias:** `W2-MAT-01`  
**Casos de uso:** `MAT-008`, `MAT-009`, `MAT-010`

## Casos de uso

- `MAT-008` Al activar Listing, encontrar Requirements compatibles y generar matches relevantes.
- `MAT-009` Al crear/cambiar Requirement, encontrar Listings activos compatibles.
- `MAT-010` Explicar contribución de criterios, exclusiones, desconocidos y versión del scoring.

## Reglas

Procesamiento event-driven, idempotente, sin acoplar supply/demand al matching. Explicación reproducible para una versión dada del algoritmo.

## DoD
- [ ] Reacciones a ambos lados testeadas.
- [ ] Reprocesamiento no duplica MatchCases.
- [ ] Explain API/read model autorizado.

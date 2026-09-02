# Cómo contribuir

Este repositorio se ejecuta con varias personas y agentes trabajando en paralelo
sobre un monorepo. Las reglas de abajo existen para que ese paralelismo no termine
en conflictos de merge ni en scope silencioso.

## Antes de tocar nada

1. Elegir una task `READY` del [`TASK_BOARD.md`](docs/tasks/TASK_BOARD.md).
2. Leer [`AGENTS.md`](AGENTS.md), el archivo de la task, las secciones relevantes de
   [`README.md`](README.md) y [`ARCHITECTURE.md`](ARCHITECTURE.md), y
   [`POC_TECH_DECISIONS.md`](docs/implementation/POC_TECH_DECISIONS.md).
3. Marcar la task `IN_PROGRESS` en el board, en un commit aparte, para que nadie la
   tome en paralelo.
4. Si la task tiene dependencias sin cerrar, no arrancarla. Ver
   [`EXECUTION_STRATEGY.md`](docs/implementation/EXECUTION_STRATEGY.md).

## Ramas

Una rama por task. Sale de `main` y vuelve a `main` por PR.

```
<tipo>/<ID-TASK>-<slug-corto>
```

Ejemplos:

```
feat/fnd-001-estructura-repositorio
chore/align-skills-dotnet10
docs/skills-ddd-hexagonal
fix/w2-mat-01-score-invalidacion
```

`main` está protegida. No se pushea directo.

Ramas largas de integración (tipo `foundation/...`) solo para tandas de fundación
acordadas por el equipo, no como hábito: cuanto más vive una rama, más caro es el
merge.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/), que es lo que el
historial del repo ya viene usando.

```
<tipo>(<scope>): <descripción en minúscula, imperativo, sin punto final>
```

Tipos: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `build`, `ci`, `perf`.

El scope es el servicio, el bounded context o el ID de task cuando aplica:

```
docs(tasks): add wave 5 operations and integrations
feat(party): registrar party con datos minimos
chore: fix aspnetcore EF Core skill directory name
test(matching): cubrir invalidacion de score
```

Un commit debe poder revertirse solo. Si el mensaje necesita un "y", probablemente
sean dos commits.

## Pull requests

**Un PR = una task.** La regla es de [`AGENTS.md`](AGENTS.md) y no es negociable:
es lo único que hace revisable el trabajo de varios agentes simultáneos.

Si aparece trabajo adicional, se abre un follow-up. No se absorbe en el PR actual,
salvo cambio trivial indispensable, y en ese caso se menciona en la descripción.

### Descripción del PR

- task que cierra;
- casos de uso cubiertos;
- comandos de build/test ejecutados;
- decisiones locales tomadas;
- follow-ups fuera de scope.

### Checklist de merge

- [ ] Definition of Done de la task cumplida.
- [ ] Definition of Done arquitectónica ([`ARCHITECTURE.md`](ARCHITECTURE.md) §19).
- [ ] Ningún cambio fuera del scope de la task.
- [ ] Ningún acceso a colección de otro servicio.
- [ ] `tenantId` presente en filtros e índices donde corresponde.
- [ ] Contratos y eventos versionados si cambiaron.
- [ ] Documentación sincronizada.
- [ ] `TASK_BOARD.md` actualizado a `REVIEW` / `DONE`.

Al menos una revisión de otra persona. Los PR de agente se revisan igual que los
humanos, no más rápido.

## Flujo PRP

Para cambios no triviales: discovery → aprobación → implementación → tests →
evaluación. El PRP se genera en `PRPs/_backlog/`, pasa a `PRPs/_in-progress/` solo
con aprobación explícita, y se cierra en `PRPs/_done/`.

`docs/tasks/` es el backlog maestro; `PRPs/` es el mecanismo de ejecución de una
task tomada. No convertir las 92 tasks en 92 PRPs.

## Cuándo frenar y escalar

Abrir un [ADR](docs/adr/README.md) antes de cambiar ownership de datos, límites de
microservicio, datastore, consistencia cross-service, contrato público, semántica
de evento, invariante core o política de tenancy.

Para detalles locales reversibles: elegir la opción más simple compatible y
documentarla en el PR. No abrir un ADR por cada decisión menor.

Si una task necesita cambiar el contrato de otra que está en progreso, no se
resuelve por merge conflict: se congela el contrato, se acuerda la versión y recién
después continúan ambas.

## Fuentes de verdad

En este orden, ante cualquier contradicción:

1. [`README.md`](README.md) — dominio.
2. [`ARCHITECTURE.md`](ARCHITECTURE.md) — arquitectura y casos de uso.
3. La task asignada — alcance de implementación.

El código existente no gana sobre estos documentos. Si el código los contradice, el
código está mal o falta un ADR.

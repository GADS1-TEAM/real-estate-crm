# Skills archivadas

Skills heredadas que **no aplican al stack de este proyecto**. Se conservan como
referencia; ningún agente debe activarlas.

Sus `description` fueron neutralizadas para que no disparen por triggers.

| Skill | Motivo | Reemplazo |
|---|---|---|
| `aspnetcore-database-access-efcore` | Enseña EF Core sobre bases relacionales. La persistencia del proyecto es MongoDB. Sus triggers (`repositorio`, `paginación`, `projection`, `optimistic concurrency`) coinciden con los de cualquier task de datos, así que se activaba sola y empujaba a modelar Mongo como SQL. | `mongodb-document-modeling` y `mongodb-dotnet-driver` — pendientes, ver [`SKILL_GAPS.md`](../../../docs/skills/SKILL_GAPS.md). |

## Cómo desarchivar

Solo con un [ADR](../../../docs/adr/README.md) aprobado que justifique el cambio de
stack. Mover la carpeta de vuelta a `.github/skills/` y restaurar el `description`
original con triggers reales.

# CLAUDE.md — CRM Inmobiliarias

Fuentes de verdad del proyecto (leelas siempre antes de decidir algo estructural):

@AGENTS.md
@README.md
@ARCHITECTURE.md

## Cómo se arranca el trabajo

Todo trabajo de desarrollo se orquesta con el slash command **`/dev <descripción>`**
(`.claude/commands/dev.md`): clasifica el pedido, pide OK y delega en el subagente correcto.

Subagentes disponibles (`.claude/agents/`):

| Subagente | Para qué |
| --- | --- |
| `prp-discovery-dotnet` | Arma el PRP de una feature no trivial antes de escribir código |
| `developer-dotnet` | Implementa según el PRP; corre `dotnet build` / `dotnet test` |
| `tester-dotnet` | Genera y corre unit + adversarial tests (xUnit + Moq) |
| `evaluator-dotnet` | Revisa código contra MUST/SHOULD de las skills; reporta PASS/FAIL (read-only) |
| `documenter` | READMEs por carpeta, diagramas Mermaid, docs de testing |

## Skills

Las skills técnicas viven en `.claude/skills/` y se auto-activan por sus triggers.
El ruteo por tarea está en `docs/skills/SKILL_ROUTING.md`.

> **Duplicación**: `.github/skills/` y `.github/agents/` son las copias que consume
> GitHub Copilot. `.claude/skills/` es una copia derivada. **Editá siempre en
> `.github/skills/`** y re-sincronizá con `pwsh scripts/sync-skills.ps1`.
> Los agentes de `.claude/agents/` sí divergen a propósito del formato Copilot
> (tools y modelos distintos): mantenelos en paralelo a mano.

## Flujo de PRPs

```
PRPs/_backlog/       → PRP redactado, esperando aprobación del dev
PRPs/_in-progress/   → exactamente UNO a la vez, aprobado, en implementación
PRPs/_done/          → append-only; se mueve al mergear el PR
```

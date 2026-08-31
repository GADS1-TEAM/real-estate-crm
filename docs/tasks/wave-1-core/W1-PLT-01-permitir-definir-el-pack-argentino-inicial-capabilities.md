# W1-PLT-01 — Pack argentino inicial, capabilities, catálogos y métricas

**Dependencias:** `FND-003`, `FND-005`  
**Casos de uso:** `PLT-001`, `PLT-004`, `PLT-006`, `PLT-014`

## Objetivo

Dar al backoffice de plataforma la primera capacidad real de configurar defaults versionados sin convertirlo en editor del dominio core.

## Casos de uso

- `PLT-001` Crear pack base: `PackManifest`, `Capability`; evento `PackDrafted`.
- `PLT-004` Administrar capabilities: metadata, dependencias y compatibilidad; evento `CapabilityPublished`.
- `PLT-006` Administrar catálogos base: tipos/motivos/categorías editables; evento `CatalogVersionPublished`.
- `PLT-014` Configurar definición de métricas: `MetricDefinition`, fórmula, dimensiones y lineage; evento `MetricDefinitionPublished`.

## Reglas

- packs/catálogos/policies son versionados;
- publicar una nueva versión no muta operaciones históricas;
- enums semánticos core no se convierten en catálogos arbitrarios;
- Platform Admin nunca edita MongoDB directamente.

## Criterios de aceptación

- [ ] Se puede crear y publicar un pack base.
- [ ] Capabilities y catálogos tienen lifecycle/versionado.
- [ ] Las métricas declaran origen/lineage esperado.
- [ ] El backoffice no puede redefinir entidades o invariantes core.

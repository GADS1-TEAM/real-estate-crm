# V2-SCP-001 — Alinear alcance, entidades, relaciones y Opportunity facade

- **Ola:** 0 — Definición y diseño
- **Estado:** READY
- **Dependencias:** ninguna
- **UCs:** SCOPE-001, SCOPE-002, SCOPE-003, SCOPE-004, SCOPE-005
- **Owner:** documentación de producto y dominio
- **Write zone:** docs/adr, docs/implementation, docs/tasks/v2 y las secciones de alcance de README.md/ARCHITECTURE.md

## Resultado esperado

Existe una única definición vigente del TP: single-installation, CRM
inmobiliario, Opportunity como fachada/proyección y no como aggregate, Party
con Empresa/Contacto, estados de identidad separados de estados comerciales,
catálogos obligatorios y
lista cerrada de capacidades fuera de alcance.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| SCOPE-001 | Equipo | Contrastar los cuatro PDF, el texto adjunto y la documentación del repo. | Documentación | Matriz de decisiones en el plan maestro. |
| SCOPE-002 | Equipo | Mapear cada entidad conservada, diferida o descartada y sus relaciones. | Dominio | Tabla y diagrama del plan maestro. |
| SCOPE-003 | Equipo | Fijar Requirement/CaptationCase como fuentes de oportunidad. | Dominio | ADR-001 y contrato de fachada. |
| SCOPE-004 | Equipo | Definir Party states y catálogos comerciales obligatorios. | Dominio/configuración | ADR-001 y catálogo de códigos. |
| SCOPE-005 | Equipo | Marcar explícitamente agenda, workflow/tasks, notifications, integraciones, alquileres/pagos y multi-tenancy como fuera. | Producto | Scope matrix y reglas V2. |

## Interfaces

- Consume: README.md, ARCHITECTURE.md, AGENTS.md, docs/implementation,
  docs/tasks, docs/consignas/*.pdf y el pasted-text recibido.
- Produce: docs/adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md y
  docs/implementation/IMPLEMENTATION_MASTER_PLAN.md.
- Contrato conceptual producido: CommercialPipelineItem con sourceType
  REQUIREMENT o CAPTATION_CASE, pipelineKind DEMAND o SUPPLY y semanticStatus
  OPEN/WON/LOST.

## Reglas

- No cambiar el modelo por preferencias de implementación si contradice los
  cuatro PDF o el alcance acordado por el usuario.
- No introducir una entidad Organization para representar clientes; Empresa
  significa cliente corporativo y Party es la identidad reusable.
- No resolver la incompatibilidad de Opportunity creando una segunda copia de
  datos.
- Toda relación inter-contexto queda expresada como ID, contrato, query o
  evento; nunca como acceso a una colección ajena.

## Pasos

- [ ] Registrar en el ADR la precedencia de consigna oficial, alcance V2 y
  documentación conceptual histórica.
- [ ] Completar la matriz de entidades y relaciones del plan maestro.
- [ ] Revisar la lista de catálogos y sus valores iniciales contra la
  especialización inmobiliaria.
- [ ] Revisar que ningún alcance excluido reaparezca en una task V2.
- [ ] Releer el plan maestro buscando Opportunity persistida, tenantId,
  agenda, Task, Notification, pago o integración.

## Criterios de aceptación

- [ ] Un implementador puede decidir si una entidad está dentro o fuera sin
  consultar las 92 tasks históricas.
- [ ] El mapping de Opportunity explica creación, edición, etapa, cierre,
  listado, detalle, filtros y tablero.
- [ ] Party separa identityStatus técnico de commercialStatus y este último
  incluye Potencial, Cliente, Inactivo y No contactar.
- [ ] Los catálogos obligatorios tienen código, etiqueta, orden/versionado y
  owner.
- [ ] No queda una dependencia a docs/consignas inexistente; se usan las cuatro
  rutas PDF reales del checkout.

## Overrides POC

- No hay código ni infraestructura en esta task.
- Los PDF de docs/consignas están sin seguimiento de Git y se leen como
  entrada local; no se editan ni se agregan automáticamente.

## Definition of Done

- [ ] ADR y plan maestro coherentes.
- [ ] Matriz de entidades/relaciones revisada.
- [ ] Board V2 y tasks V2 apuntan al mismo alcance.
- [ ] No se modificaron tareas históricas.

## Evidencia requerida

1. Lista de fuentes revisadas.
2. ADR actualizado.
3. Plan maestro con alcance, relaciones, contratos y riesgos.
4. Resultado de una búsqueda que confirme que las tasks V2 no agregan los
   módulos excluidos.

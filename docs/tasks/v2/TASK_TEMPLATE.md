# Plantilla de task V2

Toda task nueva del alcance V2 debe conservar esta estructura. Una task no
puede ampliar el alcance, crear un owner implícito ni convertir una decisión
documentada en una implementación distinta sin actualizar el ADR aplicable.

## Identidad

- **ID:** `V2-XXX-000`
- **Ola:**
- **Estado:** `READY | TODO | BLOCKED | IN_PROGRESS | DONE`
- **Dependencias:**
- **UCs:**
- **Owner:** bounded context/servicio responsable
- **Write zone:** colecciones, aggregates y contratos que esta task puede escribir

## Resultado esperado

Describir el comportamiento observable y el límite de la task en lenguaje de
negocio.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| | | | | |

## Modelo mínimo

Enumerar aggregates, entidades, estados, invariantes y referencias. No copiar
entidades de otro servicio ni inventar campos obligatorios que el dominio no
exija.

## Interfaces

Enumerar commands, queries, endpoints, eventos publicados/consumidos y
errores públicos estables. Declarar si una API es fachada BFF y dónde vive la
fuente de verdad.

## Reglas

- Autorización en backend para cada operación.
- Persistencia auditable y sin borrado físico cuando exista historial.
- No consultar directamente la DB de otro servicio.
- Sin `tenantId`/`organizationId` para este TP de una sola instalación.
- Sin aggregate, colección ni fuente de verdad `Opportunity`.
- Actividades de email/WhatsApp solo manuales; nunca envío o sincronización.
- Sin agenda, workflow, tasks, recordatorios o notifications.

## Criterios de aceptación

- [ ] Caso feliz verificable.
- [ ] Validaciones y errores públicos estables.
- [ ] Autorización por rol/permisos.
- [ ] Historial/auditoría y relectura después de persistir.
- [ ] Tenant boundary no aplica al TP; no se agrega como requisito artificial.
- [ ] No regresión de las capacidades dentro de scope.

## Overrides POC

Documentar qué decisión del modelo amplio se recorta para esta task y por qué
no se considera una contradicción accidental.

## Definition of Done

- [ ] Tests unitarios y de aplicación.
- [ ] Tests de integración/contrato cuando exista persistencia o API.
- [ ] Telemetría básica y errores observables.
- [ ] Documentación de contrato actualizada.
- [ ] No hay cambios fuera de la write zone.
- [ ] Evidencia reproducible adjunta.

## Evidencia requerida

Indicar comandos, archivos, capturas o resultados que permitan verificar los
criterios sin inferir que una interfaz visual implica persistencia real.

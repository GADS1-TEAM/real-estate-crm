# V2-UX-001 — Incorporar el diseño frontend de Claude Design

- **Ola:** 0 — Definición y diseño
- **Estado:** BLOCKED
- **Dependencias:** V2-SCP-001
- **UCs:** UX-001, UX-002, UX-003
- **Owner:** crm-web y documentación UX
- **Write zone:** documentación UX y apps/crm-web; no modifica aggregates ni contratos de dominio

## Resultado esperado

El diseño que el usuario enviará desde Claude Design queda inventariado,
relacionado con los journeys del CRM y convertido en reglas implementables
para login, Empresa, Contacto, Property/Listing, pipeline, actividad,
métricas e IA.

La task permanece BLOCKED hasta recibir el material. No se reemplaza el diseño
faltante por una maqueta inventada.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| UX-001 | Equipo | Incorporar archivos, enlaces, capturas y decisiones visuales entregados por el usuario. | crm-web | Inventario de fuentes y decisión de uso. |
| UX-002 | Equipo | Mapear cada pantalla a un journey, actor, comando/query y estado de dominio. | crm-web | Matriz pantalla → contrato → estado. |
| UX-003 | Equipo | Documentar loading, empty, error, forbidden, success, validación y responsive states. | crm-web | Catálogo de estados visuales y capturas. |

## Interfaces

- Consume: material de Claude Design, plan maestro V2 y contratos de cada
  journey.
- Produce: inventario del diseño, tokens/componentes acordados y checklist de
  pantallas para V2-REL-001.
- No produce nuevas entidades, campos obligatorios ni eventos.

## Reglas

- La interfaz se diseña por tareas del usuario, no por tablas internas.
- Los textos visibles deben usar el vocabulario inmobiliario acordado:
  Empresa, Contacto, Inmueble, Listing/Producto, Oportunidad, Actividad,
  Etapa, Ganada y Perdida.
- El formulario no puede exigir datos opcionales del modelo.
- Un tipo de actividad Email/WhatsApp es histórico manual y no puede renderizar
  un botón de envío o conexión externa.
- El tablero debe distinguir pipeline DEMAND de SUPPLY cuando ambos estén
  habilitados.
- Los estados de Party y oportunidad deben tener representación visual
  inequívoca y accesible.

## Criterios de aceptación

- [ ] Se registraron todas las fuentes recibidas y su ubicación local.
- [ ] Cada pantalla mínima de la consigna tiene journey, actor y estado de
  éxito/error.
- [ ] El diseño no introduce agenda, tareas, notificaciones, portales,
  integraciones ni administración de alquileres.
- [ ] El detalle de oportunidad muestra su sourceType sin exponer una entidad
  Opportunity ficticia.
- [ ] Existe un checklist que las tasks de frontend pueden ejecutar sin
  reinterpretar el diseño.

## Overrides POC

- Mientras falte el material, solo se permite un shell funcional de la Wave 1.
- Si el material usa una librería visual distinta, se adapta al stack Next.js +
  React + TypeScript sin agregar otra aplicación frontend.

## Definition of Done

- [ ] Material de Claude Design recibido e inventariado.
- [ ] Mapa UX aprobado por el responsable del producto.
- [ ] Estados visuales y responsive states documentados.
- [ ] No se modificaron contratos por una decisión visual sin ADR.

## Evidencia requerida

1. Inventario de archivos/enlaces recibidos.
2. Matriz de pantallas y journeys.
3. Capturas o referencias visuales de los estados principales.
4. Lista de decisiones que todavía requieren confirmación del usuario.

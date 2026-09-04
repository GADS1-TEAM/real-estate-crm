# ADR-001 — Alcance del TP y representación de oportunidad

- Estado: Aceptada para el plan de implementación V2
- Fecha: 2026-09-04
- Alcance: CRM inmobiliario del trabajo práctico

## Contexto

El modelo de dominio original describe un CRM inmobiliario SaaS mucho más
amplio que el trabajo práctico. Incluye multi-tenancy, agenda, workflows,
tareas, notificaciones, canales omnicanal, portales, integraciones,
administración de alquileres, pagos, comisiones y automatización general.

La consigna oficial exige una oportunidad comercial como concepto visible,
pero el recorte acordado conserva la decisión de no crear un aggregate
Opportunity duplicado. El modelo ya tiene conceptos con significado propio
para representar una relación comercial inmobiliaria.

## Decisiones

1. La entrega se implementa para una única instalación de una inmobiliaria.
   No se implementan multi-tenancy, tenantId, organizationId, sucursales
   aisladas ni aislamiento entre organizaciones. La palabra Empresa queda
   reservada para los clientes o actores corporativos del CRM.
2. No se crea aggregate, colección ni entidad fuente llamada Opportunity.
   La UI y el BFF pueden exponer el recurso /opportunities para cumplir el
   lenguaje de la consigna, pero ese recurso es una fachada de lectura y
   comandos.
3. Una oportunidad de demanda se origina en Requirement. Una oportunidad de
   captación se origina en CaptationCase. La progresión se completa con
   Listing, Visit, Negotiation, Reservation y Transaction según corresponda.
4. El tablero usa la proyección CommercialPipelineItem. La proyección reúne
   título, partes, inmueble/listing, responsable, origen, etapa, estado,
   valor estimado, fecha estimada y referencias a hitos, pero no es source of
   truth.
5. El estado visible de la oportunidad se limita a OPEN, WON y LOST. Cada
   fuente conserva su propio estado de dominio y la proyección documenta el
   mapeo.
6. Se conservan las métricas y estadísticas. Se eliminan de su diseño las
   métricas de alquiler, agenda, tareas, SLA o notificaciones y se priorizan
   pipeline, actividad, propiedades, listings, visitas, negociaciones,
   reservas y cierres.
7. Se agregan como catálogos comerciales obligatorios las etapas, tipos de
   actividad, orígenes, motivos de pérdida, tipos de operación y taxonomías
   inmobiliarias necesarias para que el modelo no dependa de strings libres.
8. Party conserva una sola identidad reutilizable. Empresa es una Party de
   tipo LEGAL_ENTITY, Contacto es una Party de tipo NATURAL_PERSON y la
   relación entre ambas se registra con PartyRelationship. Se separan dos
   dimensiones de estado: `identityStatus` conserva el ciclo de vida técnico
   del modelo (`PROVISIONAL`, `ACTIVE`, `ALIASED`, `INACTIVE`, `RESTRICTED`)
   y `commercialStatus` agrega los estados que exige el TP (`POTENTIAL`,
   `CUSTOMER`, `INACTIVE`, `DO_NOT_CONTACT`). El TP administra y muestra el
   segundo; Identity Resolution automática queda fuera.
9. La IA se implementa únicamente después de completar y probar el CRM
   principal. Solo podrá consultar APIs/read models autorizados, generar una
   sugerencia útil y dejar que un usuario la revise, acepte o descarte.
10. Una actividad es un hecho ya ocurrido. Registrar manualmente una actividad
    cuyo tipo sea correo, mensaje o WhatsApp no constituye integración ni
    envío desde el CRM.

## Fuera de esta decisión

Quedan diferidos para una etapa posterior: agenda y disponibilidad,
ScheduleItem, workflows configurables, Task y aprobaciones de workflow,
notifications, envío o sincronización por WhatsApp/email, portales,
sindicación e integraciones, telefonía, Identity Resolution automática,
Documents/Compliance completos, comisiones, administración de alquileres,
Receivable, Payment, liquidaciones, mantenimiento y automatización general.

## Consecuencias

- El BFF debe resolver el sourceType antes de enviar un comando de edición,
  cambio de etapa o cierre.
- Los comandos de Party no deben confundir `identityStatus` con
  `commercialStatus`: una Party puede estar técnicamente ACTIVE y ser
  comercialmente POTENTIAL, CUSTOMER, INACTIVE o DO_NOT_CONTACT.
- El read model de pipeline se puede reconstruir a partir de eventos y
  consultas de los owners; no puede aceptar escrituras directas desde la UI.
- La historia de etapas debe ser append-only y conservar etapa anterior,
  nueva etapa, fecha, usuario y observación.
- El cambio de una etapa debe validarse contra el catálogo vigente y el estado
  de la fuente; una etapa WON o LOST no puede recibir una fuente OPEN.
- Si durante la implementación la evaluación exige una oportunidad física
  persistida, se debe reabrir este ADR antes de crearla; no se agrega una
  entidad duplicada de manera silenciosa.

## Alternativas rechazadas

- Crear Opportunity y copiar dentro todos los datos de Requirement,
  CaptationCase, Listing y Transaction.
- Mantener el alcance original completo y marcar como opcionales las
  exclusiones expresas de la consigna.
- Resolver el tablero con joins directos sobre colecciones de otros servicios.

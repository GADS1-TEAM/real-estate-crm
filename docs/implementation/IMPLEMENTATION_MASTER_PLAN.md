# CRM Inmobiliario TP — Plan maestro de implementación V2

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** entregar un CRM inmobiliario funcional para la consigna, con usuarios, Party/empresa/contacto, inmuebles y listings, procesos comerciales trazables, actividades, métricas, búsqueda y una funcionalidad de IA revisable, sin implementar capacidades que el alcance excluye.

**Architecture:** DDD y arquitectura hexagonal, con microservicios por capacidad de negocio ya definidos en el repositorio, BFF orientado a journeys y MongoDB como persistencia POC. La implementación mantiene contratos y eventos versionados, pero trabaja para una única instalación: no se agrega multi-tenancy ni una entidad Opportunity duplicada. La oportunidad visible es una fachada/proyección de Requirement o CaptationCase y sus hitos relacionados.

**Tech Stack:** .NET 10 / ASP.NET Core, Next.js + React + TypeScript, MongoDB Community, RabbitMQ Community, Keycloak Community/OIDC, Docker Compose, OpenTelemetry, tests unitarios/de integración/de contrato y APIs detrás de ports.

**Spec:** docs/consignas/Consigna_Trabajo_Practico.pdf, docs/consignas/Definiciones-Generales.pdf, docs/consignas/Entregas-CRM.pdf, docs/consignas/Modulos_Principales.pdf, docs/adr/ADR-001-alcance-tp-y-oportunidad-como-proyeccion.md, README.md, ARCHITECTURE.md y el análisis recibido en pasted-text.txt.

## Global Constraints

- La consigna oficial y el alcance acordado por el usuario prevalecen sobre el modelo empresarial original cuando hay contradicción.
- La entrega es para una única instalación; no se implementan multi-tenancy, tenantId, organizationId, aislamiento entre organizaciones ni sucursales como boundary de seguridad.
- Se mantienen DDD, microservicios por capacidad, ports/adapters, BFF, MongoDB Community, RabbitMQ + Outbox/Inbox, Keycloak y Docker Compose donde el slice los necesite.
- No se crea aggregate, colección ni source of truth llamado Opportunity.
- La fachada /opportunities materializa Requirement o CaptationCase y sus relaciones con Listing, Visit, Negotiation, Reservation y Transaction.
- Los estados visibles de oportunidad son OPEN, WON y LOST; los estados internos de cada aggregate conservan su semántica y se mapean de forma explícita.
- Party se presenta como Empresa o Contacto según kind; una relación contacto-empresa se registra con PartyRelationship.
- Party conserva `identityStatus` (`PROVISIONAL`, `ACTIVE`, `ALIASED`, `INACTIVE`, `RESTRICTED`) y agrega `commercialStatus` (`POTENTIAL`, `CUSTOMER`, `INACTIVE`, `DO_NOT_CONTACT`) sin mezclar ambas dimensiones.
- Son obligatorios los catálogos de etapas, tipos de actividad, orígenes, motivos de pérdida, tipos de operación y taxonomías inmobiliarias usadas por la UI.
- Las actividades son hechos históricos ya ocurridos. No se modelan tareas, agenda, recordatorios ni acciones futuras.
- Email, mensaje y WhatsApp pueden aparecer como tipos de actividad cargados manualmente; no se envían ni sincronizan.
- Se conservan métricas y estadísticas, pero solo sobre datos del CRM incluido en este plan; no se agregan métricas de alquiler, agenda, tareas o notificaciones.
- La IA se implementa al final, sobre APIs/read models autorizados, con salida revisable y sin acceso directo del LLM a MongoDB.
- El diseño visual de Claude Design queda como entrada explícita del task V2-UX-001 y no se inventa como sustituto.
- No se incorporan Redis/Valkey, OpenSearch, Kafka, Kubernetes, portales, integraciones externas, administración de alquileres, pagos, comisiones, mantenimiento ni compliance completo.

---

## 1. Evidencia revisada y estado actual

Se revisaron los MD de la raíz, los MD de docs/implementation y docs/tasks,
el board, la plantilla, la estrategia de ejecución, el reporte de validación,
README.md, ARCHITECTURE.md, AGENTS.md y el conjunto de tareas de las waves 0 a
7. También se revisaron los cuatro PDF no versionados de docs/consignas y el
texto adjunto de la discusión con Claude.

El checkout contiene documentación y backlog, no una aplicación ejecutable:
no hay solution, csproj, package.json, código productivo ni tests. El board
original declara 92 tasks y 274 UCs, pero describe el producto empresarial
completo y no debe seguir siendo el backlog activo de este TP.

docs/consignas/ existe en el checkout, aunque sus cuatro PDF están sin
seguimiento de Git. Se preservan sin editarlos ni agregarlos automáticamente.
No existía docs/adr; este plan agrega el ADR que registra las decisiones de
alcance.

## 2. Alcance aprobado

### Dentro

| Capacidad | Regla de implementación |
|---|---|
| Inicio de sesión | OIDC/Keycloak y al menos un usuario habilitado. |
| Usuarios, roles y permisos | Administrador, Vendedor y Responsable Comercial; autorización en backend. |
| Empresas y contactos | Party única por identidad, formularios distintos y relación contacto-empresa explícita. |
| Estados de Party | Potencial, Cliente, Inactivo y No contactar; baja lógica, nunca borrado físico con historial. |
| Catálogos comerciales | Etapas, tipos de actividad, orígenes, motivos de pérdida, operaciones y tipos de inmueble. |
| Propiedades | Property física, datos urbanos/rurales mínimos, interés del propietario y Listing comercial. |
| Demanda y captación | Requirement para quien busca y CaptationCase para captar un inmueble; Valuation/Mandate mínimos si el recorrido los usa. |
| Oportunidad visible | Proyección/fachada sobre las entidades anteriores, sin Opportunity persistida. |
| Progresión inmobiliaria | Match, visita ocurrida, negociación, reserva sin pagos y operación cerrada. |
| Actividades e historial | Registro manual de hechos y timeline por Party, Property/Listing y proceso comercial. |
| Métricas y estadísticas | Dashboard de pipeline, actividad, propiedades/listings, visitas, negociaciones y cierres, con lineage. |
| Búsqueda y navegación | Filtros, orden, paginación y detalle; MongoDB detrás de ports. |
| IA | Asistente de resumen/priorización o detección de información faltante, siempre revisable y posterior al core. |

### Fuera o diferido

| Capacidad | Tratamiento |
|---|---|
| Multi-tenant y múltiples organizaciones | Fuera del TP. No usar tenantId ni organizationId como boundary. |
| Agenda y disponibilidad | Fuera. Visit solo registra un hecho ocurrido con fecha/hora. |
| Workflow, Task y aprobaciones | Fuera. El catálogo de etapas no crea checklists ni SLAs. |
| Notifications y recordatorios | Fuera. |
| WhatsApp, email, portales e integraciones | Fuera como conexión/envío/sincronización; solo se pueden registrar actividades manuales. |
| Administración de alquileres y pagos | Fuera. No ManagedAgreement, Receivable, Payment, deuda ni liquidación. |
| Comisiones, mantenimiento, documentos y compliance completos | Diferidos por no ser necesarios para la consigna. |
| Identity Resolution automática | Diferida; se conserva identityStatus por compatibilidad, pero el TP no genera ALIASED ni implementa resolución automática. |
| Automatización general | Fuera; la única automatización incluida es la funcionalidad de IA definida al final. |

## 3. Modelo de entidades y relaciones

### Entidades conservadas

| Entidad | Owner | Relación que debe quedar explícita | Alcance |
|---|---|---|---|
| UserAccount, Role, Permission | access-service | Responsable comercial se referencia por userId; no es Party. | Completo |
| Party | party-service | kind LEGAL_ENTITY = Empresa; kind NATURAL_PERSON = Contacto; identityStatus separado de commercialStatus. | Completo para TP |
| PartyRelationship | party-service | Contacto CONTACT_OF/REPRESENTS Empresa; también propietarios/interesados según contexto. | Mínimo y auditable |
| Property | property-service | Representa el inmueble físico; puede ser referenciado por varios listings. | Urbano y rural mínimo |
| PropertyInterest | property-service | Vincula uno o más ownerPartyId con Property; no se reduce a ownerId fijo. | Propietario mínimo |
| CaptationCase | supply-service | ContactParty + candidateProperty; desemboca en Valuation/Mandate/Listing. | Mínimo |
| Valuation | supply-service | Es histórica; compara expectativa del propietario con valoración, sin sobrescribir. | Mínimo |
| CommercialMandate | supply-service | Autoriza comercializar una Property bajo operación y vigencia. | Mínimo si se necesita para activar Listing |
| Listing | supply-service | Oferta comercial de Property; es el producto inmobiliario seleccionable. | Completo para TP |
| Requirement | demand-service | Una o más Parties buscan una operación; recibe Listings compatibles. | Completo para TP |
| MatchCase | matching-service | Une Requirement con Listing y explica compatibilidad. | Determinístico V1 |
| Visit | commercial-service | Registra una visita ya realizada; no tiene ScheduleItem. | Hecho histórico |
| Negotiation y Proposal | commercial-service | Negotiation relaciona Requirement/Listing y conserva propuestas inmutables. | Mínimo |
| Reservation | commercial-service | Hito previo a Transaction; no registra ni procesa pagos. | Mínimo |
| Transaction | commercial-service | Cierre de la operación; guarda outcome y valor final cuando corresponda. | Mínimo |
| Interaction | activity-service | Se presenta como Actividad; referencia Parties y opcionalmente el proceso. | Manual, histórica |
| MetricDefinition, MetricObservation | analytics-service | Proyecciones derivadas con fórmula, versión y lineage. | Completo en métricas incluidas |
| AgentDecision | automation-ai-service | Evidencia de la sugerencia IA, revisión y resultado; no guarda chain-of-thought. | Solo IA |

### Entidades que no se implementan en esta versión

Organization como boundary multi-tenant, OrganizationalUnit, ScheduleItem,
Conversation omnicanal, Inquiry automática, IdentityResolutionCase,
Documents/ComplianceCase, ChannelPublication, WorkflowTemplate, Task,
ExceptionRequest, Notification, CommissionPolicy, CommissionCalculation,
ManagedAgreement, RentSchedule, Receivable, Payment, PaymentAllocation,
Expense, OwnerSettlement, ArrearsCase, MaintenanceCase, portales,
telefonía, AutomationPolicy general y AgentToday basado en agenda/SLA.

### Relación operacional

~~~mermaid
flowchart LR
    U[UserAccount + Role] --> P[Party]
    P --> PR[PartyRelationship]
    P --> R[Requirement]
    P --> C[CaptationCase]
    C --> V[Valuation]
    C --> M[CommercialMandate]
    C --> L[Listing]
    I[Property] --> L
    I --> PI[PropertyInterest]
    R --> MC[MatchCase]
    L --> MC
    MC --> VIS[Visit ocurrida]
    R --> N[Negotiation]
    L --> N
    N --> RES[Reservation]
    RES --> T[Transaction]
    P --> A[Activity]
    R --> A
    L --> A
    T --> A
    R --> CP[CommercialPipelineItem]
    C --> CP
    N --> CP
    T --> CP
    CP --> D[Metrics / Dashboard]
    D --> AI[IA revisable]
~~~

La dirección del diagrama expresa dependencia conceptual, no foreign keys ni
acceso directo a colecciones ajenas. Cada servicio publica comandos, queries o
eventos y conserva su propio ownership.

## 4. Decisión de Opportunity

El concepto de la consigna se conserva en UX y contrato de experiencia, pero
no como entidad de dominio fuente.

### Fuentes

- DEMAND: Requirement es la fuente de la oportunidad de compra/alquiler.
- SUPPLY: CaptationCase es la fuente de la oportunidad de captación.

### Progresión

- Property identifica el activo físico.
- Listing convierte Property en oferta comercial y cumple el papel de
  producto inmobiliario seleccionable.
- MatchCase vincula una necesidad con una oferta.
- Visit confirma un hecho ocurrido.
- Negotiation y Proposal conservan la conversación comercial estructurada.
- Reservation marca un compromiso previo, sin administración de dinero.
- Transaction registra la operación ganada/cerrada o cancelada.

### Proyección y fachada

CommercialPipelineItem debe tener, como mínimo:

- pipelineItemId estable y sourceType + sourceId;
- pipelineKind DEMAND o SUPPLY;
- title;
- relatedPartyIds;
- propertyId y listingId opcionales;
- operationTypeCode;
- responsibleUserId;
- originCode;
- estimatedValue y estimatedCloseDate opcionales;
- currentStageCode;
- semanticStatus OPEN, WON o LOST;
- stageHistoryRef;
- referencias a MatchCase, Visit, Negotiation, Reservation y Transaction.

El BFF puede exponer:

- GET /opportunities con filtros por responsable, etapa, estado, origen y
  paginación;
- GET /opportunities/{pipelineItemId} para detalle;
- POST /opportunities para crear Requirement o CaptationCase según pipelineKind;
- PATCH /opportunities/{pipelineItemId} para enviar el comando al owner;
- POST /opportunities/{pipelineItemId}/stage para cambiar la etapa;
- POST /opportunities/{pipelineItemId}/win y /loss para cerrar el proceso.

El stage change debe generar un registro append-only con etapa anterior, etapa
nueva, fecha/hora, usuario y observación. La proyección se actualiza por evento
versionado e idempotente. No se aceptan escrituras directas al read model.

## 5. Estados y catálogos

### Party

#### Estado de identidad

`identityStatus` conserva la semántica técnica del modelo original:
`PROVISIONAL`, `ACTIVE`, `ALIASED`, `INACTIVE` y `RESTRICTED`. La resolución
automática de identidades no se implementa en este TP; por eso `ALIASED` no se
produce desde una pantalla del alcance, pero tampoco se reutiliza como estado
comercial.

#### Estado comercial

| Código interno | Etiqueta | Regla |
|---|---|---|
| POTENTIAL | Potencial | Identidad registrada sin relación comercial concretada. |
| CUSTOMER | Cliente | Party con al menos una relación comercial concretada o reconocida. |
| INACTIVE | Inactivo | No se usa actualmente; conserva historial y puede reactivarse con autorización. |
| DO_NOT_CONTACT | No contactar | Restricción comercial de contacto; no borra información ni historial. |

El campo comercial es independiente de `identityStatus`. Por ejemplo, una
Party puede ser `ACTIVE + POTENTIAL` o `ACTIVE + DO_NOT_CONTACT`. El estado
`INACTIVE` puede aparecer en ambas dimensiones con significados distintos: la
identidad técnica está deshabilitada o la relación comercial no está activa.
El alta normal de V2 usa `identityStatus=ACTIVE` y
`commercialStatus=POTENTIAL`; `PROVISIONAL`, `ALIASED` y `RESTRICTED` quedan
reservados al ciclo técnico del owner.
DO_NOT_CONTACT no significa que se elimine la Party ni que no pueda
consultarse su historial.

### Catálogos obligatorios

1. CommercialStage: código, nombre, orden, pipelineKind DEMAND/SUPPLY,
   semanticState OPEN/WON/LOST, active y version.
2. ActivityType: llamada, correo electrónico, mensaje, reunión presencial,
   reunión virtual, demostración, envío de propuesta, nota interna y otro.
3. CommercialOrigin: sitio web, redes sociales, publicidad, recomendación,
   evento, prospección, cliente existente y portal inmobiliario como origen
   manual.
4. LossReason: precio, falta de presupuesto, competidor, inmueble inadecuado,
   falta de respuesta, decisión postergada, inmueble no disponible,
   financiación y otro.
5. OperationType: compraventa, alquiler residencial, alquiler comercial,
   alquiler temporario y las modalidades inmobiliarias que se usen en la
   demo.
6. PropertyType: casa, departamento, PH, local, oficina, terreno, campo,
   cochera y otros tipos necesarios para distinguir lo urbano de lo rural.

Los catálogos se versionan y tienen baja lógica. Las operaciones históricas
conservan el código y la versión aplicada. El Administrador gestiona los
catálogos; los estados semánticos OPEN/WON/LOST y los invariantes no son
editables como texto libre.

## 6. Secuencia por waves

### Wave 0 — Definición y entrada de diseño

- V2-SCP-001 fija el recorte, relaciones, catálogos, estados y contrato de
  Opportunity como proyección.
- V2-UX-001 incorpora el diseño de Claude Design cuando sea enviado, mapea
  pantallas y documenta estados visuales. Queda BLOCKED mientras falte el
  material; no se inventan decisiones visuales que el usuario todavía no
  entregó.

### Wave 1 — Fundación ejecutable

- V2-FND-001 crea solution, servicios, BFF, apps web, contracts, tests y
  reglas de dependencia.
- V2-FND-002 define IDs no multi-tenant, errores, paginación, eventos,
  Outbox/Inbox, autenticación OIDC y persistencia por ports.
- V2-FND-003 levanta Mongo/Rabbit/Keycloak con Compose y agrega CI,
  observabilidad y healthchecks.

### Wave 2 — Acceso, Party y catálogos

- V2-ACL-001 implementa usuario, roles, permisos y responsable comercial.
- V2-CAT-001 implementa los catálogos obligatorios y sus defaults.
- V2-PTY-001 implementa Empresa, Contacto, PartyRelationship, estados y
  detalle/historial.

### Wave 3 — Núcleo inmobiliario

- V2-PRP-001 implementa Property, PropertyInterest, Listing y la vista de
  productos inmobiliarios.
- V2-DMD-001 implementa Requirement y CaptationCase con Valuation/Mandate
  mínimo y sus relaciones.
- V2-MAT-001 calcula compatibilidad determinística entre Requirement y Listing,
  sin ML ni integraciones externas.

### Wave 4 — Progresión comercial y oportunidad visible

- V2-PIPE-001 implementa CommercialPipelineItem, la fachada de oportunidades,
  listado, detalle, filtros base, tablero y cambio persistido de etapa.
- V2-COM-001 implementa visita ocurrida, negociación/propuestas, reserva sin
  pagos y Transaction de cierre.

### Wave 5 — Actividades e inteligencia operativa

- V2-ACT-001 registra actividades y reconstruye timelines en Party, Property/
  Listing y pipeline.
- V2-ANA-001 construye métricas y estadísticas con definiciones, observaciones,
  lineage, filtros y dashboard de manager.

### Wave 6 — IA y cierre

- V2-AI-001 agrega la asistencia IA seleccionada sobre el historial y
  pipeline, con evidencia y revisión humana.
- V2-REL-001 cierra búsqueda, paginación, QA, aceptación y demostración final.

## 7. Hitos de entrega

### Primera entrega — 24/09

Debe poder demostrarse el flujo:

1. iniciar sesión;
2. registrar una Empresa y un Contacto;
3. relacionarlos;
4. consultar listados y detalles;
5. seleccionar un Listing/producto precargado;
6. crear una oportunidad visible que cree un Requirement o CaptationCase;
7. verla en el tablero;
8. cambiarla de etapa;
9. recargar y comprobar que el cambio persiste.

Para esta fecha se permiten etapas y productos precargados. No son gates de la
primera entrega la configuración completa de catálogos, actividades,
historial de etapas, permisos completos, métricas ni IA.

### Entrega final — 12/11

Debe cubrir usuarios y roles, empresas/contactos completos, catálogos,
Property/Listing, Requirement/Captation, progresión inmobiliaria, Opportunity
facade, actividades, historial comercial, historial de etapas, cierre ganado/
perdido, búsqueda/filtros/paginación, métricas/estadísticas y la IA elegida.
La IA queda después de los tests del core.

## 8. Mapa de tareas y zonas de escritura

El board activo es docs/tasks/v2/TASK_BOARD.md. Cada task tiene un solo owner
de escritura principal y declara los contratos que consume/publica.

| Task | Entregable | Zona de escritura principal |
|---|---|---|
| V2-SCP-001 | Alcance, ADR y documentación coherente | docs, sin código de negocio |
| V2-UX-001 | Diseño de Claude Design integrado al mapa UX | apps/crm-web/docs de UX |
| V2-FND-001 | Esqueleto | solution, apps, services, bffs, contracts, tests |
| V2-FND-002 | Contratos/auth/persistencia base | contracts, building-blocks, auth adapters |
| V2-FND-003 | Compose/CI/telemetría | infra, .github, observability |
| V2-ACL-001 | Usuarios/roles/permisos | access-service |
| V2-CAT-001 | Catálogos versionados | platform-config-service |
| V2-PTY-001 | Party/Empresa/Contacto | party-service |
| V2-PRP-001 | Property/Listing | property-service y supply-service |
| V2-DMD-001 | Requirement/Captation | demand-service y supply-service |
| V2-MAT-001 | Match | matching-service |
| V2-PIPE-001 | Proyección/fachada de oportunidad | analytics-service + operations-bff; comandos en owners |
| V2-COM-001 | Visita/negociación/reserva/cierre | commercial-service |
| V2-ACT-001 | Actividad e historial | activity-service |
| V2-ANA-001 | Métricas/dashboard | analytics-service |
| V2-AI-001 | IA revisable | automation-ai-service |
| V2-REL-001 | Integración y aceptación | tests, apps y bff de cada slice, sin cambiar owners |

## 9. Estrategia de pruebas

- Unit: invariantes de cada aggregate, transiciones de estados, catálogos,
  mapeo de oportunidad, valores opcionales y cierre.
- Application: comandos y queries por owner, autorización por rol, no
  modificación de registros históricos y baja lógica.
- Integration: MongoDB, RabbitMQ, Outbox/Inbox, reconstrucción de proyecciones,
  persistencia del stage change y reload de la primera entrega.
- Contract: OpenAPI del BFF, contratos de eventos y compatibilidad de
  CommercialPipelineItem.
- End-to-end: login, Empresa/Contacto, Listing, creación de Requirement o
  CaptationCase, tablero, cambio de etapa, actividad, cierre y dashboard.
- AI: evidencia registrada, contexto acotado, salida revisable, rechazo sin
  mutar el dominio y no acceso directo a MongoDB.

Los tests no deben crear una falsa cobertura de multi-tenancy. En su lugar
deben verificar que los permisos por rol y la asignación de responsables se
aplican en backend y que un usuario no puede acceder a registros fuera de su
scope funcional.

## 10. Riesgos y decisiones controladas

1. Opportunity facade: es la decisión de alcance aprobada, pero se debe
   mantener el mapping visible en UX y documentación para que el evaluador
   encuentre el concepto de la consigna.
2. Diseño faltante: V2-UX-001 queda bloqueada hasta recibir el material de
   Claude Design; las tasks de backend pueden avanzar con contratos.
3. Confusión Empresa/Organization: Empresa es cliente corporativo; no se
   debe introducir Organization como tenant.
4. Email/WhatsApp: una actividad manual con ese tipo no puede llamar a un
   proveedor externo ni emitir mensajes.
5. Etapas versus workflow: CommercialStage solo ordena el embudo. No agrega
   Task, SLA, recordatorio ni aprobación.
6. Alquiler versus administración de alquileres: OperationType puede incluir
   alquiler como operación comercial; eso no habilita ManagedAgreement,
   cobranza, pagos ni liquidaciones.

## 11. Definition of Done del plan

- [ ] Todas las tasks V2 tienen alcance, UCs, owner, write zone, contratos,
  criterios y evidencia.
- [ ] La primera entrega es reproducible desde Docker Compose y conserva el
  cambio de etapa en una recarga.
- [ ] El modelo no contiene tenantId/organizationId ni agrega Opportunity
  como source of truth.
- [ ] Los catálogos y ambos grupos de Party states están documentados y usados por commands,
  validaciones, queries y UI.
- [ ] Las actividades son hechos históricos y el stage history es append-only.
- [ ] Métricas y estadísticas tienen lineage y no dependen de capacidades fuera
  de scope.
- [ ] IA se ejecuta después del core, tiene revisión humana y deja evidencia.
- [ ] Tests, contratos, documentación y UX de los journeys incluidos están en
  verde.

## 12. Orden de ejecución práctico

1. Ejecutar V2-SCP-001 y revisar el ADR.
2. Mantener V2-UX-001 bloqueada hasta recibir el diseño; registrar el material
   cuando llegue.
3. Ejecutar V2-FND-001.
4. Ejecutar V2-FND-002 y V2-FND-003 en paralelo cuando el esqueleto compile.
5. Ejecutar V2-ACL-001, V2-CAT-001 y V2-PTY-001 con contratos estables.
6. Ejecutar V2-PRP-001; luego V2-DMD-001 y V2-MAT-001.
7. Ejecutar V2-PIPE-001 y cerrar el flujo de la primera entrega.
8. Ejecutar V2-COM-001 y V2-ACT-001.
9. Ejecutar V2-ANA-001.
10. Desbloquear V2-AI-001 después de la regresión del core.
11. Ejecutar V2-REL-001 y preparar la demo final.

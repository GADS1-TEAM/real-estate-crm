# Task Board

Cobertura: **274/274 casos de uso** · Tasks: **92**.

Estados permitidos: `TODO`, `READY`, `IN_PROGRESS`, `BLOCKED`, `REVIEW`, `DONE`, `DEFERRED`.

## Ola 0 — Fundación

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`FND-001`](wave-0-foundation/FND-001-estructura-repositorio.md) | Crear el esqueleto del repositorio, workspaces, convenciones de nombres y reglas de dependencia. | 0 | — | `READY` |
| [`FND-002`](wave-0-foundation/FND-002-ci-calidad.md) | Automatizar lint, typecheck, tests, build, convenciones de commit y checks de PR. | 0 | `FND-001` | `TODO` |
| [`FND-003`](wave-0-foundation/FND-003-contratos-compartidos.md) | Definir contratos versionados, IDs, errores, paginación, metadata de actor/tenant y envelope de eventos. | 0 | `FND-001` | `TODO` |
| [`FND-004`](wave-0-foundation/FND-004-infra-local.md) | Levantar la plataforma POC mínima con Docker Compose: MongoDB Community, RabbitMQ Community y Keycloak; agregar logs/OpenTelemetry sin stack pesado. | 0 | `FND-001` | `TODO` |
| [`FND-005`](wave-0-foundation/FND-005-shells-web-bff.md) | Crear crm-web, platform-admin-web y sus BFFs .NET 10 con navegación, OIDC y componentes base. Mobile queda diferido. | 0 | `FND-001`, `FND-003` | `TODO` |
| [`FND-006`](wave-0-foundation/FND-006-seguridad-tenancy.md) | Fijar tenantId, actor context, OIDC adapter, authorization port, audit envelope y propagación de correlationId. | 0 | `FND-003`, `FND-004` | `TODO` |
| [`FND-007`](wave-0-foundation/FND-007-observabilidad-resiliencia.md) | OpenTelemetry, logs estructurados, métricas RED/USE, health/readiness, retry policy e idempotency helpers. | 0 | `FND-004` | `TODO` |
| [`FND-008`](wave-0-foundation/FND-008-harness-integracion.md) | Crear testcontainers/fixtures, consumer-driven contracts y pruebas de eventos/APIs. | 0 | `FND-002`, `FND-003`, `FND-004` | `TODO` |

## Ola 1 — Núcleo operativo mínimo

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W1-ORG-01`](wave-1-core/W1-ORG-01-permitir-crear-una-inmobiliaria-estructura-minima-y.md) | Permitir crear una inmobiliaria, estructura mínima y dejarla lista para operar con defaults. | 3 | `FND-005`, `FND-006` | `TODO` |
| [`W1-ORG-02`](wave-1-core/W1-ORG-02-invitar-usuarios-asignar-rol-scope-consultar-permisos.md) | Invitar usuarios, asignar rol/scope, consultar permisos efectivos y desactivar acceso. | 4 | `W1-ORG-01` | `TODO` |
| [`W1-PLT-01`](wave-1-core/W1-PLT-01-permitir-definir-el-pack-argentino-inicial-capabilities.md) | Permitir definir el pack argentino inicial, capabilities, catálogos y definiciones de métricas. | 4 | `FND-003`, `FND-005` | `TODO` |
| [`W1-PTY-01`](wave-1-core/W1-PTY-01-registrar-una-party-con-datos-minimos-agregar.md) | Registrar una Party con datos mínimos, agregar contacto, buscarla y mostrar completitud sin bloquear. | 4 | `FND-005`, `FND-006` | `TODO` |
| [`W1-PRP-01`](wave-1-core/W1-PRP-01-registrar-inmueble-urbano-rural-con-datos-minimos.md) | Registrar inmueble urbano/rural con datos mínimos, enriquecerlo y buscarlo por zona/mapa. | 4 | `FND-005`, `FND-006` | `TODO` |
| [`W1-DEM-01`](wave-1-core/W1-DEM-01-crear-intencion-convertirla-en-requirement-y-soportar.md) | Crear intención, convertirla en Requirement y soportar múltiples necesidades simultáneas por Party. | 4 | `W1-PTY-01` | `TODO` |

## Ola 2 — Loop comercial completo

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W2-ORG-01`](wave-2-commercial-loop/W2-ORG-01-transferir-cartera-completa-parcial-y-visualizar-carga.md) | Transferir cartera completa/parcial y visualizar carga organizacional. | 3 | `W1-ORG-02`, `W1-PTY-01`, `W1-DEM-01` | `TODO` |
| [`W2-PTY-01`](wave-2-commercial-loop/W2-PTY-01-enriquecer-party-como-persona-humana-juridica-o.md) | Enriquecer Party como persona humana, jurídica o estructura jurídica. | 3 | `W1-PTY-01` | `TODO` |
| [`W2-PTY-02`](wave-2-commercial-loop/W2-PTY-02-registrar-identidad-fiscalidad-y-relaciones-entre-parties.md) | Registrar identidad/fiscalidad y relaciones entre Parties con vigencia. | 4 | `W2-PTY-01` | `TODO` |
| [`W2-PTY-03`](wave-2-commercial-loop/W2-PTY-03-componer-vista-360-y-permitir-correcciones-con.md) | Componer vista 360 y permitir correcciones con historial. | 2 | `W2-PTY-02` | `TODO` |
| [`W2-PRP-01`](wave-2-commercial-loop/W2-PRP-01-modelar-edificios-campos-unidades-y-analizar-rendimiento.md) | Modelar edificios/campos/unidades y analizar rendimiento agregado. | 4 | `W1-PRP-01` | `TODO` |
| [`W2-SUP-01`](wave-2-commercial-loop/W2-SUP-01-abrir-asignar-priorizar-y-registrar-actividad-de.md) | Abrir, asignar, priorizar y registrar actividad de una captación. | 4 | `W1-PTY-01`, `W1-PRP-01` | `TODO` |
| [`W2-SUP-02`](wave-2-commercial-loop/W2-SUP-02-crear-uno-o-varios-listings-terminos-comerciales.md) | Crear uno o varios Listings, términos comerciales y presentación base. | 4 | `W2-SUP-01` | `TODO` |
| [`W2-SUP-03`](wave-2-commercial-loop/W2-SUP-03-activar-pausar-reanudar-y-cerrar-un-listing.md) | Activar, pausar, reanudar y cerrar un Listing. | 3 | `W2-SUP-02` | `TODO` |
| [`W2-INT-01`](wave-2-commercial-loop/W2-INT-01-permitir-registrar-una-interaccion-rapida-y-ver.md) | Permitir registrar una interacción rápida y ver timeline omnicanal aunque todavía no haya conectores. | 3 | `W1-PTY-01` | `TODO` |
| [`W2-DEM-01`](wave-2-commercial-loop/W2-DEM-01-resolver-inquiry-como-captacion-caso-existente-o.md) | Resolver Inquiry como captación, caso existente o no comercial. | 3 | `W1-DEM-01`, `W2-SUP-01` | `TODO` |
| [`W2-DEM-02`](wave-2-commercial-loop/W2-DEM-02-modelar-must-have-preferido-indiferente-desconocido-y.md) | Modelar must-have, preferido, indiferente/desconocido y confirmación. | 4 | `W1-DEM-01` | `TODO` |
| [`W2-DEM-03`](wave-2-commercial-loop/W2-DEM-03-actualizar-por-aprendizaje-pausar-cerrar-y-sugerir.md) | Actualizar por aprendizaje, pausar/cerrar y sugerir enriquecimiento. | 4 | `W2-DEM-02` | `TODO` |
| [`W2-MAT-01`](wave-2-commercial-loop/W2-MAT-01-generar-candidatos-calcular-score-explicable-e-invalidarlo.md) | Generar candidatos, calcular score explicable e invalidarlo por cambios. | 3 | `W2-SUP-02`, `W2-DEM-02` | `TODO` |
| [`W2-MAT-02`](wave-2-commercial-loop/W2-MAT-02-presentar-match-y-registrar-descartes-de-cliente.md) | Presentar match y registrar descartes de cliente/agente. | 4 | `W2-MAT-01` | `TODO` |
| [`W2-MAT-03`](wave-2-commercial-loop/W2-MAT-03-reaccionar-a-nueva-oferta-demanda-y-explicar.md) | Reaccionar a nueva oferta/demanda y explicar el score. | 3 | `W2-MAT-01` | `TODO` |
| [`W2-ANA-01`](wave-2-commercial-loop/W2-ANA-01-primeros-read-models-operativos-para-agente-manager.md) | Primeros read models operativos para agente/manager y embudo. | 3 | `W2-SUP-03`, `W2-DEM-03`, `W2-MAT-02` | `TODO` |
| [`W2-ANA-02`](wave-2-commercial-loop/W2-ANA-02-medir-listing-huecos-oferta-demanda-lineage-y.md) | Medir Listing, huecos oferta-demanda, lineage y UNKNOWN vs cero. | 5 | `W2-ANA-01` | `TODO` |

## Ola 3 — Integridad, campo y cumplimiento

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W3-ORG-01`](wave-3-integrity-compliance/W3-ORG-01-soportar-excepciones-de-permisos-y-configuracion-por.md) | Soportar excepciones de permisos y configuración por tenant/sucursal. | 4 | `W1-ORG-02`, `W1-PLT-01` | `TODO` |
| [`W3-PTY-01`](wave-3-integrity-compliance/W3-PTY-01-archivar-reactivar-party-y-crearla-desde-canales.md) | Archivar/reactivar Party y crearla desde canales de forma automática. | 3 | `W1-PTY-01`, `W2-INT-01` | `TODO` |
| [`W3-IDR-01`](wave-3-integrity-compliance/W3-IDR-01-disparar-resolucion-por-alta-cambio-y-recuperar.md) | Disparar resolución por alta/cambio y recuperar candidatos. | 3 | `W2-PTY-02` | `TODO` |
| [`W3-IDR-02`](wave-3-integrity-compliance/W3-IDR-02-calcular-score-auto-resolver-alta-confianza-o.md) | Calcular score, auto-resolver alta confianza o solicitar revisión. | 3 | `W3-IDR-01` | `TODO` |
| [`W3-IDR-03`](wave-3-integrity-compliance/W3-IDR-03-confirmar-negar-revertir-y-explicar-unificacion.md) | Confirmar, negar, revertir y explicar unificación. | 4 | `W3-IDR-02` | `TODO` |
| [`W3-DOC-01`](wave-3-integrity-compliance/W3-DOC-01-carga-versionado-validacion-y-rechazo-documental.md) | Carga, versionado, validación y rechazo documental. | 4 | `FND-004`, `FND-006` | `TODO` |
| [`W3-DOC-02`](wave-3-integrity-compliance/W3-DOC-02-vencimientos-requisitos-contextuales-y-consentimiento-revocacion.md) | Vencimientos, requisitos contextuales y consentimiento/revocación. | 4 | `W3-DOC-01`, `W1-PLT-01` | `TODO` |
| [`W3-PRP-01`](wave-3-integrity-compliance/W3-PRP-01-derechos-intereses-registros-legales-e-historia-significativa.md) | Derechos/intereses, registros legales e historia significativa. | 4 | `W2-PRP-01`, `W3-DOC-01` | `TODO` |
| [`W3-SUP-01`](wave-3-integrity-compliance/W3-SUP-01-solicitar-emitir-tasaciones-y-comparar-expectativa-vs.md) | Solicitar/emitir tasaciones y comparar expectativa vs valoración. | 4 | `W2-SUP-01`, `W3-PRP-01` | `TODO` |
| [`W3-SUP-02`](wave-3-integrity-compliance/W3-SUP-02-negociar-registrar-renovar-y-vigilar-vencimiento-del.md) | Negociar, registrar, renovar y vigilar vencimiento del mandato. | 4 | `W3-SUP-01`, `W3-DOC-01` | `TODO` |
| [`W3-SUP-03`](wave-3-integrity-compliance/W3-SUP-03-cerrar-captacion-ganada-perdida-y-dormir-reactivar.md) | Cerrar captación ganada/perdida y dormir/reactivar casos. | 3 | `W3-SUP-02` | `TODO` |
| [`W3-VIS-01`](wave-3-integrity-compliance/W3-VIS-01-crear-consultar-disponibilidad-reprogramar-y-cancelar-citas.md) | Crear, consultar disponibilidad, reprogramar y cancelar citas. | 4 | `W1-ORG-02` | `TODO` |
| [`W3-VIS-02`](wave-3-integrity-compliance/W3-VIS-02-sincronizar-google-y-outlook-mediante-adapters.md) | Sincronizar Google y Outlook mediante adapters. | 2 | `W3-VIS-01` | `TODO` |
| [`W3-VIS-03`](wave-3-integrity-compliance/W3-VIS-03-crear-confirmar-iniciar-completar-y-registrar-no.md) | Crear, confirmar, iniciar, completar y registrar no-show. | 5 | `W3-VIS-01`, `W2-MAT-02` | `TODO` |
| [`W3-VIS-04`](wave-3-integrity-compliance/W3-VIS-04-notas-en-vivo-grabacion-consentida-y-extraccion.md) | Notas en vivo, grabación consentida y extracción de feedback. | 3 | `W3-VIS-03`, `W3-DOC-02` | `TODO` |
| [`W3-PLT-01`](wave-3-integrity-compliance/W3-PLT-01-configurar-defaults-versionados-que-guian-operaciones-y.md) | Configurar defaults versionados que guían operaciones y compliance. | 3 | `W1-PLT-01`, `W3-DOC-02` | `TODO` |
| [`W3-PLT-02`](wave-3-integrity-compliance/W3-PLT-02-definir-politicas-y-automatizaciones-starter-del-pack.md) | Definir políticas y automatizaciones starter del pack. | 2 | `W1-PLT-01` | `TODO` |

## Ola 4 — Negociación y cierre

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W4-CLS-01`](wave-4-negotiation-closing/W4-CLS-01-abrir-negociacion-ofertar-contraofertar-y-consultar-historial.md) | Abrir negociación, ofertar/contraofertar y consultar historial. | 4 | `W3-VIS-03` | `TODO` |
| [`W4-CLS-02`](wave-4-negotiation-closing/W4-CLS-02-aceptar-rechazar-retirar-o-expirar-propuestas.md) | Aceptar, rechazar, retirar o expirar propuestas. | 4 | `W4-CLS-01` | `TODO` |
| [`W4-CLS-03`](wave-4-negotiation-closing/W4-CLS-03-crear-confirmar-reserva-registrar-sena-y-resolver.md) | Crear/confirmar reserva, registrar seña y resolver expiración/cancelación. | 4 | `W4-CLS-02`, `W3-DOC-01` | `TODO` |
| [`W4-CLS-04`](wave-4-negotiation-closing/W4-CLS-04-crear-operacion-desde-reserva-directa-e-instanciar.md) | Crear operación desde reserva/directa e instanciar/completar checklist. | 4 | `W4-CLS-03`, `W3-PLT-01` | `TODO` |
| [`W4-CLS-05`](wave-4-negotiation-closing/W4-CLS-05-omitir-solicitar-aprobar-excepciones-y-avanzar-workflow.md) | Omitir/solicitar/aprobar excepciones y avanzar workflow. | 4 | `W4-CLS-04`, `W1-ORG-02` | `TODO` |
| [`W4-CLS-06`](wave-4-negotiation-closing/W4-CLS-06-cerrar-compraventa-locacion-cancelar-y-consultar-expediente.md) | Cerrar compraventa/locación, cancelar y consultar expediente integral. | 4 | `W4-CLS-05` | `TODO` |
| [`W4-DOC-01`](wave-4-negotiation-closing/W4-DOC-01-instanciar-y-completar-cumplimiento-con-excepciones-auditadas.md) | Instanciar y completar cumplimiento con excepciones auditadas. | 4 | `W3-DOC-02`, `W3-PLT-01` | `TODO` |
| [`W4-DOC-02`](wave-4-negotiation-closing/W4-DOC-02-registrar-beneficiario-final-y-reconstruir-expediente-documental.md) | Registrar beneficiario final y reconstruir expediente documental. | 2 | `W4-DOC-01` | `TODO` |
| [`W4-COMI-01`](wave-4-negotiation-closing/W4-COMI-01-crear-sobrescribir-versionar-politicas-sin-alterar-historico.md) | Crear/sobrescribir/versionar políticas sin alterar histórico. | 3 | `W3-PLT-02`, `W1-ORG-02` | `TODO` |
| [`W4-COMI-02`](wave-4-negotiation-closing/W4-COMI-02-calcular-comision-splits-ajustes-y-estimacion.md) | Calcular comisión, splits, ajustes y estimación. | 4 | `W4-COMI-01`, `W4-CLS-04` | `TODO` |
| [`W4-COMI-03`](wave-4-negotiation-closing/W4-COMI-03-analizar-rendimiento-segun-politicas-incentivos.md) | Analizar rendimiento según políticas/incentivos. | 1 | `W4-COMI-02` | `TODO` |

## Ola 5 — Operación e integraciones

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W5-SYN-01`](wave-5-operations-integrations/W5-SYN-01-publicar-listing-en-canal-y-definir-diferencias.md) | Publicar Listing en canal y definir diferencias respecto de la presentación base. | 2 | `W2-SUP-03` | `TODO` |
| [`W5-SYN-02`](wave-5-operations-integrations/W5-SYN-02-sincronizar-estado-externo-y-reintentar-fallos.md) | Sincronizar estado externo y reintentar fallos. | 2 | `W5-SYN-01` | `TODO` |
| [`W5-INT-01`](wave-5-operations-integrations/W5-INT-01-whatsapp-email-meta-y-envio-desde-crm.md) | WhatsApp/email/Meta y envío desde CRM mediante adapters. | 4 | `W2-INT-01` | `TODO` |
| [`W5-INT-02`](wave-5-operations-integrations/W5-INT-02-iniciar-registrar-llamada-grabar-con-consentimiento-y.md) | Iniciar/registrar llamada, grabar con consentimiento y transcribir. | 4 | `W3-DOC-02`, `W2-INT-01` | `TODO` |
| [`W5-INT-03`](wave-5-operations-integrations/W5-INT-03-vincular-interacciones-a-party-casos-reasignar-y.md) | Vincular interacciones a Party/casos, reasignar y adjuntar archivos. | 4 | `W5-INT-01`, `W1-PTY-01` | `TODO` |
| [`W5-INT-04`](wave-5-operations-integrations/W5-INT-04-detectar-conversaciones-que-se-enfrian.md) | Detectar conversaciones que se enfrían. | 1 | `W5-INT-01` | `TODO` |
| [`W5-RNT-01`](wave-5-operations-integrations/W5-RNT-01-crear-activar-contrato-cronograma-y-reglas-de.md) | Crear/activar contrato, cronograma y reglas de ajuste. | 4 | `W4-CLS-06` | `TODO` |
| [`W5-RNT-02`](wave-5-operations-integrations/W5-RNT-02-aplicar-ajustes-generar-receivables-y-mostrar-proximos.md) | Aplicar ajustes, generar receivables y mostrar próximos hitos. | 3 | `W5-RNT-01` | `TODO` |
| [`W5-RNT-03`](wave-5-operations-integrations/W5-RNT-03-registrar-imputar-pagos-completos-parciales-y-consultar.md) | Registrar/imputar pagos completos/parciales y consultar deuda. | 4 | `W5-RNT-02` | `TODO` |
| [`W5-RNT-04`](wave-5-operations-integrations/W5-RNT-04-detectar-y-gestionar-mora.md) | Detectar y gestionar mora. | 2 | `W5-RNT-03` | `TODO` |
| [`W5-RNT-05`](wave-5-operations-integrations/W5-RNT-05-registrar-gastos-calcular-y-emitir-liquidacion-al.md) | Registrar gastos, calcular y emitir liquidación al propietario. | 3 | `W5-RNT-03`, `W4-COMI-02` | `TODO` |
| [`W5-RNT-06`](wave-5-operations-integrations/W5-RNT-06-resolver-lifecycle-contractual-venta-con-locacion-y.md) | Resolver lifecycle contractual, venta con locación y export contable. | 4 | `W5-RNT-05` | `TODO` |
| [`W5-ANA-01`](wave-5-operations-integrations/W5-ANA-01-comparar-sucursales-metricas-de-alquiler-y-exportarlas.md) | Comparar sucursales, métricas de alquiler y exportarlas. | 3 | `W5-RNT-05`, `W2-ANA-01` | `TODO` |
| [`W5-PLT-01`](wave-5-operations-integrations/W5-PLT-01-administrar-catalogo-de-conectores-y-flags-globales.md) | Administrar catálogo de conectores y flags globales. | 2 | `W1-PLT-01` | `TODO` |

## Ola 6 — Automatización e inteligencia

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W6-AUT-01`](wave-6-automation-ai/W6-AUT-01-crear-y-activar-desactivar-automationpolicy.md) | Crear y activar/desactivar AutomationPolicy. | 2 | `W3-PLT-02` | `TODO` |
| [`W6-AUT-02`](wave-6-automation-ai/W6-AUT-02-responder-enrutar-a-especialista-y-escalar-baja.md) | Responder, enrutar a especialista y escalar baja confianza. | 3 | `W5-INT-01`, `W6-AUT-01` | `TODO` |
| [`W6-AUT-03`](wave-6-automation-ai/W6-AUT-03-sugerir-pedir-aprobacion-o-autoejecutar-con-guardrails.md) | Sugerir, pedir aprobación o autoejecutar con guardrails. | 3 | `W6-AUT-01` | `TODO` |
| [`W6-AUT-04`](wave-6-automation-ai/W6-AUT-04-disparar-automatizaciones-por-riesgo-de-sla-y.md) | Disparar automatizaciones por riesgo de SLA y cambios de matching. | 2 | `W5-INT-04`, `W2-MAT-03` | `TODO` |
| [`W6-AUT-05`](wave-6-automation-ai/W6-AUT-05-crear-notificaciones-internas-recordatorios-externos-y-retries.md) | Crear notificaciones internas, recordatorios externos y retries. | 3 | `W6-AUT-01` | `TODO` |
| [`W6-AUT-06`](wave-6-automation-ai/W6-AUT-06-auditar-decisiones-y-medir-precision-calidad-de.md) | Auditar decisiones y medir precisión/calidad de automatización. | 2 | `W6-AUT-02`, `W6-AUT-03` | `TODO` |
| [`W6-INT-01`](wave-6-automation-ai/W6-INT-01-extraer-intencion-conversacional-y-escalar-a-humano.md) | Extraer intención conversacional y escalar a humano. | 2 | `W6-AUT-02` | `TODO` |
| [`W6-ANA-01`](wave-6-automation-ai/W6-ANA-01-medir-tasaciones-y-reconstruir-proyecciones-de-forma.md) | Medir tasaciones y reconstruir proyecciones de forma segura. | 2 | `W3-SUP-01`, `W2-ANA-01` | `TODO` |
| [`W6-ANA-02`](wave-6-automation-ai/W6-ANA-02-consultas-naturales-con-permisos-fuentes-y-explicacion.md) | Consultas naturales con permisos, fuentes y explicación. | 2 | `W6-AUT-06`, `W2-ANA-02` | `TODO` |

## Ola 7 — Expansión opcional y backoffice avanzado

| Task | Objetivo | UC | Depende de | Estado |
|---|---|---:|---|---|
| [`W7-PRP-01`](wave-7-optional-expansion/W7-PRP-01-operaciones-complejas-sobre-propiedades-sin-perder-historia.md) | Operaciones complejas sobre propiedades sin perder historia. | 3 | `W3-PRP-01` | `TODO` |
| [`W7-MNT-01`](wave-7-optional-expansion/W7-MNT-01-abrir-priorizar-y-asignar-reclamos.md) | Abrir, priorizar y asignar reclamos. | 3 | `W5-RNT-01` | `TODO` |
| [`W7-MNT-02`](wave-7-optional-expansion/W7-MNT-02-solicitar-y-decidir-presupuestos.md) | Solicitar y decidir presupuestos. | 3 | `W7-MNT-01` | `TODO` |
| [`W7-MNT-03`](wave-7-optional-expansion/W7-MNT-03-completar-trabajo-cerrar-consultar-historia-y-desactivar.md) | Completar trabajo, cerrar, consultar historia y desactivar módulo. | 4 | `W7-MNT-02` | `TODO` |
| [`W7-POR-01`](wave-7-optional-expansion/W7-POR-01-autoservicio-de-inmuebles-liquidaciones-documentos-y-aprobaciones.md) | Autoservicio de inmuebles, liquidaciones, documentos y aprobaciones. | 4 | `W5-RNT-05`, `W7-MNT-02` | `TODO` |
| [`W7-POR-02`](wave-7-optional-expansion/W7-POR-02-contrato-canon-pagos-y-mantenimiento.md) | Contrato, canon, pagos y mantenimiento. | 4 | `W5-RNT-03`, `W7-MNT-01` | `TODO` |
| [`W7-POR-03`](wave-7-optional-expansion/W7-POR-03-permitir-confirmar-visita-o-criterio-accion-mediante.md) | Permitir confirmar visita o criterio/acción mediante link/portal. | 2 | `W3-VIS-03`, `W2-DEM-02` | `TODO` |
| [`W7-PLT-01`](wave-7-optional-expansion/W7-PLT-01-versionar-deprecar-y-previsualizar-impacto.md) | Versionar/deprecar y previsualizar impacto. | 3 | `W1-PLT-01` | `TODO` |
| [`W7-PLT-02`](wave-7-optional-expansion/W7-PLT-02-pilotos-y-extensionschema-versionado.md) | Pilotos y ExtensionSchema versionado. | 3 | `W7-PLT-01` | `TODO` |
| [`W7-PLT-03`](wave-7-optional-expansion/W7-PLT-03-correcciones-auditadas-auditoria-y-promocion-entre-ambientes.md) | Correcciones auditadas, auditoría y promoción entre ambientes. | 3 | `W7-PLT-01`, `FND-006` | `TODO` |

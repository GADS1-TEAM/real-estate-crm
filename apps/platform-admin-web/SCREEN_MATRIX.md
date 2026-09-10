# Matriz de superficies — Platform Admin

El registro ejecutable es la fuente de verdad de esta matriz. Cada entrada tiene un `renderKey` reutilizable; no existe una página duplicada por superficie.

| ID | Superficie | Journey | Ruta | Estado principal |
| --- | --- | --- | --- | --- |
| PA-001 | Overview | — | `/` | Disponible |
| PA-002 | Centro de atención | — | `/centro-de-atencion` | Atención |
| PA-010 | Lista de packs | — | `/packs` | Disponible |
| PA-011 | Detalle de pack | — | `/packs/argentina-base` | Publicado |
| PA-012 | Versión de pack | — | `/packs/argentina-base/versiones/v8` | Solo lectura |
| PA-013 | Editor de borrador de pack | J1 | `/packs/argentina-base/versiones/v8/editar` | Bloqueado |
| PA-014 | Diff de pack | J1 | `/packs/argentina-base/versiones/v8/comparar` | Diferencias |
| PA-015 | Revisión de publicación | J1 | `/packs/argentina-base/versiones/v8/publicar` | Cuatro pasos |
| PA-016 | Vista previa de impacto | J6 | `/release/impacto/argentina-base-v8` | Impacto |
| PA-017 | Deprecar pack | J6 | `/packs/argentina-base/deprecar` | Restringido |
| PA-020 | Lista de capabilities | — | `/capabilities` | Disponible |
| PA-021 | Detalle de capability | — | `/capabilities/maintenance` | Piloto |
| PA-022 | Grafo de dependencias | — | `/capabilities/maintenance/dependencias` | Relaciones |
| PA-023 | Compatibilidad | — | `/capabilities/maintenance/compatibilidad` | Compatible |
| PA-030 | Lista de catálogos base | — | `/catalogos-base` | Disponible |
| PA-031 | Detalle de catálogo | — | `/catalogos-base/property-types` | Publicado |
| PA-032 | Editor de versión de catálogo | — | `/catalogos-base/property-types/versiones/v6/editar` | Borrador |
| PA-033 | Diff de catálogo | — | `/catalogos-base/property-types/comparar` | Diferencias |
| PA-034 | Panel de validación | — | `/validacion` | Bloqueos y avisos |
| PA-040 | Lista de workflows | — | `/workflows` | Disponible |
| PA-041 | Detalle de workflow | — | `/workflows/residential-sale` | Publicado |
| PA-042 | Editor de workflow | J3 | `/workflows/residential-sale/v7/editar` | Bloqueado |
| PA-043 | Vista previa de workflow | — | `/workflows/residential-sale/v7/previsualizar` | Previsualización |
| PA-044 | Diff de workflow | — | `/workflows/residential-sale/comparar` | Diferencias |
| PA-050 | Lista de requisitos documentales | — | `/requisitos` | Disponible |
| PA-051 | Matriz de requisitos | J4 | `/requisitos/matriz` | Solapamientos |
| PA-052 | Editor de requisito | J4 | `/requisitos/nuevo` | Borrador |
| PA-053 | Detalle de requisito | — | `/requisitos/identity-document` | Publicado |
| PA-060 | Lista de políticas de compliance | — | `/compliance` | Disponible |
| PA-061 | Editor de política de compliance | — | `/compliance/kyc/editar` | Borrador |
| PA-062 | Simulador de compliance | — | `/compliance/kyc/simular` | Simulación |
| PA-063 | Detalle de política de compliance | — | `/compliance/kyc` | Publicado |
| PA-070 | Lista de políticas de comisión | — | `/comisiones` | Disponible |
| PA-071 | Detalle de política de comisión | — | `/comisiones/sale-default` | Publicado |
| PA-072 | Editor de reglas de comisión | J2 | `/comisiones/sale-default/reglas` | Borrador |
| PA-073 | Editor de splits | J2 | `/comisiones/sale-default/splits` | Bloqueado |
| PA-074 | Simulador de comisión | J2 | `/comisiones/sale-default/simular` | Simulación |
| PA-075 | Diff de política de comisión | — | `/comisiones/sale-default/comparar` | Diferencias |
| PA-080 | Lista de automatizaciones | — | `/automatizaciones` | Disponible |
| PA-081 | Editor de automatización | J5 | `/automatizaciones/lead-routing/editar` | Bloqueado |
| PA-082 | Simulador de automatización | J5 | `/automatizaciones/lead-routing/simular` | Simulación |
| PA-083 | Detalle de automatización | — | `/automatizaciones/lead-routing` | Publicado |
| PA-090 | Lista de métricas | — | `/metricas` | Disponible |
| PA-091 | Editor de métrica | — | `/metricas/close-rate/editar` | Borrador |
| PA-092 | Lineage de métrica | — | `/metricas/close-rate/lineage` | Explicable |
| PA-093 | Detalle de métrica | — | `/metricas/close-rate` | Publicado |
| PA-100 | Catálogo de conectores | — | `/conectores` | Disponible |
| PA-101 | Detalle de conector | — | `/conectores/whatsapp` | Publicado |
| PA-102 | Feature flags | — | `/feature-flags` | Auditable |
| PA-110 | Lista de Extension Schemas | — | `/extension-schemas` | Disponible |
| PA-111 | Editor de schema | — | `/extension-schemas/property/editar` | Borrador |
| PA-112 | Vista previa de schema | — | `/extension-schemas/property/previsualizar` | Previsualización |
| PA-113 | Diff de schema | — | `/extension-schemas/property/comparar` | Diferencias |
| PA-120 | Impacto de un cambio | — | `/release/impacto` | Impacto |
| PA-121 | Lista de pilotos | J7 | `/release/pilotos` | Pilotos |
| PA-122 | Detalle de piloto | J7 | `/release/pilotos/rental-administration` | Dependencia |
| PA-123 | Crear piloto | J7 | `/release/pilotos/nuevo` | Formulario |
| PA-124 | Rollout | J7 | `/release/pilotos/rental-administration/rollout` | En curso |
| PA-125 | Ambientes | J9 | `/release/ambientes` | Comparación |
| PA-126 | Comparar ambientes | J9 | `/release/ambientes/comparar` | Diferencias |
| PA-127 | Promoción | J9 | `/release/ambientes/promover` | Bloqueado |
| PA-130 | Buscar inmobiliaria | J8 | `/diagnostico/inmobiliarias` | Consulta |
| PA-131 | Configuración de inmobiliaria | J8 | `/diagnostico/inmobiliarias/inmobiliaria-norte` | Solo lectura parcial |
| PA-132 | Inspector de configuración efectiva | J8 | `/diagnostico/inmobiliarias/inmobiliaria-norte/configuracion-efectiva` | Solo lectura |
| PA-140 | Registro de auditoría | — | `/auditoria` | Auditable |
| PA-141 | Detalle de auditoría | — | `/auditoria/entry-2026-0910` | Inmutable |
| PA-142 | Catálogo de correcciones | J10 | `/correcciones` | Restringido |
| PA-143 | Flujo de corrección | J10 | `/correcciones/reprocess-compliance` | Permiso + motivo |
| PA-150 | Búsqueda global | — | `/buscar` | Disponible |
| PA-151 | Historial de cambios de producto | — | `/historial-producto` | Histórico |
| PA-152 | Sin permiso | — | `/sin-permiso` | Explicativo |
| PA-153 | VersionHeader | — | `/version-header` | Transversal |


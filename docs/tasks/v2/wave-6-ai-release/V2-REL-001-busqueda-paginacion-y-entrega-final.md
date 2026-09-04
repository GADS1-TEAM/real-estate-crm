# V2-REL-001 — Búsqueda, paginación, regresión y entrega final

- **Ola:** 6 — IA y entrega
- **Estado:** TODO
- **Dependencias:** V2-SCP-001, V2-UX-001, V2-FND-001, V2-FND-002, V2-FND-003, V2-ACL-001, V2-CAT-001, V2-PTY-001, V2-PRP-001, V2-DMD-001, V2-MAT-001, V2-PIPE-001, V2-COM-001, V2-ACT-001, V2-ANA-001, V2-AI-001
- **UCs:** REL-001, REL-002, REL-003, REL-004, REL-005
- **Owner:** integración y calidad de entrega
- **Write zone:** tests e2e/contract, documentación de entrega y ajustes de integración sin cambiar ownership de dominio

## Resultado esperado

El CRM final permite buscar, filtrar y paginar Empresas, Contactos, Listings y
Oportunidades visibles, y se puede demostrar de extremo a extremo en las fechas
de entrega sin activar módulos fuera de scope.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| REL-001 | Vendedor/Responsable | Buscar y paginar Empresas y Contactos. | BFF + owners | API/e2e. |
| REL-002 | Vendedor/Responsable | Buscar y paginar Listings/Productos. | BFF + supply-service | API/e2e. |
| REL-003 | Vendedor/Responsable | Filtrar Oportunidades por responsable, etapa, estado y origen. | operations-bff + analytics-service | Board/e2e. |
| REL-004 | Equipo | Ejecutar regresión de la primera entrega y del flujo final. | tests | Checklist con resultados reales. |
| REL-005 | Equipo | Preparar demo y trazabilidad frente a los PDF. | documentación | Matriz de cobertura y guion. |

## Interfaces

- Queries: SearchCompanies, SearchContacts, SearchListings y
  SearchOpportunities.
- Parámetros comunes: q, page, pageSize, sort, status y filtros propios del
  recurso.
- Response común: items, page, pageSize, total y hasNext.
- No se agregan endpoints de exportación ni integraciones de terceros.

## Reglas

- pageSize tiene máximo documentado y las consultas usan índices reales.
- Filtros combinados no pierden autorización por rol.
- El reload de la primera entrega lee de MongoDB y no de estado local del
  navegador.
- No se usa localStorage como source of truth.
- La demo debe mostrar actividades solo en la etapa final; no se simulan como
  requisito de la primera entrega.
- Se debe comprobar explícitamente que no existen agenda, Task, Notification,
  email/WhatsApp outbound, portal, integración, pago o multi-tenancy en el
  recorrido.

## Criterios de aceptación

- [ ] Empresas y Contactos tienen búsqueda, orden y paginación.
- [ ] Listings/Productos tienen búsqueda por operación, tipo y estado.
- [ ] El tablero filtra por responsable, etapa, estado y origen.
- [ ] La primera entrega completa login → Empresa → Contacto → Listing →
  oportunidad → board → cambio de etapa → reload.
- [ ] La entrega final completa roles → catálogos → Party → Property/Listing →
  Requirement/Captation → visita/negociación/reserva/cierre → actividad/
  historial → métricas → IA.
- [ ] Se ejecutan tests unitarios, integración, contrato y e2e con resultados
  registrados.
- [ ] La matriz de cobertura relaciona los casos mínimos de los PDF con una
  task y una evidencia.

## Overrides POC

- Testcontainers o servicios Docker locales son suficientes; no se requiere
  infraestructura externa.
- Los datos de demo son fixtures reproducibles y no se mezclan con producción.
- La funcionalidad IA puede reportarse no disponible si falta configuración,
  siempre que sus tests y flujo revisable estén documentados.

## Definition of Done

- [ ] Búsqueda/filtros/paginación en verde.
- [ ] Regresión de primera entrega y final en verde.
- [ ] Guion de demo reproducible.
- [ ] Matriz de cobertura y lista de exclusiones actualizadas.
- [ ] Ningún ajuste de integración cambió un owner o agregó un módulo fuera
  de scope.

## Evidencia requerida

1. Salidas de los comandos de test/build/lint/contract.
2. Capturas de listados, tablero, detalle, historial, dashboard e IA.
3. Guion de demo de 24/09 y 12/11.
4. Matriz PDF → UC → task → test/evidencia.

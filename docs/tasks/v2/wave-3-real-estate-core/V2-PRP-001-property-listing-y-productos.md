# V2-PRP-001 — Property, propietario, Listing y productos inmobiliarios

- **Ola:** 3 — Núcleo inmobiliario
- **Estado:** TODO
- **Dependencias:** V2-PTY-001, V2-CAT-001
- **UCs:** PRP-001, PRP-002, PRP-003, PRP-004, PRP-005, PRP-006
- **Owner:** property-service y supply-service
- **Write zone:** Property/PropertyInterest en property-service; Listing en supply-service; pantalla de productos inmobiliarios

## Resultado esperado

El CRM representa el inmueble físico separado de su oferta comercial. Puede
registrar propiedades urbanas y rurales, vincular propietarios, crear listings
de venta o alquiler y mostrar esos listings como el catálogo de productos
inmobiliarios seleccionables en una oportunidad.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| PRP-001 | Administrador/Vendedor | Crear una Property urbana con ubicación y tipo mínimos. | property-service | Command + query. |
| PRP-002 | Administrador/Vendedor | Crear una Property rural sin forzar atributos urbanos. | property-service | Modelo rural + test. |
| PRP-003 | Administrador/Vendedor | Vincular uno o más ownerPartyId mediante PropertyInterest. | property-service | Relación visible. |
| PRP-004 | Administrador/Vendedor | Crear y modificar un Listing asociado a Property. | supply-service | CRUD + regla Property != Listing. |
| PRP-005 | Administrador/Vendedor | Activar/pausar/cerrar Listing con operación y términos. | supply-service | State transition test. |
| PRP-006 | Vendedor | Consultar el listado de productos inmobiliarios para seleccionarlo. | supply-service/read model | GET /products y filtros. |

## Modelo mínimo

~~~text
Property
- propertyId
- propertyTypeCode
- location
- surfaceM2? / surfaceHa?
- physicalAttributes?
- ruralAttributes?
- lifecycleStatus
- createdAt
- updatedAt
- version

PropertyInterest
- interestId
- propertyId
- holderPartyId
- rightType: OWNER | CO_OWNER | USUFRUCTUARY
- participation?
- validFrom
- validTo?
- status

Listing
- listingId
- propertyId
- operationTypeCode
- canonicalTitle
- canonicalDescription?
- commercialTerms
- availability
- responsibleUserId?
- status: DRAFT | READY | ACTIVE | PAUSED | CLOSED | WITHDRAWN | EXPIRED
- catalogVersion
- version
~~~

## Interfaces

- Property commands: CreateProperty, UpdateProperty, AddPropertyInterest.
- Listing commands: CreateListing, UpdateListing, ActivateListing,
  PauseListing, CloseListing.
- Queries: SearchProperties, GetPropertyDetail, SearchListings y
  GetProductCatalog.
- Events v1: PropertyRegistered, PropertyUpdated, PropertyInterestAdded,
  ListingCreated, ListingActivated, ListingPaused, ListingClosed.
- Consume: Party reference contract de V2-PTY-001 y catálogos de V2-CAT-001.
- Produce: ListingReference para V2-DMD-001, V2-MAT-001 y V2-PIPE-001.

## Reglas

- Property es el activo físico; Listing es su oferta comercial.
- Una Property puede tener muchos listings históricos y simultáneos según
  operación; cerrar un Listing no elimina Property.
- Un Listing puede requerir al menos un PropertyInterest activo cuando la
  política de comercialización lo marque; no se inventa una documentación
  legal completa.
- Los tipos urbanos y rurales usan campos específicos; no se reduce campo a
  un terreno con superficie en m2.
- OperationType y PropertyType vienen de catálogos, no de strings libres.
- No se publica en portales ni se sincroniza con canales externos.

## Criterios de aceptación

- [ ] Se crea y consulta una Property urbana.
- [ ] Se crea y consulta una Property rural con atributos rurales opcionales.
- [ ] La propiedad puede vincular dos propietarios sin un ownerId único.
- [ ] Se crean dos Listings para la misma Property con operaciones distintas.
- [ ] El cierre de un Listing conserva Property y su historial.
- [ ] El listado de productos devuelve Listings seleccionables para una
  oportunidad.
- [ ] Un Vendedor no puede activar un Listing con Property inexistente.

## Overrides POC

- El primer corte puede precargar Listings/productos para la demo.
- MongoDB usa índices por tipo, ubicación, operationTypeCode y status.
- No se implementan ChannelPublication, portales, documentos, pagos ni
  syndication.

## Definition of Done

- [ ] Property y Listing tienen owners de datos separados.
- [ ] Relación PropertyInterest persistida.
- [ ] CRUD, lifecycle y consultas testeados.
- [ ] Catálogos y eventos documentados.

## Evidencia requerida

1. Registro de una Property urbana y una rural.
2. Dos listings de una misma Property.
3. Consulta de productos desde el BFF.
4. Test de cierre de Listing sin borrado de Property.

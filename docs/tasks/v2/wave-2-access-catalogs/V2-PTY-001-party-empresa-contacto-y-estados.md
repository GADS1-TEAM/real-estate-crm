# V2-PTY-001 — Party, Empresa, Contacto, relaciones y estados

- **Ola:** 2 — Acceso, Party y catálogos
- **Estado:** TODO
- **Dependencias:** V2-FND-002, V2-ACL-001, V2-CAT-001
- **UCs:** PTY-001, PTY-002, PTY-003, PTY-004, PTY-005, PTY-006, PTY-007
- **Owner:** party-service
- **Write zone:** party-service, formularios Empresa/Contacto y contratos de Party

## Resultado esperado

El CRM diferencia Empresa y Contacto en la experiencia, pero mantiene una
Party única por identidad. Se pueden crear, modificar, relacionar, consultar,
asignar y pasar a baja lógica sin perder historial.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| PTY-001 | Administrador/Vendedor | Crear una Empresa como Party LEGAL_ENTITY. | party-service | Command y detalle. |
| PTY-002 | Administrador/Vendedor | Modificar datos de Empresa sin duplicar identidad. | party-service | Update test. |
| PTY-003 | Administrador/Vendedor | Crear un Contacto como Party NATURAL_PERSON. | party-service | Command y detalle. |
| PTY-004 | Administrador/Vendedor | Modificar Contacto y conservar su relación histórica. | party-service | Update test. |
| PTY-005 | Vendedor/Responsable | Relacionar Contacto con Empresa o dejarlo individual. | party-service | PartyRelationship. |
| PTY-006 | Usuario autorizado | Consultar detalle de Empresa o Contacto. | party-service/read model | Query contract. |
| PTY-007 | Administrador/Responsable | Cambiar estado comercial y responsable con baja lógica. | party-service + access-service | Audit/event test. |

## Modelo mínimo

~~~text
Party
- partyId
- kind: LEGAL_ENTITY | NATURAL_PERSON
- displayName
- legalName? / givenNames? / familyNames?
- taxIdentifier?
- identityDocument?
- email?
- phone?
- address?
- industry?
- identityStatus: PROVISIONAL | ACTIVE | ALIASED | INACTIVE | RESTRICTED
- commercialStatus: POTENTIAL | CUSTOMER | INACTIVE | DO_NOT_CONTACT
- responsibleUserId?
- originCode?
- notes?
- createdAt
- updatedAt
- version

PartyRelationship
- relationshipId
- fromPartyId
- toPartyId
- relationshipType: CONTACT_OF | REPRESENTS
- validFrom
- validTo?
- createdBy
~~~

## Interfaces

- Commands: CreateCompany, UpdateCompany, CreateContact, UpdateContact,
  RelateContactToCompany, ChangePartyCommercialStatus y
  AssignPartyResponsible. El ciclo `identityStatus` se conserva como estado
  técnico del owner; no se implementa Identity Resolution automática.
- Queries: SearchParties, GetCompanyDetail y GetContactDetail.
- Events v1: PartyRegistered, PartyUpdated, PartyRelationshipCreated,
  PartyCommercialStatusChanged y PartyResponsibleAssigned. Si el owner cambia
  el ciclo técnico, el evento separado es PartyIdentityStatusChanged.
- El origen usa CommercialOrigin de V2-CAT-001.

## Reglas

- Empresa no es Organization/tenant; es una Party corporativa del negocio.
- Contacto puede existir sin Empresa.
- Una Empresa puede tener muchos Contactos y un Contacto puede tener una
  relación vigente con una o más Empresas si el dominio lo permite.
- `identityStatus` y `commercialStatus` son dimensiones independientes. Una
  Party puede ser `ACTIVE + POTENTIAL` o `ACTIVE + DO_NOT_CONTACT`.
- El alta normal de V2 usa `identityStatus=ACTIVE` y
  `commercialStatus=POTENTIAL`; `PROVISIONAL`, `ALIASED` y `RESTRICTED` quedan
  disponibles para el ciclo técnico del owner y no se convierten en estados
  comerciales.
- POTENTIAL, CUSTOMER, INACTIVE y DO_NOT_CONTACT son los cuatro estados
  comerciales requeridos para V2.
- INACTIVE y DO_NOT_CONTACT no borran ni ocultan la historia.
- Los campos opcionales se muestran como faltantes y no bloquean la captura
  mínima; el formulario exige solo nombre y el dato de contacto definido por
  la regla de alta.
- No se implementa Identity Resolution automática ni se genera ALIASED desde
  la UI; el estado técnico queda reservado para la evolución del owner.

## Criterios de aceptación

- [ ] Se pueden crear y modificar Empresa y Contacto desde pantallas distintas.
- [ ] Un Contacto se puede relacionar con una Empresa y ver esa relación en
  ambos detalles.
- [ ] Un Contacto individual no requiere Empresa.
- [ ] Los cuatro estados comerciales se pueden consultar y las etiquetas son claras.
- [ ] `identityStatus` y `commercialStatus` se persisten y no se confunden en
  commands, queries ni UI.
- [ ] Un registro con actividad u oportunidad no se elimina físicamente.
- [ ] El responsable y el origen se pueden consultar desde el detalle.
- [ ] Se conserva actor y fecha en cambios de datos sensibles y estado.

## Overrides POC

- No hay tenantId, organizationId, sucursales ni resolución de duplicados.
- Se permite un índice MongoDB local para nombre, email, teléfono y CUIT.
- La vista 360 completa se compone en V2-ANA-001; esta task entrega detalle
  base y relaciones propias.

## Definition of Done

- [ ] CRUD de Empresa/Contacto.
- [ ] PartyRelationship persistida y consultable.
- [ ] Estados técnicos/comerciales, baja lógica y no borrado cubiertos por tests.
- [ ] Permisos y eventos documentados.

## Evidencia requerida

1. Flujo Empresa → Contacto → detalle de ambos.
2. Flujo Contacto individual.
3. Test de estado DO_NOT_CONTACT sin borrado.
4. Test de independencia ACTIVE + POTENTIAL frente a ACTIVE + DO_NOT_CONTACT.
5. Request de baja lógica con actor y fecha.

## Handoff

**Qué quedó (rama `feat/v2-pty-001-party`, sin marcar DONE):**

- `party-service` completo: aggregates `Party` y `PartyRelationship`, los 7 commands y 3 queries de la
  task, eventos v1 por outbox (`PartyRegistered`, `PartyUpdated`, `PartyRelationshipCreated`,
  `PartyCommercialStatusChanged`, `PartyResponsibleAssigned` con `ResponsibleAssignedV1`), base
  `crm_party`, concurrencia optimista (409), baja lógica, sin unicidad ni deduplicación.
- Regla de propiedad evaluada en party-service (D2), **solo para `parties.write`** (UpdateCompany,
  UpdateContact, RelateContactToCompany): Administrador edita todo; Vendedor y Responsable
  Comercial solo donde son `responsibleUserId`. `parties.change_commercial_status` y
  `parties.assign_responsible` dependen únicamente del permiso (Administrador y Responsable
  Comercial); la asignación además la valida access-service.
- Contrato nuevo de access-service: `GET /api/v1/users/me` (`UserSelfV1`) + `IUserDirectoryPort`/
  `HttpUserDirectoryPort` (caché 60 s por `sub`), autorizado y aditivo.
- BFF: screens `PTY-01/05/06/07/08/09/10/11/13` y `GLB-11`; mutations de Party, en los controllers
  existentes.
- Evidencia end-to-end de ASSIGN-001/002 (reasignación → evento en RabbitMQ real).

**Desvío explícito de la task:** solo el nombre es obligatorio (alineado con `crm-web`), no
"nombre + dato de contacto".

**Qué falta / decisiones abiertas** (detalle en `IMPLEMENTATION_REPORT-V2-PTY-001.md`):

- Evento de reasignación confirmado: `PartyResponsibleAssigned` con payload `ResponsibleAssignedV1`.
- **PR "fix" pendiente antes de Wave 3:** (1) mover `BearerTokenRelayHandler` a
  `BuildingBlocks.Infrastructure` y usarlo en `HttpCatalogReaderPort`; (2) aislar la base Mongo en
  los tests de ACL/CAT (`UseSetting` en vez de `ConfigureAppConfiguration`).
- `HttpCatalogReaderPort` (CAT-001) no reenvía el Bearer y el GET de catálogos exige JWT: party-service
  lo resuelve localmente con `BearerTokenRelayHandler`; la corrección de fondo es de CAT.
- Los tests de ACL/CAT no aíslan la base Mongo (`ConfigureAppConfiguration` no aplica a
  `AddMongoPersistence`); los de PTY usan `UseSetting`.
- Sin tests automáticos del BFF ni corrida e2e con los cuatro servicios reales (solo se levantan
  Mongo/RabbitMQ/Keycloak en `test-integration.sh`).
- Sin cierre de relaciones (`validTo`), sin `run-slice`, `crm-web` sigue en modo demo (ids string).

**Cómo verificar:**

```bash
bash scripts/test-fast.sh          # sin Docker
bash scripts/test-integration.sh   # Docker: mongo, rabbitmq, keycloak
# manual, 4 procesos (ver DEVELOPMENT.md, sección party-service):
dotnet run --project services/access-service/src/AccessService.Api
dotnet run --project services/platform-config-service/src/PlatformConfigService.Api
dotnet run --project services/party-service/src/PartyService.Api
dotnet run --project bffs/operations-bff/src/OperationsBff.Api
```

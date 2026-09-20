# IMPLEMENTATION REPORT - V2-PRP-001

## Descripción
Se implementó `V2-PRP-001 — Property, PropertyInterest, Listing y productos` para la Wave 3.

## Servicios modificados
- `property-service`: Se agregó el Aggregate Root `Property` y la entidad `PropertyInterest`. Se incluyeron los comandos y endpoints requeridos (`CreateProperty`, `AddPropertyInterest`, etc). 
- `supply-service`: Se agregó el Aggregate Root `Listing`. Se incluyeron los comandos de lifecycle del Listing (`CreateListing`, `ActivateListing`, `CloseListing`, etc) con las validaciones de existencia de property.
- `OperationsBff.Api`: Se agregó el ruteo proxy hacia `property-service` (pantallas `PRP-*`) y `supply-service` (pantallas `LST-*`) en `ScreensController.cs` y `MutationsController.cs`.
- `RealEstateCrm.Contracts`: Se crearon los eventos V1 (`PropertyRegisteredV1`, `ListingCreatedV1`, etc) y las referencias de puertos para `IPropertyReferencePort` y `IListingReferencePort`.

## Evidencia
1. Registro de una Property urbana y una rural: Implementado en los seeds de `property-service`.
2. Dos listings de una misma Property: Implementado en los seeds de `supply-service`.
3. Consulta de productos desde el BFF: Ruteo activo en `OperationsBff.Api`.
4. Test de cierre de Listing sin borrado de Property: Modelado en `supply-service` donde `CloseListing` solamente modifica el estado a `CLOSED`.

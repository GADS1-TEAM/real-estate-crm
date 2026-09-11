# Contrato frontend para `platform-admin-bff`

La aplicación no conoce entidades internas ni consulta MongoDB. Todas las lecturas y mutaciones productivas atraviesan `PlatformAdminDataSource` y su implementación `BffPlatformAdminDataSource`.

## Contexto de la solicitud

El cliente envía cookies de sesión con `credentials: include` y agrega:

- `x-correlation-id`: UUID por solicitud para trazabilidad.
- `x-platform-environment`: ambiente seleccionado en la interfaz.
- `Content-Type: application/json` en requests con payload.

El usuario, la sesión y los permisos no se reciben desde un selector del cliente: el BFF debe resolverlos desde OIDC/sesión y devolver solo los permisos efectivos.

## Operaciones

| Método | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/overview` | Artefactos visibles, impacto y snapshot de sesión |
| `PATCH` | `/drafts/field` | Autosave de un campo del draft |
| `POST` | `/drafts/{artifactId}/validate` | Validación del draft e issues explicables |
| `POST` | `/publish` | Publicación con versión, warnings aceptados y confirmación |
| `POST` | `/deprecate` | Deprecación no destructiva con motivo |
| `POST` | `/corrections/execute` | Corrección autorizada, auditable y con target |

## Respuesta común

`GET /overview` y las validaciones devuelven un `PlatformSnapshot`:

```ts
type PlatformSnapshot = {
  artifacts: ArtifactSummary[];
  impact: ImpactSummary;
  state: ReviewState;
};
```

Los errores públicos deben responder con HTTP no exitoso y un código estable. El cliente los expone como `PlatformDataSourceError` (`PLATFORM_BFF_4xx`, `PLATFORM_BFF_5xx` o `PLATFORM_BFF_UNAVAILABLE`) sin cambiar a datos locales.

## Invariantes consumidos por la UI

- Una versión publicada es de solo lectura.
- Editar una publicación crea el único draft activo del artefacto.
- Guardar no publica.
- Publicar no migra instancias históricas.
- Deprecar no elimina.
- Overrides de tenant se inspeccionan, pero no se editan desde Platform Admin.
- `UNKNOWN` es distinto de `false`, `0` y una colección vacía.
- Una acción sin permiso permanece visible, deshabilitada y explica el permiso necesario.

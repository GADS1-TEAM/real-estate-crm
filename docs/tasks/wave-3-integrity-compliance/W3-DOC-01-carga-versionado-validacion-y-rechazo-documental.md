# W3-DOC-01 — Carga, versionado, validación y rechazo documental

**Dependencias:** `FND-004`, `FND-006`  
**Casos de uso:** `DOC-001`, `DOC-002`, `DOC-003`, `DOC-004`

## Casos de uso

- `DOC-001` **Cargar documento** (Agente/Cliente): subir archivo mediante Object Storage y crear Document/DocumentVersion vinculado a Party, Property, Transaction u otro subject. Servicios: documents-compliance-service + asset adapter. Evento `DocumentUploaded`.
- `DOC-002` **Reemplazar versión documental**: agregar nueva versión manteniendo anterior como evidencia; `DocumentVersionReplaced`.
- `DOC-003` **Validar documento**: marcar versión válida para propósito aplicable tras controles humanos/automáticos; `DocumentValidated`.
- `DOC-004` **Rechazar documento**: rechazar con motivo y permitir corrección sin borrar evidencia; `DocumentRejected`.

## Reglas

Archivos binarios fuera del documento Mongo; metadata/versiones auditables. Datos/documentos solo obligatorios por acción/norma contextual. Aplicar tenancy, permissions y minimización de PII.

## DoD
- [ ] Versionado inmutable probado.
- [ ] Validar/rechazar autorizados.
- [ ] Storage adapter reemplazable.

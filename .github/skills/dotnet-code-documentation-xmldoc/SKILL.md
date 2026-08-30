---
name: dotnet-code-documentation-xmldoc
description: |
  Activa cuando se escribe o modifica código C#/.NET en un microservicio ASP.NET Core y hay
  que documentarlo con XML documentation comments. Triggers: "documentá", "XML doc", "///",
  "<summary>", "<param>", "<returns>", "<exception>", "<inheritdoc/>", crear o exponer una
  clase/interface pública, un método público, un Controller, un servicio, un DTO o un tipo
  compartido. Garantiza XML doc obligatorio en API pública nueva, sugerido en código
  existente que se entiende, y prohíbe comentarios triviales o que inventan comportamiento.
  NO activar para cambios cosméticos (formato, typos), ni para lógica interna de miembros
  privados obvios (eso son comentarios //, no XML doc).
---

# Documentación de código con XML doc comments (ASP.NET Core)

## Objetivo

Que la API pública del microservicio exponga contratos claros mediante XML documentation
comments (`///`): qué hace un tipo/miembro, qué recibe, qué devuelve, qué excepciones lanza
y qué efectos tiene. Documentar el **contrato y el porqué**, no repetir el nombre del símbolo.

## Cuándo activar

- Se crea o expone una `class`/`interface`/`record`/`enum` público.
- Se define un método público de un Controller, servicio, repositorio o helper.
- Se define un DTO, un tipo de dominio o una excepción tipada compartida.
- Se toca código existente cuyo comportamiento se comprende con certeza.

## Cuándo NO activar

- Cambios cosméticos (formato, renombres triviales, typos).
- Miembros privados y lógica interna obvia (comentarios `//`, no XML doc).
- Código existente cuyo comportamiento NO se entiende con certeza (ver MUST NOT 8).

## Decisiones del proyecto

- Formato: comentarios `///` con etiquetas XML. Etiquetas frecuentes: `<summary>`,
  `<param>`, `<returns>`, `<exception>`, `<remarks>`, `<see cref="..."/>`,
  `<inheritdoc/>` (para no duplicar docs de interfaces).
- Idioma: español; términos técnicos pueden quedar en inglés.

## Reglas obligatorias (MUST / MUST NOT)

### MUST

1. **MUST documentar con XML doc toda API pública nueva**: tipos públicos, métodos
   públicos, DTOs y excepciones tipadas compartidas.
2. **MUST describir el contrato**: `<summary>` con el propósito, `<param>` de parámetros no
   obvios, `<returns>` y `<exception>` para los errores que lanza.
3. **MUST documentar efectos y contratos no obvios**: llamadas de red, acceso a DB (EF Core),
   uso de `CancellationToken`, thread-safety, unidades, rangos válidos e invariantes.
4. **MUST usar `<inheritdoc/>`** en implementaciones cuya interface ya está documentada, en
   lugar de duplicar; y `[Obsolete]` + `<remarks>` al obsoletar algo.

### MUST NOT

5. **MUST NOT documentar lo trivial**: propiedades autoexplicativas o `<summary>` que solo
   repite el nombre del miembro.
6. **MUST NOT limitarse a repetir la firma** (tipos y nombres de params sin aportar
   significado).
7. **MUST NOT dejar XML doc desactualizado**: si cambia la firma o el comportamiento, se
   actualiza en el mismo cambio.
8. **MUST NOT inventar comportamiento**: si el código existente no se entiende con certeza,
   NO se le agrega XML doc adivinado.

## Recomendaciones (SHOULD)

- **SHOULD documentar código existente que se toca**, solo si se comprende bien qué hace.
- SHOULD usar `<see cref="..."/>` para enlazar tipos y miembros relacionados.
- SHOULD documentar la propagación de `CancellationToken` y el comportamiento ante cancelación.

## Anti-patrones prohibidos

Código malo:

```csharp
/// <summary>Obtiene el cliente.</summary>
/// <param name="id">El id.</param>
/// <returns>El cliente.</returns>
public Task<Cliente> GetCliente(string id) { /* ... */ }
```

Código bueno:

```csharp
/// <summary>
/// Busca un cliente por su CUIL consultando el core bancario vía named client EPA.
/// </summary>
/// <param name="cuil">CUIL sin guiones (11 dígitos), validado en el borde.</param>
/// <param name="ct">Token de cancelación propagado hasta el HttpClient.</param>
/// <returns>El cliente, o <c>null</c> si el core responde 404.</returns>
/// <exception cref="UpstreamTimeoutException">Si el core supera el timeout configurado.</exception>
public Task<Cliente?> BuscarPorCuilAsync(string cuil, CancellationToken ct) { /* ... */ }
```

## Checklist antes de devolver código

- [ ] Toda API pública nueva tiene XML doc con `<summary>` y contrato.
- [ ] `<exception>` documenta los errores relevantes (excepciones tipadas EPA incluidas).
- [ ] No hay docs triviales ni mera repetición de la firma; se usa `<inheritdoc/>` cuando aplica.
- [ ] El XML doc refleja el comportamiento actual.
- [ ] Nada documentado por adivinación sobre código no comprendido.

## Auditoría de cobertura XML doc

Cuando se pide revisar la cobertura de XML doc en un microservicio:

1. Buscar tipos y miembros `public` en `**/*.cs` (excluir `*Tests.cs`, `Migrations/`,
   y código auto-generado).
2. Para cada miembro público, verificar si tiene `///` con `<summary>` inmediatamente antes.
3. Clasificar: **AUSENTE** | **INCOMPLETO** (falta `<param>`, `<returns>` o `<exception>`) | **OK**.
4. Emitir reporte con `archivo:línea` por hallazgo.
5. Activar `<GenerateDocumentationFile>true</GenerateDocumentationFile>` en el `.csproj`
   para que el compilador también detecte miembros públicos sin `<summary>` (warning CS1591).

Criterio de completitud:

- **Mínimo** (API interna): `<summary>` + `<param>` para params no obvios.
- **Completo** (API pública / endpoints de controller / contratos de servicio):
  mínimo + `<returns>` en miembros no-void + `<exception>` para las excepciones
  tipadas que puede lanzar.

## Validación

- Activar `<GenerateDocumentationFile>true</GenerateDocumentationFile>` en el `.csproj` para
  que el compilador advierta sobre miembros públicos sin documentar (CS1591 configurable).
- El `evaluator` marca como SHOULD (no bloqueante) la falta de docs, salvo que el PRP la
  declare como criterio de aceptación (entonces es MUST).

## Conexiones con otros skills

- `aspnetcore-microservice-orchestrator` — arbitra cuándo aplica.
- `aspnetcore-error-and-observability` — coherencia entre `<exception>` y las excepciones tipadas EPA.
- `dotnet-async-and-concurrency` — documentar propagación de `CancellationToken`.
- `dotnet-unit-testing` — los ejemplos deben ser consistentes con los tests.

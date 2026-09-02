# ADR-003 — Driver MongoDB 3.11.1 exacta e IDs como GUID binario

| Campo | Valor |
|---|---|
| Estado | `ACCEPTED` |
| Fecha | 2026-09-02 |
| Autor | Fabri |
| Task / PRP relacionado | `FND-001`, `FND-003` |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

- [x] datastore principal
- [x] contrato público incompatible

## Contexto

El driver .NET de MongoDB rompió compatibilidad entre 2.x y 3.x, y varios de esos
cambios son **silenciosos**: compilan igual y fallan al leer datos. Los tres
peligrosos son `decimal` (pasó de BSON string a `Decimal128`), `DateTimeOffset`
(de array a documento) y los GUIDs.

Además, el driver 3.x **no asume una representación de GUID por defecto**: hay que
registrarla explícitamente. Si nadie lo hace, los IDs se guardan de forma no
determinada y cualquier registro posterior lanza excepción.

Y el versionado del propio driver no es estable dentro del major: 3.5 y 3.10
introdujeron breaking changes.

## Decisión

**Driver:** `MongoDB.Driver` **3.11.1 exacta**, declarada una sola vez en
`Directory.Packages.props` con `ManagePackageVersionsCentrally`. Ningún `.csproj`
declara versión propia.

**IDs:** los identificadores de aggregate son `Guid`, serializados como **binario
con `GuidRepresentation.Standard`** (subtype 4). El `GuidSerializer` se registra en
el arranque, antes de cualquier operación.

**LINQ:** solo LINQ3. Las proyecciones client-side quedan **deshabilitadas**: una
query no traducible lanza `ExpressionNotSupportedException` en vez de traer la
colección entera y filtrar en memoria.

**Dinero:** `decimal` → BSON `Decimal128`. Nunca `double`.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| Rango flotante `[3.11.1,4.0.0)` | Menos mantenimiento, pero 3.5 y 3.10 ya demostraron traer breaking changes dentro del major, y dos servicios podrían compilar contra versiones distintas. |
| GUID como string | Legible en Compass y en logs, pero ocupa más y indexa peor. Se privilegió el tamaño y el índice; el costo es tener que registrar bien el serializer. |
| `ObjectId` | Nativo y eficiente, pero acopla el ID público al motor de persistencia, en contra de la regla hexagonal de que el dominio no conoce MongoDB. |
| Habilitar proyecciones client-side | Cómodo, pero esconde queries que escanean la colección entera. El fallo ruidoso es una red de seguridad. |

## Consecuencias

**Aceptamos:**

- versión con los CVE-2026-81527, 81528, 81529 y 81530 corregidos;
- soporte de .NET 10 y C# 14, que entra recién en 3.6/3.7;
- tracing OpenTelemetry nativo del driver, disponible desde 3.7;
- IDs compactos y con buen comportamiento de índice;
- las queries no traducibles fallan en desarrollo, no en producción.

**Perdemos:**

- subir de versión pasa a ser una decisión consciente, no automática;
- los IDs no son legibles al inspeccionar la base con Compass;
- una configuración de serialización mal hecha rompe la lectura de todo.

**Deuda que queda abierta:**

- MongoDB Server debe ser 4.4 o superior: el driver 3.10+ dejó de soportar 4.2 y
  anteriores. La POC usa 8.x, pero el tag de la imagen debe quedar fijo.

## Impacto en el repositorio

- Documentos actualizados: ninguno fuera de las skills.
- Skills afectadas: `mongodb-dotnet-driver`, `mongodb-document-modeling`.
- Tasks afectadas: `FND-001` (crea `Directory.Packages.props`), `FND-003` (IDs
  públicos en contratos), `FND-004` (tag de la imagen).
- Contratos que cambian de versión: define la representación de los IDs públicos.
- Servicios que deben migrar datos: ninguno, el proyecto nace en 3.x.

## Revisión

Se revisa al salir el driver 4.x, o si la ilegibilidad de los IDs binarios
entorpece el diagnóstico en la práctica.

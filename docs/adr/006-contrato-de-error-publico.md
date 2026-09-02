# ADR-006 — Contrato de error público: Problem Details + código estable

| Campo | Valor |
|---|---|
| Estado | `ACCEPTED` |
| Fecha | 2026-09-02 |
| Autor | Fabri |
| Task / PRP relacionado | `FND-003` |
| Reemplaza a | — |
| Reemplazado por | — |

## Disparador

- [x] contrato público incompatible

## Contexto

`ARCHITECTURE.md` §19 exige "errores públicos estables" y `AGENTS.md` lo repite en
la Definition of Done, pero **el formato nunca se definió**.

Ese vacío tiene un costo concreto y actual: seis skills heredadas de otro proyecto
siguen enseñando el formato `meta-data-error`, `IResponseBuilder` y las excepciones
tipadas de un arquetipo corporativo ajeno (`epa-net-paas`). No se pueden reescribir
mientras no exista un formato propio que ponga en su lugar, y mientras tanto un
agente que las cargue intenta heredar de clases que no existen.

Sin decisión, cada servicio inventaría su forma de responder errores y el frontend
tendría que manejar veinte variantes.

## Decisión

**Formato:** `application/problem+json` según **RFC 9457 (Problem Details for HTTP
APIs)**, que es el estándar y tiene soporte nativo en ASP.NET Core.

```json
{
  "type": "https://crm.local/errors/party-already-exists",
  "title": "La Party ya existe en esta organización.",
  "status": 409,
  "detail": "Ya hay una Party con ese CUIT en el tenant.",
  "instance": "/parties",
  "code": "PARTY_ALREADY_EXISTS",
  "correlationId": "0f9c…",
  "errors": { "taxId": ["Ya registrado."] }
}
```

- `type`, `title`, `status`, `detail` e `instance` son los campos del RFC.
- **`code`** es la extensión propia: identificador **estable y en mayúsculas** que
  el cliente puede usar para decidir. `title` y `detail` pueden cambiar de redacción;
  `code` no.
- **`correlationId`** permite atar el error a las trazas.
- **`errors`** solo en fallos de validación, con la forma de `ValidationProblemDetails`.

**Excepciones tipadas del proyecto**, en `building-blocks`:

| Excepción | Status |
|---|---|
| `ValidationException` | 400 |
| `UnauthenticatedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `ConflictException` (incluye `ConcurrencyConflictException`) | 409 |
| `BusinessRuleViolationException` | 422 |
| `DependencyUnavailableException` | 503 |

**Mapeo centralizado:** un `IExceptionHandler` registrado con `AddProblemDetails()`
traduce excepción → Problem Details. Los controllers no arman respuestas de error.

**Controllers:** derivan de `ControllerBase` de ASP.NET Core. **No hay clase base
propia** ni `IResponseBuilder`: el controller traduce HTTP a command o query y
devuelve el resultado. Sin lógica de negocio, sin `try/catch` de mapeo.

**Reglas de contenido:**

- nunca stack traces, nombres de índice, connection strings ni PII en la respuesta;
- un recurso de otro tenant devuelve **404**, no 403 (ver
  `multitenancy-authorization`);
- cada servicio mantiene su catálogo de `code` versionado como código; agregar uno
  es aditivo, cambiar su significado es breaking.

## Alternativas consideradas

| Alternativa | Por qué no |
|---|---|
| Conservar `meta-data-error` del arquetipo heredado | Es el formato propietario de otra empresa, sin las clases que lo implementan. Mantenerlo obligaría a reimplementar un estándar ajeno sin ninguna ventaja. |
| Formato propio desde cero | Trabajo de diseño, documentación y tooling que RFC 9457 ya resolvió, y ASP.NET Core ya soporta de fábrica. |
| Solo el status HTTP, sin cuerpo | Insuficiente: el frontend necesita distinguir dos conflictos distintos que ambos son 409. |
| Solo `code`, sin Problem Details | Pierde interoperabilidad y el soporte nativo del framework. |

## Consecuencias

**Aceptamos:**

- soporte nativo del framework: menos código propio que mantener;
- el frontend maneja un solo formato en todos los servicios y BFFs;
- `code` estable permite que la UI reaccione sin parsear textos;
- **desbloquea la reescritura de las seis skills heredadas**.

**Perdemos:**

- hay que mantener un catálogo de códigos por servicio y disciplinarlo en revisión;
- el equipo tiene que aprender el formato, aunque sea estándar.

**Deuda que queda abierta:**

- el catálogo inicial de `code` por servicio se completa a medida que las tasks los
  necesitan; `FND-003` crea la estructura y las excepciones base.

## Impacto en el repositorio

- Documentos a actualizar: `docs/skills/SKILL_GAPS.md` (cierra la deuda abierta).
- Skills a reescribir: `aspnetcore-rest-layer`,
  `aspnetcore-error-and-observability`, `aspnetcore-di-and-middleware-pipeline`,
  `aspnetcore-security-owasp-baseline`, `aspnetcore-outgoing-http`,
  `aspnetcore-config-and-secrets`.
- Tasks afectadas: `FND-003` implementa las excepciones y el handler; toda task con
  endpoints lo consume.
- Contratos que cambian de versión: define el contrato de error público de todas
  las APIs.
- Servicios que deben migrar datos: ninguno.

## Revisión

Se revisa si aparece un consumidor externo que exija otro formato, o si el catálogo
de códigos se vuelve inmanejable.

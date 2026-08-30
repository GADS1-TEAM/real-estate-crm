---
name: prp-feature-discovery-dotnet
description: |
  Activa cuando el usuario pide implementar una feature, refactor, integración o
  cualquier cambio no trivial en un microservicio ASP.NET Core antes de que se
  escriba código. Triggers: "nueva feature .NET", "endpoint nuevo", "microservicio
  EPA", "antes de implementar en C#", "necesito implementar X", "vamos a hacer Y",
  "armemos Z", "tengo que agregar", "diseñemos", "refactor de", "integrar con",
  "nuevo consumer", "nuevo servicio", "agregar soporte para", o cuando el pedido
  implica cambios en múltiples archivos, un contrato de API nuevo, mensajería, o
  coordinación con otro microservicio.
  Fuerza preguntas estructuradas y produce un PRP en PRPs/_backlog/ antes de
  escribir código. NO activar para: bug fixes en un solo archivo de menos de
  50 líneas, typos, cambios de configuración menores, agregar tests a código
  existente sin cambiar el código bajo test, o cuando el usuario dice
  explícitamente "ya tengo el PRP" o "implementá directo".
---

# PRP Feature Discovery (.NET 8 / ASP.NET Core)

## Objetivo

Evitar que el agente arranque a programar sin haber entendido la tarea. Forzar un
proceso corto de descubrimiento estructurado (5 preguntas obligatorias + 0 a 3
condicionales) que termina en un archivo PRP versionado, aprobado por el dev, y
que sirve como contrato para la implementación posterior.

En el contexto de microservicios ASP.NET Core con el arquetipo `epa-net-paas` el
discovery es especialmente importante porque un cambio puede impactar: contratos
de API REST consumidos por otros servicios, esquemas de mensajería, modelos de
datos con migraciones de BD, configuración en appsettings/Key Vault, o contratos
de seguridad (OAuth2 scopes). Identificar el scope real antes de codear evita
rework costoso.

## Cuándo activar

El skill se activa cuando el pedido cumple AL MENOS uno de:

- Implica una funcionalidad nueva (no un fix puntual de un solo archivo).
- Define o modifica un contrato de API REST (endpoint nuevo, cambio de DTO).
- Crea o modifica un consumer/producer de mensajería.
- Cambia el modelo de datos (nueva entidad, nueva relación, migración de BD).
- Integra con un microservicio externo o un sistema de otro equipo.
- Es un refactor con riesgo de regresión (cambio de lógica de negocio).
- El dev usa lenguaje de "vamos a hacer", "armemos", "necesito implementar",
  "agreguemos", "diseñemos", "integremos".

## Cuándo NO activar

- Bug fix en un solo archivo de menos de 50 líneas.
- Typo, cambio de formato, o ajuste de configuración menor.
- Agregar tests a código que ya existe sin cambiar el código bajo test.
- El dev dice explícitamente "ya tengo el PRP", "implementá esto directo",
  "saltá el PRP".

## Estado actual vs target

- **Target:** PRP en `PRPs/_backlog/` con contexto, scope, diseño técnico y
  criterios de aceptación antes de escribir código.
- **Cualquier microservicio ASP.NET Core con epa-net-paas:** aplica siempre el
  proceso de discovery. El PRP documenta decisiones técnicas y queda como
  historial en `PRPs/_done/`.

## Decisiones del proyecto

- **El PRP es el contrato:** sin PRP aprobado en `_in-progress/`, no se escribe
  código de feature.
- **Buscar PRPs previos** en `PRPs/_done/` antes de armar uno nuevo: puede haber
  patrones ya resueltos reutilizables.
- **5 preguntas obligatorias** en un único mensaje; no una pregunta por turno.
- **Condicionales** (0 a 3 adicionales): si la feature toca mensajería, preguntar
  sobre topic/queue, dead-letter e idempotencia; si toca EF Core, preguntar sobre
  migraciones y índices necesarios; si toca API externa, preguntar sobre contrato
  y versión.

## Preguntas obligatorias

```
1. ¿Qué problema de negocio resuelve esta feature? (Una oración)
2. ¿Qué capas del microservicio toca? (REST endpoint / service / EF Core / mensajería / config / seguridad)
3. ¿Hay contratos con otros equipos que se modifican? (API REST, esquemas de mensajería, DB compartida)
4. ¿Hay criterios de aceptación medibles? (comportamiento esperado, casos de error, métricas)
5. ¿Hay restricciones de tiempo, disponibilidad o compliance que deba saber?
```

## Preguntas condicionales

- Si toca mensajería: ¿Nuevo topic/queue o existente? ¿Dead-letter configurado? ¿Idempotencia requerida?
- Si toca EF Core: ¿Migración de BD con `dotnet ef migrations add`? ¿Índices necesarios? ¿Datos existentes a migrar?
- Si toca API externa: ¿Está documentada (OpenAPI)? ¿Versión del contrato? ¿Auth requerida?

## Reglas obligatorias

### MUST

1. **MUST generar el PRP en `PRPs/_backlog/`** con nombre `YYYY-MM-DD-<slug-descriptivo>.md`
   antes de escribir código de implementación.

2. **MUST hacer las 5 preguntas en un único mensaje**, no una por turno.

3. **MUST buscar PRPs relacionados en `PRPs/_done/`** antes de armar el nuevo
   PRP para reutilizar patrones ya resueltos.

4. **MUST incluir en el PRP**: contexto del problema, scope (qué entra y qué NO),
   diseño técnico propuesto, plan de implementación, criterios de aceptación y
   riesgos.

5. **MUST esperar aprobación del dev** antes de pasar a implementación. El PRP
   se mueve a `_in-progress/` al aprobarse.

### MUST NOT

6. **MUST NOT escribir código de implementación sin PRP aprobado** (para tareas
   no triviales según los criterios de activación).

7. **MUST NOT hacer preguntas de a una por turno.** Las 5 obligatorias van juntas.

8. **MUST NOT inventar respuestas** a las preguntas de discovery; si el dev no
   puede responder una, documentarlo como incertidumbre en el PRP.

## Checklist antes de devolver código

- [ ] PRP creado en `PRPs/_backlog/` con formato completo.
- [ ] Las 5 preguntas obligatorias están respondidas (o marcadas como pendientes).
- [ ] Se consultaron `PRPs/_done/` para reutilizar patrones.
- [ ] PRP aprobado y en `_in-progress/` antes de implementar.

## Conexiones con otros skills

- `aspnetcore-rest-layer` — skill de implementación de la capa REST (post-PRP).
- `aspnetcore-di-and-middleware-pipeline` — skill de DI y pipeline (post-PRP).
- Todos los demás skills del set .NET — aplican durante la implementación post-PRP.

# V2-FND-003 — Infraestructura local, CI y observabilidad

- **Ola:** 1 — Fundación ejecutable
- **Estado:** TODO
- **Dependencias:** V2-FND-001
- **UCs:** OPS-001, OPS-002, OPS-003, OPS-004, OPS-005
- **Owner:** infraestructura y calidad
- **Write zone:** docker-compose, infra, .github/workflows, configuración de observabilidad y scripts de test

## Resultado esperado

Una máquina limpia puede levantar MongoDB Community, RabbitMQ Community y
Keycloak con Docker Compose, ejecutar build/tests/lint desde CI y seguir una
request por correlationId sin registrar PII sensible.

## Alcance trazable

| UC | Actor | Comportamiento | Owner | Evidencia |
|---|---|---|---|---|
| OPS-001 | Desarrollo | Levantar servicios POC con un comando. | Infra | docker compose up y healthchecks. |
| OPS-002 | CI | Ejecutar build, test, lint y contract checks. | Calidad | Workflow exitoso y fallo controlado. |
| OPS-003 | Operador | Consultar alive/ready y dependencias caídas. | Infra | Health/readiness tests. |
| OPS-004 | Desarrollo | Correlacionar request, comando, evento y consumo. | Observability | Log/trace de un slice. |
| OPS-005 | Seguridad | Evitar secretos y PII en logs. | Observability | Test de sanitización. |

## Interfaces

- Compose services: mongo, rabbitmq, keycloak y los procesos del slice
  ejecutado.
- Endpoints: /health/live y /health/ready.
- Variables: nombres documentados en .env.example sin secretos reales.
- CI: comandos reproducibles para dotnet, frontend, contracts y tests.

## Reglas

- No agregar Redis/Valkey, OpenSearch, Kafka, Kubernetes, MinIO ni servicios
  pagos.
- No configurar infraestructura multi-tenant ni ambientes externos.
- OpenTelemetry y logs estructurados deben usar correlationId y actorId cuando
  exista, nunca contenido de conversaciones ni credenciales.
- Un retry solo se habilita en operaciones idempotentes.

## Criterios de aceptación

- [ ] docker compose up levanta MongoDB, RabbitMQ y Keycloak desde cero.
- [ ] Los healthchecks diferencian servicio vivo de dependencias listas.
- [ ] CI ejecuta localmente el mismo conjunto de comandos y falla ante un
  test roto.
- [ ] Un request de prueba conserva correlationId desde BFF hasta consumer.
- [ ] Un escaneo de logs no encuentra password, token, documento completo ni
  texto de actividad.

## Overrides POC

- Los secretos del realm local son ficticios y solo de desarrollo.
- La observabilidad no requiere collector o dashboard externo.
- El filesystem local se usa únicamente si una task necesita un archivo de
  prueba; no se agrega asset-service.

## Definition of Done

- [ ] Compose reproducible.
- [ ] CI y comandos locales documentados.
- [ ] Healthchecks y telemetría testeados.
- [ ] No se agregaron dependencias fuera de scope.

## Evidencia requerida

1. Salida de docker compose config y healthchecks.
2. Salida de CI local y workflow.
3. Trace/log de una request correlacionada.
4. Resultado del test de sanitización.

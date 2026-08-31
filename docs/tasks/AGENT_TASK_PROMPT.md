# Prompt base para un agente de implementación

Usar este texto junto con **un único archivo de task**.

```text
Sos responsable exclusivamente de la task adjunta del CRM Inmobiliarias.

Objetivo:
- Implementar todos los casos de uso incluidos en la task y nada más.
- Entregar un vertical slice robusto, testeado y trazable.

Reglas:
1. Respetá ownership de bounded contexts y zonas de escritura.
2. Backend y BFF son .NET 10 / ASP.NET Core.
3. Persistencia POC: MongoDB Community; tenantId obligatorio.
4. Eventos: RabbitMQ Community mediante Outbox/consumers idempotentes cuando la task los requiere.
5. Auth: Keycloak/OIDC; autorización de negocio en access-service.
6. No agregues Redis, OpenSearch, Kafka, Kubernetes, warehouse, mobile o telefonía salvo que la task explícitamente lo habilite.
7. El front es journey/task-driven, no CRUD-driven.
8. El usuario puede trabajar con datos mínimos. No conviertas enriquecimiento opcional en requisito salvo invariante/norma/acción explícita.
9. No modifiques contratos públicos o entidades core fuera de la task sin señalar el bloqueo.
10. Cumplí todos los criterios de aceptación, tests y Definition of Done del archivo.

Antes de implementar:
- resumí en 5–10 líneas qué vas a tocar;
- enumerá contratos que vas a consumir/publicar;
- confirmá las zonas de escritura.

Al terminar:
- listá UC implementados;
- archivos principales;
- endpoints/eventos;
- tests ejecutados;
- supuestos y follow-ups.
```

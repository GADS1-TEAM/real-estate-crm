import { describe, expect, it } from "vitest";
import { calculateDemandScore, crmCatalogFamilies, demoInitialState, demoReducer, demoStorageKey, normalizeDemoState, persistDemoState, readDemoState, type DemoAction } from "@/lib/demo-store";

describe("demoReducer", () => {
  it("records a stage transition and its history", () => {
    const action: DemoAction = { type: "opportunity/change-stage", id: "opp-1", stage: "Negociación", reason: "Cliente confirmó interés" };
    const state = demoReducer(demoInitialState, action);
    expect(state.opportunities.find((item) => item.id === "opp-1")?.stage).toBe("Negociación");
    expect(state.stageHistory.at(-1)).toMatchObject({ opportunityId: "opp-1", to: "Negociación" });
  });

  it("recalculates a criterion score without treating an unknown as a negative", () => {
    const state = demoReducer(demoInitialState, {
      type: "demand/update-criterion",
      demandId: "demand-1",
      criterionId: "rooms",
      value: "4",
      weight: "must",
    });
    expect(state.demands.find((item) => item.id === "demand-1")?.score).toBeGreaterThan(0);
    expect(state.demands.find((item) => item.id === "demand-1")?.criteria.find((item) => item.id === "surface")?.value).toBe("UNKNOWN");
  });

  it("keeps the score explainable when a criterion weight changes", () => {
    const demand = demoInitialState.demands[0];
    const original = calculateDemandScore(demand.criteria);
    const lighterNeighborhood = demand.criteria.map((criterion) => criterion.id === "neighborhood" ? { ...criterion, weight: "any" as const } : criterion);
    const recalculated = calculateDemandScore(lighterNeighborhood);

    expect(original).toBe(82);
    expect(recalculated).toBeLessThan(100);
    expect(recalculated).not.toBe(original);
  });

  it("creates a contact and an inmueble through reducer actions", () => {
    const withContact = demoReducer(demoInitialState, {
      type: "contact/create",
      item: { id: "contact-new", name: "María López", kind: "Persona", phone: "+54 9 11 4000 0000", email: "maria@example.com", status: "Activo", owner: "Martín Quiroga" },
    });
    const withProperty = demoReducer(withContact, {
      type: "property/create",
      item: { id: "property-new", title: "PH nuevo", address: "Aráoz 100", type: "PH", status: "Disponible", price: 120000, currency: "USD", bedrooms: "3 ambientes", geo: "Desconocida" },
    });

    expect(withProperty.contacts.at(-1)?.name).toBe("María López");
    expect(withProperty.properties.at(-1)?.title).toBe("PH nuevo");
  });

  it("opens a captation case with an unknown valuation", () => {
    const state = demoReducer(demoInitialState, {
      type: "captation/create",
      item: { id: "captation-new", propertyId: "property-2", owner: "Lucía Ferrari", stage: "Nueva", expectation: 180000, valuation: null, currency: "USD" },
    });

    expect(state.captations.at(-1)).toMatchObject({ id: "captation-new", stage: "Nueva", valuation: null });
  });

  it("creates a demand with a live explainable score", () => {
    const state = demoReducer(demoInitialState, {
      type: "demand/create",
      item: {
        id: "demand-new",
        contactId: "contact-1",
        title: "Ana busca PH en Palermo",
        status: "Activa",
        criteria: [{ id: "rooms", label: "Ambientes", value: "3", weight: "must" }, { id: "neighborhood", label: "Barrio", value: "Palermo", weight: "nice" }],
        score: 0,
      },
    });

    expect(state.demands.at(-1)?.title).toBe("Ana busca PH en Palermo");
    expect(state.demands.at(-1)?.score).toBeGreaterThan(0);
  });

  it("creates a commercial pipeline item while preserving its source type", () => {
    const state = demoReducer(demoInitialState, {
      type: "opportunity/create",
      item: { id: "opp-new", title: "Nueva captación", sourceType: "CAPTATION_CASE", sourceId: "captation-2", stage: "Nuevo", owner: "Lucía Ferrari", fee: 0, currency: "USD", daysInStage: 0 },
    });

    expect(state.opportunities.at(-1)).toMatchObject({ title: "Nueva captación", sourceType: "CAPTATION_CASE", sourceId: "captation-2" });
  });

  it("closes and reassigns a commercial pipeline item without losing its source", () => {
    const reassigned = demoReducer(demoInitialState, { type: "opportunity/reassign", id: "opp-1", owner: "Rodrigo Vergara" });
    const closed = demoReducer(reassigned, { type: "opportunity/close", id: "opp-1", outcome: "won", closeDate: "2026-09-10", finalValue: 7050000 });
    const opportunity = closed.opportunities.find((item) => item.id === "opp-1");

    expect(opportunity).toMatchObject({ owner: "Rodrigo Vergara", stage: "Cerrada", outcome: "won", sourceType: "REQUIREMENT" });
    expect(closed.stageHistory.at(-1)).toMatchObject({ from: "Visita", to: "Cerrada" });
  });

  it("creates an operation from an active reservation and advances its stage", () => {
    const created = demoReducer(demoInitialState, {
      type: "operation/create",
      item: { id: "operation-new", reservationId: "reservation-1", propertyTitle: "PH en Guardia Vieja 3355", stage: "Documentación" },
    });
    const advanced = demoReducer(created, { type: "operation/advance", id: "operation-new", stage: "Escrituración" });

    expect(advanced.operations.at(-1)).toMatchObject({ id: "operation-new", stage: "Escrituración" });
  });

  it("cancels a reservation or operation without deleting its history", () => {
    const cancelledReservation = demoReducer(demoInitialState, { type: "reservation/cancel", id: "reservation-1", reason: "Cliente desistió" });
    const cancelledOperation = demoReducer(cancelledReservation, { type: "operation/cancel", id: "operation-1", reason: "Cliente desistió" });

    expect(cancelledOperation.reservations[0].status).toBe("Cancelada");
    expect(cancelledOperation.operations[0].stage).toBe("Cancelada");
  });

  it("resolves a proposal with a traceable outcome", () => {
    const state = demoReducer(demoInitialState, { type: "proposal/resolve", outcome: "accepted", reason: "Condiciones confirmadas" });

    expect(state.proposal).toMatchObject({ outcome: "accepted", reason: "Condiciones confirmadas" });
  });

  it("keeps proposal terms immutable across proposal and counterproposal sequences", () => {
    const proposal = demoReducer(demoInitialState, {
      type: "proposal/submit",
      item: {
        id: "proposal-1",
        opportunityId: "opp-2",
        sequence: 1,
        kind: "PROPUESTA",
        proposedBy: "Martín Quiroga",
        proposedTo: "Carla Benítez",
        amount: 175000,
        currency: "USD",
        validity: "2026-09-15",
        conditions: "Entrega en 30 días",
        createdAt: "2026-09-10T10:00:00.000Z",
        outcome: "pending",
        reason: "",
      },
    });
    const counterproposal = demoReducer(proposal, {
      type: "proposal/submit",
      item: {
        id: "proposal-2",
        opportunityId: "opp-2",
        sequence: 2,
        kind: "CONTRAPROPUESTA",
        proposedBy: "Carla Benítez",
        proposedTo: "Martín Quiroga",
        amount: 180000,
        currency: "USD",
        validity: "2026-09-18",
        conditions: "Entrega en 45 días",
        createdAt: "2026-09-10T11:00:00.000Z",
        outcome: "pending",
        reason: "",
      },
    });
    const resolved = demoReducer(counterproposal, { type: "proposal/resolve", proposalId: "proposal-2", outcome: "accepted", reason: "Se aceptó la contrapropuesta" });
    const duplicate = demoReducer(resolved, { type: "proposal/submit", item: resolved.proposals[0] });

    expect(counterproposal.proposals).toHaveLength(2);
    expect(resolved.proposals[0]).toMatchObject({ amount: 175000, conditions: "Entrega en 30 días", outcome: "pending" });
    expect(resolved.proposals[1]).toMatchObject({ amount: 180000, conditions: "Entrega en 45 días", outcome: "accepted", reason: "Se aceptó la contrapropuesta" });
    expect(duplicate).toBe(resolved);
  });

  it("updates an existing activity instead of creating a duplicate", () => {
    const state = demoReducer(demoInitialState, { type: "activity/update", id: "activity-1", changes: { body: "Texto corregido" } });

    expect(state.activities.find((item) => item.id === "activity-1")?.body).toBe("Texto corregido");
    expect(state.activities).toHaveLength(demoInitialState.activities.length);
  });

  it("keeps administration changes in the demo snapshot", () => {
    const invited = demoReducer(demoInitialState, { type: "user/invite", item: { id: "user-new", name: "Nora Díaz", email: "nora@example.com", role: "Vendedor", status: "Pendiente" } });
    const enabled = demoReducer(invited, { type: "user/update", id: "user-new", changes: { status: "Habilitado" } });
    const catalog = demoReducer(enabled, { type: "catalog/update-entry", id: "stage-new", label: "Ingreso", status: "Activo" });

    expect(catalog.users.at(-1)).toMatchObject({ name: "Nora Díaz", status: "Habilitado" });
    expect(catalog.catalogEntries[0].label).toBe("Ingreso");
    expect(catalog.catalogVersion).toBe(3);
  });

  it("keeps the six catalog families distinct and protects pipeline semantics", () => {
    expect(crmCatalogFamilies).toHaveLength(6);
    expect(new Set(demoInitialState.catalogEntries.map((entry) => entry.catalogType)).size).toBe(6);
    expect(demoInitialState.catalogEntries.filter((entry) => entry.catalogType === "pipeline-stage").every((entry) => entry.protected && entry.semanticState)).toBe(true);
  });

  it("adds offline actions to the isolated demo queue", () => {
    const state = demoReducer(demoInitialState, {
      type: "offline/enqueue",
      item: { id: "offline-1", label: "Visita guardada", createdAt: "2026-09-09T12:00:00.000Z" },
    });
    expect(state.offlineQueue).toHaveLength(1);
    expect(state.offlineQueue[0].label).toBe("Visita guardada");
  });

  it("persists and hydrates only the demo state, clearing malformed data", () => {
    const state = demoReducer(demoInitialState, { type: "offline/enqueue", item: { id: "offline-2", label: "Nota pendiente", createdAt: "2026-09-09T12:00:00.000Z" } });
    window.localStorage.clear();
    persistDemoState(window.localStorage, state);
    expect(window.localStorage.getItem(demoStorageKey)).toContain("offline-2");
    expect(readDemoState(window.localStorage)?.offlineQueue[0].label).toBe("Nota pendiente");

    window.localStorage.setItem(demoStorageKey, "{malformed");
    expect(readDemoState(window.localStorage)).toBeNull();
    expect(window.localStorage.getItem(demoStorageKey)).toBeNull();
  });

  it("normalizes an older persisted snapshot before the UI reads it", () => {
    const oldSnapshot = { contacts: [] } as unknown;
    const normalized = normalizeDemoState(oldSnapshot);

    expect(normalized.demands).toEqual(demoInitialState.demands);
    expect(normalized.offlineQueue).toEqual([]);
    expect(normalized.opportunities[0]).toHaveProperty("sourceType");
  });

  it("normalizes legacy contacts with explicit commercial and identity states", () => {
    const normalized = normalizeDemoState({ contacts: [{ id: "legacy", name: "Legacy", kind: "Persona", phone: "", email: "", status: "No contactar", owner: "Martín Quiroga" }] });

    expect(normalized.contacts[0]).toMatchObject({ commercialStatus: "DO_NOT_CONTACT", identityStatus: "UNKNOWN" });
  });

  it("rejects invalid stage, close, activity, resolution, and terminal operation mutations", () => {
    const noStageReason = demoReducer(demoInitialState, { type: "opportunity/change-stage", id: "opp-1", stage: "Negociación" });
    expect(noStageReason).toBe(demoInitialState);

    const noCloseEvidence = demoReducer(demoInitialState, { type: "opportunity/close", id: "opp-1", outcome: "won" });
    expect(noCloseEvidence).toBe(demoInitialState);

    const noActivityRelation = demoReducer(demoInitialState, { type: "activity/add", item: { id: "invalid", type: "Nota", subject: "Inválida", body: "", actor: "Martín Quiroga", createdAt: "Ahora" } });
    expect(noActivityRelation).toBe(demoInitialState);

    const noResolutionReason = demoReducer(demoInitialState, { type: "proposal/resolve", outcome: "rejected", reason: "  " });
    expect(noResolutionReason).toBe(demoInitialState);

    const noCancelReason = demoReducer(demoInitialState, { type: "reservation/cancel", id: "reservation-1", reason: "  " });
    expect(noCancelReason).toBe(demoInitialState);

    const cancelled = demoReducer(demoInitialState, { type: "operation/cancel", id: "operation-1", reason: "Comprador desistió" });
    const terminalAdvance = demoReducer(cancelled, { type: "operation/advance", id: "operation-1", stage: "Escrituración" });
    expect(terminalAdvance).toBe(cancelled);
  });

  it("stores close, cancellation, and activity evidence when inputs are valid", () => {
    const closed = demoReducer(demoInitialState, { type: "opportunity/close", id: "opp-1", outcome: "won", closeDate: "2026-09-10", finalValue: 7050000 });
    expect(closed.opportunities[0]).toMatchObject({ outcome: "won", closedAt: "2026-09-10", finalValue: 7050000 });
    expect(closed.stageHistory.at(-1)).toMatchObject({ reason: "Cierre ganado", outcome: "won" });

    const cancelled = demoReducer(demoInitialState, { type: "reservation/cancel", id: "reservation-1", reason: "Cliente desistió" });
    expect(cancelled.reservations[0]).toMatchObject({ status: "Cancelada", cancellationReason: "Cliente desistió" });

    const activity = demoReducer(demoInitialState, { type: "activity/add", item: { id: "valid", type: "WhatsApp", subject: "Histórico", body: "Texto", actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: "2026-09-10T10:00:00.000Z", relatedRecordId: "contact-1", relatedRecordType: "Party", createdAt: "Ahora" } });
    expect(activity.activities[0]).toMatchObject({ id: "valid", occurredAt: "2026-09-10T10:00:00.000Z", relatedRecordId: "contact-1", historical: true });
  });

  it("keeps party relationships explicit and rejects activities for unknown records", () => {
    const related = demoReducer(demoInitialState, { type: "party/relate", contactId: "contact-2", companyId: "contact-4" });
    expect(related.relationships.at(-1)).toMatchObject({ fromPartyId: "contact-2", toPartyId: "contact-4", relationshipType: "CONTACT_OF" });

    const invalidActivity = demoReducer(demoInitialState, { type: "activity/add", item: { id: "unknown-relation", type: "Nota", subject: "Inválida", body: "No debe guardarse", actor: "Martín Quiroga", recordedByUserId: "user-1", occurredAt: "2026-09-10T10:00:00.000Z", relatedRecordId: "missing", relatedRecordType: "Party", createdAt: "Ahora" } });
    expect(invalidActivity).toBe(demoInitialState);
  });

  it("records AI review without changing a business aggregate", () => {
    const reviewed = demoReducer(demoInitialState, { type: "ai/review", id: "ai-surface" });
    expect(reviewed.aiSuggestions.find((suggestion) => suggestion.id === "ai-surface")).toMatchObject({ status: "reviewed" });
    expect(reviewed.demands).toEqual(demoInitialState.demands);
    expect(reviewed.opportunities).toEqual(demoInitialState.opportunities);
  });
});

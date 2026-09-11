import { describe, expect, it } from "vitest";
import {
  applyContactSuggestions,
  COMMERCIAL_STATUS_VALUES,
  criterionContribution,
  getCommercialStatusMeaning,
  parseHistoricalContactText,
  resolveSelectedRecord,
  validateActivity,
  validateOperationClose,
  validateOperationTransition,
  validateOpportunityClose,
  validateOpportunityStageChange,
} from "@/lib/workflow-rules";
import { crmPersonas } from "@/lib/permissions";

describe("workflow rules", () => {
  it("selects the supplied record and does not fall back for an unknown id", () => {
    const records = [{ id: "party-1" }, { id: "party-2" }];
    expect(resolveSelectedRecord(records, "party-2")).toEqual({ id: "party-2" });
    expect(resolveSelectedRecord(records, "missing")).toBeUndefined();
  });

  it("exposes exactly the four commercial statuses", () => {
    expect(COMMERCIAL_STATUS_VALUES).toEqual(["POTENTIAL", "CUSTOMER", "INACTIVE", "DO_NOT_CONTACT"]);
  });

  it("allows no-op stages, requires a reason for changes, and keeps closed opportunities closed", () => {
    expect(validateOpportunityStageChange("CONTACT", "CONTACT")).toEqual({ valid: true });
    expect(validateOpportunityStageChange("CONTACT", "VISIT")).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOpportunityStageChange("CONTACT", "VISIT", "Cliente confirmó visita")).toEqual({ valid: true });
    expect(validateOpportunityStageChange("CLOSED", "CONTACT", "Reabrir")).toMatchObject({ valid: false, error: expect.any(String) });
  });

  it("validates won and lost close requirements", () => {
    expect(validateOpportunityClose("won", "2026-09-10", 125000)).toEqual({ valid: true });
    expect(validateOpportunityClose("won", "", 125000)).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOpportunityClose("won", "2026-09-10", -1)).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOpportunityClose("won", "2026-09-10", Number.NaN)).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOpportunityClose("won", "2026-09-10", Number.POSITIVE_INFINITY)).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOpportunityClose("lost", "2026-09-10", undefined, "Eligió otra propiedad")).toEqual({ valid: true });
    expect(validateOpportunityClose("lost", undefined, undefined, "Eligió otra propiedad")).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOpportunityClose("lost", undefined, undefined, "  ")).toMatchObject({ valid: false, error: expect.any(String) });
  });

  it("requires occurredAt independently", () => {
    expect(validateActivity({ activityType: "Call", occurredAt: "", actor: "Ana", relatedRecordId: "party-1" })).toMatchObject({ valid: false, error: expect.any(String) });
  });

  it("requires actor independently", () => {
    expect(validateActivity({ activityType: "Call", occurredAt: "2026-09-10", actor: "  ", relatedRecordId: "party-1" })).toMatchObject({ valid: false, error: expect.any(String) });
  });

  it("requires relatedRecordId independently", () => {
    expect(validateActivity({ activityType: "Call", occurredAt: "2026-09-10", actor: "Ana", relatedRecordId: "" })).toMatchObject({ valid: false, error: expect.any(String) });
  });

  it("keeps email and WhatsApp historical", () => {
    expect(validateActivity({ activityType: "Email", occurredAt: "2026-09-10", actor: "Ana", relatedRecordId: "party-1" })).toMatchObject({ valid: true, historical: true });
    expect(validateActivity({ activityType: "WhatsApp", occurredAt: "2026-09-10", actor: "Ana", relatedRecordId: "party-1" })).toMatchObject({ valid: true, historical: true });
  });

  it("keeps unknown criterion results explicit", () => {
    expect(criterionContribution("Villa Crespo", "Villa Crespo")).toEqual({ status: "match", contribution: 1 });
    expect(criterionContribution("Villa Crespo", "UNKNOWN")).toEqual({ status: "unknown", contribution: null });
    expect(criterionContribution("Villa Crespo", "Belgrano")).toEqual({ status: "mismatch", contribution: 0 });
  });

  it("parses historical contact suggestions without applying them before confirmation", () => {
    const suggestions = parseHistoricalContactText("Nombre: Paula Gómez\nTel: +54 9 11 4444-8899\nEmail: paula@example.com");
    const contact = { id: "contact-1", name: "Ana", phone: "", email: "" };

    expect(suggestions).toEqual({ name: "Paula Gómez", phone: "+54 9 11 4444-8899", email: "paula@example.com" });
    expect(applyContactSuggestions(contact, suggestions, false)).toEqual(contact);
    expect(applyContactSuggestions(contact, suggestions, true)).toMatchObject(suggestions);
  });

  it("keeps the documented commercial status meanings explicit", () => {
    expect(getCommercialStatusMeaning("DO_NOT_CONTACT")).toContain("No contactar");
    expect(getCommercialStatusMeaning("POTENTIAL")).toContain("Potencial");
  });

  it("exposes exactly the five CRM personas and their entry screens", () => {
    expect(crmPersonas.map(({ name, startScreen }) => ({ name, startScreen }))).toEqual([
      { name: "Martín Quiroga", startScreen: "INI-01" },
      { name: "Lucía Ferrari", startScreen: "PRP-01" },
      { name: "Rodrigo Vergara", startScreen: "INI-03" },
      { name: "Elena Vergara", startScreen: "ANA-05" },
      { name: "Sofía Rendón", startScreen: "ADM-01" },
    ]);
  });

  it("requires operation close evidence and prevents advancing terminal operations", () => {
    expect(validateOperationClose("", 250000)).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOperationClose("2026-09-10", -1)).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOperationClose("2026-09-10", 250000)).toEqual({ valid: true });
    expect(validateOperationTransition("Cerrada", "Escrituración")).toMatchObject({ valid: false, error: expect.any(String) });
    expect(validateOperationTransition("Cancelada", "Cerrada")).toMatchObject({ valid: false, error: expect.any(String) });
  });
});

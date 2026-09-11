import { describe, expect, it } from "vitest";
import { demoInitialState, getAnalyticsSnapshot } from "@/lib/demo-store";

describe("analytics snapshot", () => {
  const defaults = { period: "all" as const, responsible: "ALL", pipelineKind: "ALL" as const, origin: "ALL" };

  it("computes metrics from the selected pipeline scope", () => {
    const snapshot = getAnalyticsSnapshot(demoInitialState, defaults);

    expect(snapshot.opportunities).toHaveLength(4);
    expect(snapshot.openOpportunities).toBe(4);
    expect(snapshot.averageStageDays).toBe(4.3);
    expect(snapshot.estimatedFees).toBe(21090000);
  });

  it("applies responsible, pipeline kind, and origin filters", () => {
    const snapshot = getAnalyticsSnapshot(demoInitialState, {
      ...defaults,
      responsible: "Lucía Ferrari",
      pipelineKind: "CAPTATION_CASE",
      origin: "Referido",
    });

    expect(snapshot.opportunities.map((opportunity) => opportunity.id)).toEqual(["opp-3"]);
    expect(snapshot.estimatedFees).toBe(8700000);
  });

  it("keeps unavailable derived rates as UNKNOWN instead of zero", () => {
    const snapshot = getAnalyticsSnapshot(demoInitialState, { ...defaults, responsible: "No existe" });

    expect(snapshot.estimatedFees).toBe("UNKNOWN");
    expect(snapshot.averageStageDays).toBe("UNKNOWN");
    expect(snapshot.conversionRate).toBe("UNKNOWN");

    const noVisits = getAnalyticsSnapshot({ ...demoInitialState, activities: [] }, defaults);
    expect(noVisits.conversionRate).toBe("UNKNOWN");
  });
});

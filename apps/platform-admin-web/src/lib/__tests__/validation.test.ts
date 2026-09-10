import { describe, expect, it } from "vitest";
import { validateReviewState } from "@/lib/validation";
import { reviewInitialState } from "@/lib/review-store";

describe("critical platform validations", () => {
  it("blocks a pack that references a draft workflow", () => {
    const result = validateReviewState({ ...reviewInitialState, journeyFixes: {} });
    expect(result.some((issue) => issue.journey === "J1" && issue.severity === "blocker" && issue.category === "Referencias")).toBe(true);
  });

  it("keeps UNKNOWN distinct from false, zero and an empty confirmed list", () => {
    const result = validateReviewState({ ...reviewInitialState, unknownValue: "UNKNOWN" });
    expect(result.find((issue) => issue.code === "UNKNOWN_PRESERVED")?.severity).toBe("ok");
    expect(result.find((issue) => issue.code === "UNKNOWN_PRESERVED")?.message).toContain("UNKNOWN");
  });

  it("covers each critical journey with an explicit validation outcome", () => {
    const result = validateReviewState(reviewInitialState);
    expect(new Set(result.filter((issue) => issue.journey).map((issue) => issue.journey))).toEqual(new Set(["J1", "J2", "J3", "J4", "J5", "J6", "J7", "J8", "J9", "J10"]));
    const resolved = validateReviewState({ ...reviewInitialState, journeyFixes: { J1: true, J2: true, J3: true, J4: true, J5: true, J6: true, J7: true, J9: true, J10: true } });
    expect(resolved.some((issue) => issue.severity === "blocker")).toBe(false);
    expect(resolved.find((issue) => issue.code === "UNKNOWN_PRESERVED")?.severity).toBe("ok");
  });
});

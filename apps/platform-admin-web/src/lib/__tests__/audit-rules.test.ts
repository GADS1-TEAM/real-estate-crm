import { describe, expect, it } from "vitest";
import { buildAuditEvidence, formatCorrelationId } from "@/lib/audit-rules";

describe("audit rules", () => {
  it("preserves the complete correlation id as a copyable value", () => {
    const correlationId = "corr-1234567890-abcdefghijklmnopqrstuvwxyz";
    expect(formatCorrelationId(correlationId)).toEqual({ displayValue: correlationId, copyValue: correlationId });
    expect(formatCorrelationId("UNKNOWN")).toEqual({ displayValue: "UNKNOWN", copyValue: "UNKNOWN" });
  });

  it("preserves UNKNOWN for missing evidence without coercing false or zero", () => {
    expect(buildAuditEvidence({ action: "publish", actor: "admin", reason: "approved", correlationId: "corr-1", before: 0, after: false })).toEqual({
      action: "publish", actor: "admin", reason: "approved", correlationId: "corr-1", before: 0, after: false,
    });
    expect(buildAuditEvidence({ action: "publish", actor: "admin", reason: "approved", correlationId: "corr-2" })).toEqual({
      action: "publish", actor: "admin", reason: "approved", correlationId: "corr-2", before: "UNKNOWN", after: "UNKNOWN",
    });
  });
});

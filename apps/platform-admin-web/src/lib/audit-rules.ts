export const UNKNOWN_VALUE = "UNKNOWN" as const;

export type CorrelationPresentation = { displayValue: string; copyValue: string };

export function formatCorrelationId(correlationId?: string | null): CorrelationPresentation {
  const value = correlationId ?? UNKNOWN_VALUE;
  return { displayValue: value, copyValue: value };
}

export type AuditEvidenceInput = { action: string; actor: string; reason: string; correlationId: string; before?: unknown; after?: unknown };
export type AuditEvidence = Omit<AuditEvidenceInput, "before" | "after"> & { before: unknown; after: unknown };

export function buildAuditEvidence(input: AuditEvidenceInput): AuditEvidence {
  return {
    action: input.action,
    actor: input.actor,
    reason: input.reason,
    correlationId: input.correlationId,
    before: input.before === undefined ? UNKNOWN_VALUE : input.before,
    after: input.after === undefined ? UNKNOWN_VALUE : input.after,
  };
}

import { describe, expect, it } from "vitest";
import { persistReviewState, readReviewState, reviewInitialState, reviewReducer, reviewStorageKey } from "@/lib/review-store";

describe("review store", () => {
  it("creates a new draft instead of mutating a published version", () => {
    const next = reviewReducer(reviewInitialState, { type: "draft/create", artifactId: "pack-argentina", baseVersion: 8 });
    expect(next.drafts["pack-argentina"]?.version).toBe(9);
    expect(next.published["pack-argentina"]?.version).toBe(8);
  });

  it("keeps a single active draft and models publish/deprecate transitions", () => {
    const created = reviewReducer(reviewInitialState, { type: "draft/create", artifactId: "workflow-sale", baseVersion: 7 });
    const duplicate = reviewReducer(created, { type: "draft/create", artifactId: "workflow-sale", baseVersion: 7 });
    expect(Object.keys(duplicate.drafts).filter((id) => id === "workflow-sale")).toHaveLength(1);
    const published = reviewReducer(duplicate, { type: "artifact/publish", artifactId: "workflow-sale" });
    expect(published.published["workflow-sale"]?.version).toBe(8);
    expect(published.drafts["workflow-sale"]).toBeUndefined();
    expect(published.audit[0]).toMatchObject({ action: "Publicó versión", before: { version: 7 }, after: { version: 8 } });
    expect(published.selectedAuditId).toBe(published.audit[0].id);
    expect(published.toastAuditId).toBe(published.audit[0].id);
    const deprecated = reviewReducer(published, { type: "artifact/deprecate", artifactId: "workflow-sale" });
    expect(deprecated.published["workflow-sale"]?.status).toBe("DEPRECATED");
    expect(deprecated.audit[0]).toMatchObject({ action: "Deprecó artefacto", before: { status: "PUBLISHED" }, after: { status: "DEPRECATED" } });
  });

  it("moves a draft to review without changing the published version", () => {
    const next = reviewReducer(reviewInitialState, { type: "artifact/review", artifactId: "pack-argentina" });

    expect(next.drafts["pack-argentina"]?.status).toBe("IN_REVIEW");
    expect(next.published["pack-argentina"]?.version).toBe(8);
  });

  it("stores draft values separately from the published version and can discard them", () => {
    const updated = reviewReducer(reviewInitialState, { type: "draft/update", artifactId: "pack-argentina", field: "description", value: "Nueva descripción" });
    expect(updated.draftValues["pack-argentina"]?.description).toBe("Nueva descripción");
    expect(updated.published["pack-argentina"]?.name).toBe("Argentina Base Pack");

    const discarded = reviewReducer(updated, { type: "draft/discard", artifactId: "pack-argentina" });
    expect(discarded.draftValues["pack-argentina"]).toBeUndefined();
    expect(discarded.drafts["pack-argentina"]).toBeUndefined();
  });

  it("records a reason when a warning is accepted for publication", () => {
    const next = reviewReducer(reviewInitialState, { type: "validation/accept-warning", code: "REQUIRED_CAPABILITY", reason: "La capability queda cubierta por el piloto vigente." });
    expect(next.acceptedWarnings["REQUIRED_CAPABILITY"]).toBe("La capability queda cubierta por el piloto vigente.");
  });

  it("persists and restores an isolated review snapshot", () => {
    const storage = new Map<string, string>();
    const fakeStorage = {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => storage.set(key, value),
      removeItem: (key: string) => storage.delete(key),
    };
    persistReviewState(fakeStorage, { ...reviewInitialState, toast: "Guardado" });
    expect(storage.has(reviewStorageKey)).toBe(true);
    expect(readReviewState(fakeStorage)?.toast).toBe("Guardado");
  });

  it("records before and after evidence for an administrative correction", () => {
    const next = reviewReducer(reviewInitialState, { type: "correction/audit", command: "REPROCESS_COMPLIANCE_CASE", targetId: "case-1", reason: "Reintentar con la versión vigente" });

    expect(next.audit[0]).toMatchObject({ before: { targetId: "case-1", status: "unchanged" }, after: { targetId: "case-1", status: "correction-requested" }, correlationId: "corr-2" });
    expect(next.toastAuditId).toBe(next.audit[0].id);
  });
});

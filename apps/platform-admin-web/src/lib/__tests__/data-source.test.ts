import { afterEach, describe, expect, it, vi } from "vitest";
import { BffPlatformAdminDataSource, createPlatformAdminDataSource, PlatformDataSourceError, ReviewPlatformAdminDataSource } from "@/lib/data-source";

describe("platform data source boundary", () => {
  afterEach(() => vi.restoreAllMocks());
  it("does not silently fall back to local data in productive mode", () => {
    const source = createPlatformAdminDataSource("bff", undefined);
    expect(source.kind).toBe("bff");
    expect(source.getAvailability()).toEqual({ available: false, reason: "BFF_URL_MISSING" });
  });

  it("enables local review data only explicitly", () => {
    expect(createPlatformAdminDataSource("review").kind).toBe("review");
  });

  it("propagates a stable public error instead of using local data when the BFF is unavailable", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("network")));
    await expect(new BffPlatformAdminDataSource("http://bff.local").getSnapshot()).rejects.toEqual(expect.objectContaining({ code: "PLATFORM_BFF_UNAVAILABLE" } satisfies Partial<PlatformDataSourceError>));
  });

  it("keeps the BFF correlation id on a public API error", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ code: "VALIDATION_FAILED", correlationId: "corr-test" }), { status: 422, headers: { "Content-Type": "application/json" } })));
    await expect(new BffPlatformAdminDataSource("http://bff.local").getSnapshot()).rejects.toEqual(expect.objectContaining({ code: "VALIDATION_FAILED", status: 422, correlationId: "corr-test" } satisfies Partial<PlatformDataSourceError>));
  });

  it("sends the selected environment as request context", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ artifacts: [], impact: { tenantsAffected: 0, operationsAffected: 0, blockers: 0, warnings: 0, reasons: [] }, state: {} }), { status: 200, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);

    await new BffPlatformAdminDataSource("http://bff.local", "Staging").getSnapshot();

    expect(fetchMock.mock.calls[0][1]?.headers).toMatchObject({ "x-platform-environment": "Staging" });
  });

  it("persists mutations only when the local review source is explicitly constructed with storage", async () => {
    const storage = new Map<string, string>();
    const localStorageLike = { getItem: (key: string) => storage.get(key) ?? null, setItem: (key: string, value: string) => storage.set(key, value), removeItem: (key: string) => storage.delete(key) };
    const source = new ReviewPlatformAdminDataSource(undefined, localStorageLike);
    await source.saveDraft({ artifactId: "pack-argentina", field: "description", value: "Actualizado" });
    expect(storage.size).toBe(1);
    expect(JSON.parse(storage.values().next().value as string).drafts["pack-argentina"].changedFields).toContain("description");
  });
});

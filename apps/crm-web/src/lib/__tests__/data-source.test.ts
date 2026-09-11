import { describe, expect, it, vi } from "vitest";
import { BffCrmDataSource, DemoCrmDataSource, createCrmDataSource } from "@/lib/data-source";

describe("CRM data sources", () => {
  it("does not silently fall back when the BFF is not configured", async () => {
    await expect(new BffCrmDataSource("").getScreenData("INI-01")).rejects.toThrow("CRM_BFF_NOT_CONFIGURED");
  });

  it("uses the explicit demo source only for demo mode", async () => {
    const snapshot = { contacts: 4 };
    const source = createCrmDataSource("demo", snapshot);
    expect(source).toBeInstanceOf(DemoCrmDataSource);
    await expect(source.getScreenData("PTY-01")).resolves.toEqual({ screenId: "PTY-01", snapshot });
    expect(createCrmDataSource(undefined)).toBeInstanceOf(BffCrmDataSource);
  });

  it("sends BFF mutations through the configured HTTP boundary", async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true });
    vi.stubGlobal("fetch", fetchMock);
    await new BffCrmDataSource("http://bff.test").saveMutation("opportunity/change-stage", { id: "opp-1" });
    expect(fetchMock).toHaveBeenCalledWith("http://bff.test/mutations/opportunity%2Fchange-stage", expect.objectContaining({ method: "POST" }));
    vi.unstubAllGlobals();
  });
});

import { describe, expect, it } from "vitest";
import { buildCrmScreenUrl } from "@/lib/navigation";
import { resolveSelectedRecord } from "@/lib/workflow-rules";

describe("buildCrmScreenUrl", () => {
  it("preserves a supplied entity id while selecting the target screen", () => {
    expect(buildCrmScreenUrl("/contactos", "PTY-04", "contact: Ana & Co/10")).toBe(
      "/contactos?screen=PTY-04&entity=contact%3A+Ana+%26+Co%2F10",
    );
  });

  it("omits entity when the target has no selected record", () => {
    expect(buildCrmScreenUrl("/contactos", "PTY-01")).toBe("/contactos?screen=PTY-01");
    expect(buildCrmScreenUrl("/contactos", "PTY-01", null)).toBe("/contactos?screen=PTY-01");
  });

  it("preserves an explicitly empty entity instead of turning it into an absent selection", () => {
    expect(buildCrmScreenUrl("/contactos", "PTY-05", "")).toBe("/contactos?screen=PTY-05&entity=");
  });

  it("round trips opaque ids without trimming or interpreting query characters", () => {
    const id = "  contacto/ñ?screen=PTY-01&entity=other+#%  ";
    const url = new URL(buildCrmScreenUrl("/contactos", "PTY-05", id), "https://crm.example");
    expect(url.searchParams.get("entity")).toBe(id);
    expect(url.searchParams.get("screen")).toBe("PTY-05");
    expect(url.hash).toBe("");
  });

  it("does not resolve an unknown URL selection to another record", () => {
    const records = [{ id: "contact-1", name: "Unrelated fixture" }];
    const url = new URL(buildCrmScreenUrl("/contactos", "PTY-05", "missing"), "https://crm.example");
    expect(resolveSelectedRecord(records, url.searchParams.get("entity"))).toBeUndefined();
  });
});

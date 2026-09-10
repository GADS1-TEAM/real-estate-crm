import { describe, expect, it } from "vitest";
import { can, explainPermission, platformRoles } from "@/lib/permissions";

describe("platform permissions", () => {
  it("keeps restricted actions visible but reports who can execute them", () => {
    expect(can("configurator", "PUBLISH")).toBe(false);
    expect(explainPermission("configurator", "PUBLISH")).toContain("PUBLISH");
    expect(explainPermission("configurator", "PUBLISH")).toContain("Release manager");
  });

  it("supports the proposed internal personas without hiding their scopes", () => {
    expect(platformRoles.map((role) => role.id)).toEqual([
      "configurator",
      "release-manager",
      "compliance",
      "support",
      "platform-admin",
    ]);
    expect(can("platform-admin", "ADMIN_CORRECTION")).toBe(true);
    expect(can("support", "VIEW_AUDIT")).toBe(true);
  });
});

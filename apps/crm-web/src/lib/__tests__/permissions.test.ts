import { describe, expect, it } from "vitest";
import { getRole, hasPermission, permissionReason } from "@/lib/permissions";

describe("effective permissions", () => {
  it("keeps role capabilities explicit", () => {
    expect(hasPermission("vendedor", "party.write")).toBe(true);
    expect(hasPermission("vendedor", "property.write")).toBe(false);
    expect(hasPermission("responsable", "analytics.read")).toBe(true);
    expect(hasPermission("direccion", "analytics.read")).toBe(true);
    expect(hasPermission("direccion", "commercial.write")).toBe(false);
    expect(hasPermission("administradora", "admin.write")).toBe(true);
  });

  it("returns a Spanish reason for denied actions", () => {
    expect(permissionReason("vendedor", "property.write")).toContain("no incluye «Editar inmuebles»");
    expect(permissionReason("responsable", "analytics.read")).toBe("Permitido por tu rol efectivo.");
  });

  it("falls back to the least-privileged defined role for an unknown identifier", () => {
    expect(getRole("inexistente" as never).id).toBe("vendedor");
  });
});

import { describe, expect, it } from "vitest";
import { screenRegistry, getScreenById } from "@/lib/screen-registry";

describe("screenRegistry", () => {
  it("covers every inventory entry exactly once", () => {
    expect(screenRegistry).toHaveLength(172);
    expect(new Set(screenRegistry.map((screen) => screen.id)).size).toBe(172);
  });

  it("maps 137 V2 screens and preserves 35 deferred F2 surfaces", () => {
    expect(screenRegistry.filter((screen) => screen.phase === "V2")).toHaveLength(137);
    expect(screenRegistry.filter((screen) => screen.phase === "F2")).toHaveLength(35);
    expect(screenRegistry.filter((screen) => screen.phase === "V2").every((screen) => screen.renderKey && screen.renderKey !== "deferred")).toBe(true);
    expect(screenRegistry.filter((screen) => screen.phase === "F2").every((screen) => screen.deferred)).toBe(true);
  });

  it("resolves a concrete screen and a deferred screen by id", () => {
    expect(getScreenById("COM-08")).toMatchObject({ phase: "V2", renderKey: "commercial" });
    expect(getScreenById("AGD-01")).toMatchObject({ phase: "F2", deferred: true });
  });
});

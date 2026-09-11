import { describe, expect, it } from "vitest";
import { getScreenById, getScreenByRoute, screenRegistry } from "@/lib/screen-registry";

describe("screenRegistry", () => {
  it("contains every handoff surface exactly once", () => {
    expect(screenRegistry).toHaveLength(72);
    expect(new Set(screenRegistry.map((screen) => screen.id)).size).toBe(72);
    expect(screenRegistry.every((screen) => screen.route && screen.renderKey)).toBe(true);
    expect(screenRegistry.every((screen) => getScreenByRoute(screen.route)?.id === screen.id)).toBe(true);
  });

  it("maps the critical editors and transversal surfaces", () => {
    expect(getScreenById("PA-042")?.renderKey).toBe("workflow-editor");
    expect(getScreenById("PA-051")?.renderKey).toBe("requirements-matrix");
    expect(getScreenById("PA-081")?.renderKey).toBe("automation-editor");
    expect(getScreenById("PA-132")?.renderKey).toBe("effective-inspector");
    expect(getScreenById("PA-153")?.renderKey).toBe("version-header");
  });
});

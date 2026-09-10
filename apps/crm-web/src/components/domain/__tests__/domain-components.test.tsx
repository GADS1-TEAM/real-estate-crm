import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CriterionEditor, MatchScoreExplanation } from "@/components/domain/domain-components";
import { Tabs } from "@/components/ui/primitives";

describe("domain components", () => {
  it("explains a match with evidence instead of exposing only a score", () => {
    render(<MatchScoreExplanation score={82} confidence="Alta" matches={["4 ambientes", "Villa Crespo"]} unknowns={["Superficie cubierta"]} />);
    expect(screen.getByLabelText("82 puntos")).toBeTruthy();
    expect(screen.getByText("4 ambientes")).toBeTruthy();
    expect(screen.getByText("Superficie cubierta desconocida")).toBeTruthy();
  });

  it("changes a criterion weight with accessible controls", async () => {
    const user = userEvent.setup();
    render(<CriterionEditor label="Ambientes" value="4" weight="must" onChange={() => undefined} />);
    await user.click(screen.getByRole("button", { name: "Ojalá" }));
    expect(screen.getByRole("button", { name: "Ojalá" }).getAttribute("aria-pressed")).toBe("true");
  });

  it("keeps the selected tab interactive even when the route owns the content", async () => {
    const user = userEvent.setup();
    render(<Tabs tabs={[{ id: "summary", label: "Resumen" }, { id: "history", label: "Historial" }]} active="summary" onChange={() => undefined} />);
    await user.click(screen.getByRole("tab", { name: "Historial" }));
    expect(screen.getByRole("tab", { name: "Historial" }).getAttribute("aria-selected")).toBe("true");
  });
});

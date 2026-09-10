import { describe, expect, it } from "vitest";
import { formatCurrency, type CurrencyCode } from "@/lib/currency";

describe("formatCurrency", () => {
  it("formats Argentine pesos with an explicit currency", () => {
    expect(formatCurrency(1240580.5, "ARS")).toBe("$ 1.240.580,50");
  });

  it("formats US dollars without losing the currency label", () => {
    expect(formatCurrency(180000, "USD")).toBe("US$ 180.000,00");
  });

  it("does not turn unknown into zero", () => {
    expect(formatCurrency(null, "ARS")).toBe("Desconocido");
    expect(formatCurrency(undefined, "USD")).toBe("Desconocido");
  });

  it("keeps the currency type intentionally narrow", () => {
    const currency: CurrencyCode = "ARS";
    expect(currency).toBe("ARS");
  });
});

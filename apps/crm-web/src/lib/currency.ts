export type CurrencyCode = "ARS" | "USD";

const currencyLabels: Record<CurrencyCode, string> = {
  ARS: "$",
  USD: "US$",
};

export function formatCurrency(value: number | null | undefined, currency: CurrencyCode): string {
  if (value === null || value === undefined || Number.isNaN(value)) return "Desconocido";
  const formatted = new Intl.NumberFormat("es-AR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
  return `${currencyLabels[currency]} ${formatted}`;
}

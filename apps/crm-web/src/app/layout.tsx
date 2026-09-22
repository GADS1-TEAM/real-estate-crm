import "@fontsource/inter/400.css";
import "@fontsource/inter/500.css";
import "./globals.css";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "CRM · Inmobiliaria Norte",
  description: "CRM inmobiliario",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="es" suppressHydrationWarning><body suppressHydrationWarning>{children}</body></html>;
}

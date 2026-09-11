import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Platform Admin",
  description: "Configuración, versionado y gobierno de la plataforma inmobiliaria.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="es"><body>{children}</body></html>;
}

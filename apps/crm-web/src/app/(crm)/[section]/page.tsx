import { CrmApp } from "@/components/crm-workspace";
import { Suspense } from "react";

export function generateStaticParams() {
  return ["inicio", "contactos", "inmuebles", "publicaciones", "captaciones", "busquedas", "compatibilidades", "oportunidades", "actividad", "metricas", "asistente", "administracion"]
    .map((section) => ({ section }));
}

export default async function BusinessSectionPage({ params }: { params: Promise<{ section: string }> }) {
  const { section } = await params;
  return <Suspense fallback={<main>Cargando CRM…</main>}><CrmApp initialSection={section} /></Suspense>;
}

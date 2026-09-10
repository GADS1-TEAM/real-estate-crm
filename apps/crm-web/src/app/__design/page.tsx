import { Suspense } from "react";
import { CrmApp } from "@/components/crm-workspace";

export default function DesignCatalogPage() {
  return <Suspense fallback={<div className="catalog-loading">Cargando catálogo…</div>}><CrmApp initialSection="inicio" catalogOnly /></Suspense>;
}

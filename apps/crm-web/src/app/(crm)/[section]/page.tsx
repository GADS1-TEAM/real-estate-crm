import { CrmApp } from "@/components/crm-workspace";

export default async function BusinessSectionPage({ params }: { params: Promise<{ section: string }> }) {
  const { section } = await params;
  return <CrmApp initialSection={section} />;
}

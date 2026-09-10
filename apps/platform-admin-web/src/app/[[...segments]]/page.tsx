import { PlatformApp } from "@/components/platform-app";
import { getScreenByRoute } from "@/lib/screen-registry";

type PageProps = {
  params: Promise<{ segments?: string[] }>;
  searchParams: Promise<Record<string, string | string[] | undefined>>;
};

export default async function ProductPage({ params, searchParams }: PageProps) {
  const [{ segments }, query] = await Promise.all([params, searchParams]);
  const path = `/${(segments ?? []).join("/")}` || "/";
  const explicitScreen = typeof query.screen === "string" ? query.screen : undefined;
  const routeScreen = getScreenByRoute(path);
  return <PlatformApp initialScreenId={explicitScreen ?? routeScreen?.id ?? "PA-001"} />;
}

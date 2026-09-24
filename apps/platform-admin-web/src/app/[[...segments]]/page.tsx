import { PlatformApp } from "@/components/platform-app";
import { getScreenByRoute, screenRegistry } from "@/lib/screen-registry";

type PageProps = {
  params: Promise<{ segments?: string[] }>;
};

export function generateStaticParams() {
  return [...new Set(screenRegistry.map((screen) => screen.route))]
    .map((route) => ({ segments: route === "/" ? [] : route.slice(1).split("/") }));
}

export default async function ProductPage({ params }: PageProps) {
  const { segments } = await params;
  const path = `/${(segments ?? []).join("/")}` || "/";
  const routeScreen = getScreenByRoute(path);
  return <PlatformApp initialScreenId={routeScreen?.id ?? "PA-001"} />;
}

import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  poweredByHeader: false,
  ...(process.env.CLOUDFLARE_STATIC_EXPORT === "true"
    ? { output: "export" as const }
    : { async rewrites() { return [{ source: "/__design", destination: "/design" }]; } }),
};

export default nextConfig;

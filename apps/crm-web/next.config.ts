import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  distDir: process.env.CRM_WEB_DIST_DIR || ".next",
  reactStrictMode: true,
  poweredByHeader: false,
  async rewrites() {
    return [{ source: "/__design", destination: "/design" }];
  },
};

export default nextConfig;

import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  poweredByHeader: false,
  env: {
    NEXT_PUBLIC_PLATFORM_ADMIN_MODE: process.env.NEXT_PUBLIC_PLATFORM_ADMIN_MODE ?? "bff",
    NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL: process.env.NEXT_PUBLIC_PLATFORM_ADMIN_BFF_URL ?? "",
    NEXT_PUBLIC_PLATFORM_ADMIN_ENVIRONMENT: process.env.NEXT_PUBLIC_PLATFORM_ADMIN_ENVIRONMENT ?? "Development",
  },
};

export default nextConfig;

import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Imagen mínima de producción para Docker/Render: genera
  // .next/standalone/server.js sin necesitar node_modules completo.
  output: "standalone",
  async headers() {
    return [
      {
        source: "/sw.js",
        headers: [
          {
            key: "Content-Type",
            value: "application/javascript; charset=utf-8",
          },
          {
            key: "Cache-Control",
            value: "no-cache, no-store, must-revalidate",
          },
        ],
      },
    ];
  },
};

export default nextConfig;

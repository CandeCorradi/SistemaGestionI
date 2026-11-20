import type { Metadata, Viewport } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import 'bootstrap/dist/css/bootstrap.min.css';
import 'bootstrap-icons/font/bootstrap-icons.css';
import { AuthProvider } from '@/features/auth/auth-provider';
import { ServiceWorkerRegister } from '@/components/pwa/service-worker-register';

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    default: "Sistema de Gestión",
    template: "%s | Sistema de Gestión",
  },
  description:
    "Gestión integral de carpintería: clientes, proveedores, productos, presupuestos y pedidos.",
  applicationName: "Sistema de Gestión",
  appleWebApp: {
    capable: true,
    title: "Sistema Gestión",
    statusBarStyle: "default",
  },
};

export const viewport: Viewport = {
  themeColor: "#1f2937",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body>
        <AuthProvider>{children}</AuthProvider>
        <ServiceWorkerRegister />
      </body>
    </html>
  );
}

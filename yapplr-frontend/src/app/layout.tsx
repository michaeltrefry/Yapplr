'use client';


import { Inter } from "next/font/google";
import "./globals.css";
import { AuthProvider } from "@/contexts/AuthContext";
import { NotificationProvider } from "@/contexts/NotificationContext";
import { ThemeProvider } from "@/contexts/ThemeContext";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, useEffect } from "react";
import { NotificationStatusBadge } from "@/components/NotificationStatus";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const [queryClient] = useState(() => new QueryClient());

  useEffect(() => {
    // Remove preload class after component mounts to enable transitions
    document.body.classList.remove('preload');
  }, []);

  return (
    <html lang="en">
      <head>
        <title>Yapplr - Social Media</title>
        <meta name="description" content="Chaotic good. Or at least chaotic." />
        <link rel="icon" href="/logo-32.png" type="image/png" />
        <link rel="apple-touch-icon" href="/logo-128.png" />
        <meta name="theme-color" content="#3B82F6" />
      </head>
      <body className={`${inter.variable} font-sans antialiased preload`}>
        <QueryClientProvider client={queryClient}>
          <AuthProvider>
            <ThemeProvider>
              <NotificationProvider>
                {children}
                <NotificationStatusBadge />
              </NotificationProvider>
            </ThemeProvider>
          </AuthProvider>
        </QueryClientProvider>
      </body>
    </html>
  );
}

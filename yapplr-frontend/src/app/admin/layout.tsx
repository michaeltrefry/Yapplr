'use client';

import { useAuth } from '@/contexts/AuthContext';
import { UserRole } from '@/types';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import Link from 'next/link';
import Image from 'next/image';
import { usePathname } from 'next/navigation';
import {
  Shield,
  Users,
  FileText,
  MessageSquare,
  BarChart3,
  Settings,
  Tag,
  AlertTriangle,
  FileSearch,
  Scale,
  Home,
  Edit,
} from 'lucide-react';

interface AdminLayoutProps {
  children: React.ReactNode;
}

const adminNavItems = [
  {
    name: 'Dashboard',
    href: '/admin',
    icon: Home,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Content Queue',
    href: '/admin/queue',
    icon: AlertTriangle,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Users',
    href: '/admin/users',
    icon: Users,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Posts',
    href: '/admin/posts',
    icon: FileText,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Comments',
    href: '/admin/comments',
    icon: MessageSquare,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'System Tags',
    href: '/admin/system-tags',
    icon: Tag,
    roles: [UserRole.Admin],
  },
  {
    name: 'Appeals',
    href: '/admin/appeals',
    icon: Scale,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Audit Logs',
    href: '/admin/audit-logs',
    icon: FileSearch,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Analytics',
    href: '/admin/analytics',
    icon: BarChart3,
    roles: [UserRole.Moderator, UserRole.Admin],
  },
  {
    name: 'Content Management',
    href: '/admin/content',
    icon: Edit,
    roles: [UserRole.Admin],
  },
];

export default function AdminLayout({ children }: AdminLayoutProps) {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [isAuthorized, setIsAuthorized] = useState(false);

  useEffect(() => {
    if (!isLoading) {
      if (!user) {
        router.push('/login');
        return;
      }

      if (!user.role || (user.role !== UserRole.Moderator && user.role !== UserRole.Admin)) {
        router.push('/');
        return;
      }

      setIsAuthorized(true);
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (!isAuthorized) {
    return null;
  }

  const filteredNavItems = adminNavItems.filter(item => 
    item.roles.includes(user!.role!)
  );

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <Image
                src="/logo-64.png"
                alt="Yapplr Logo"
                width={40}
                height={40}
                className="w-10 h-10 mr-3"
              />
              <Shield className="h-8 w-8 text-blue-600 mr-3" />
              <h1 className="text-2xl font-semibold text-gray-900">
                Yapplr Admin
              </h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-600">
                {user?.role === UserRole.Admin ? 'Administrator' : 'Moderator'}
              </span>
              <span className="text-sm font-medium text-gray-900">
                @{user?.username}
              </span>
              <Link
                href="/"
                className="text-sm text-blue-600 hover:text-blue-800"
              >
                Back to App
              </Link>
            </div>
          </div>
        </div>
      </header>

      <div className="flex">
        {/* Sidebar */}
        <nav className="w-64 bg-white shadow-sm min-h-screen">
          <div className="p-4">
            <ul className="space-y-2">
              {filteredNavItems.map((item) => {
                const Icon = item.icon;
                const isActive = pathname === item.href;
                
                return (
                  <li key={item.name}>
                    <Link
                      href={item.href}
                      className={`flex items-center px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                        isActive
                          ? 'bg-blue-100 text-blue-700'
                          : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                      }`}
                    >
                      <Icon className="h-5 w-5 mr-3" />
                      {item.name}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        </nav>

        {/* Main Content */}
        <main className="flex-1 p-6">
          {children}
        </main>
      </div>
    </div>
  );
}

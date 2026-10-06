import React, { useEffect } from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth-store';
import { LayoutDashboard, Monitor, Shield, Building2, Settings, LogOut, Menu } from 'lucide-react';
import { startSignalR, stopSignalR } from '@/lib/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { cn } from '@/lib/utils';
import { Toaster } from '@/components/ui/toast';

const navItems = [
  { href: '/', label: 'Dashboard', icon: LayoutDashboard },
  { href: '/machines', label: 'Máquinas', icon: Monitor },
  { href: '/rules', label: 'Regras', icon: Shield },
  { href: '/sectors', label: 'Setores', icon: Building2 },
  { href: '/settings', label: 'Configurações', icon: Settings },
];

export function DashboardLayout() {
  const { user, logout } = useAuthStore();
  const location = useLocation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isMobileOpen, setIsMobileOpen] = React.useState(false);

  useEffect(() => {
    startSignalR(queryClient);
    return () => {
      stopSignalR();
    };
  }, [queryClient]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <Toaster />
      {/* Mobile Sidebar Overlay */}
      {isMobileOpen && (
        <div className="fixed inset-0 z-40 bg-black/50 md:hidden" onClick={() => setIsMobileOpen(false)} />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-50 w-64 transform bg-brand-sidebar text-white transition-transform duration-200 ease-in-out md:static md:translate-x-0",
          isMobileOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <div 
          className="flex h-16 items-center px-6 cursor-pointer hover:opacity-80 transition-opacity" 
          onClick={() => window.location.reload()}
          title="Clique para atualizar a página"
        >
          <Shield className="mr-2 h-6 w-6 text-brand-accent" />
          <span className="text-xl font-bold">Crivo</span>
        </div>
        <nav className="mt-6 space-y-1 px-4">
          {navItems.map(item => {
            const isActive = location.pathname === item.href;
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                to={item.href}
                onClick={() => setIsMobileOpen(false)}
                className={cn(
                  "flex items-center rounded-md px-2 py-2 text-sm font-medium transition-colors",
                  isActive ? "bg-brand-dark text-white" : "text-gray-300 hover:bg-brand-dark/50 hover:text-white"
                )}
              >
                <Icon className="mr-3 h-5 w-5" />
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>

      {/* Main Content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        <header className="flex h-16 items-center justify-between border-b bg-white px-4 md:px-6">
          <div className="flex items-center">
            <button className="mr-4 md:hidden" onClick={() => setIsMobileOpen(true)}>
              <Menu className="h-6 w-6 text-gray-500" />
            </button>
            <h1 className="text-xl font-semibold text-gray-800">
              {navItems.find(n => n.href === location.pathname)?.label || 'Painel'}
            </h1>
          </div>
          <div className="flex items-center space-x-4">
            <div className="hidden flex-col items-end md:flex">
              <span className="text-sm font-medium text-gray-900">{user?.name}</span>
              <span className="text-xs text-gray-500">{user?.tenantName}</span>
            </div>
            <button
              onClick={handleLogout}
              className="flex items-center rounded-md p-2 text-gray-500 hover:bg-gray-100 hover:text-gray-700"
            >
              <LogOut className="h-5 w-5" />
            </button>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

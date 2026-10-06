import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/auth-store';
import { authApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Shield } from 'lucide-react';
import { toast, Toaster } from '@/components/ui/toast';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const login = useAuthStore(state => state.login);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const res = await authApi.login({ email, password });
      login(res.token, { name: res.user?.name, role: res.user?.role, tenantName: res.user?.tenantName });
      navigate('/');
    } catch (err: any) {
      toast({
        title: 'Erro no login',
        description: err.response?.data?.message || 'Verifique suas credenciais e tente novamente.',
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50">
      <Toaster />
      <div className="w-full max-w-md space-y-8 rounded-lg bg-white p-8 shadow-lg">
        <div className="flex flex-col items-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-brand-accent text-white">
            <Shield size={24} />
          </div>
          <h2 className="mt-4 text-3xl font-bold tracking-tight text-gray-900">Crivo</h2>
          <p className="mt-2 text-sm text-gray-600">Painel de Controle</p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">Email</label>
              <Input
                type="email"
                required
                value={email}
                onChange={e => setEmail(e.target.value)}
                className="mt-1"
                placeholder="seu@email.com"
              />
            </div>
            <div>
              <label className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">Senha</label>
              <Input
                type="password"
                required
                value={password}
                onChange={e => setPassword(e.target.value)}
                className="mt-1"
              />
            </div>
          </div>

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? 'Entrando...' : 'Entrar'}
          </Button>
        </form>
      </div>
    </div>
  );
}

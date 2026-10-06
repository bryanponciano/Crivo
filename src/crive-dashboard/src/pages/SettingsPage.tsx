import React, { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { tenantsApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { toast } from '@/components/ui/toast';

export default function SettingsPage() {
  const [passwords, setPasswords] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });

  const mutation = useMutation({
    mutationFn: (data: any) => tenantsApi.updateUninstallPassword(data),
    onSuccess: () => {
      toast({ title: 'Sucesso', description: 'Senha atualizada.', variant: 'success' });
      setPasswords({ currentPassword: '', newPassword: '', confirmPassword: '' });
    },
    onError: (err: any) => {
      toast({ 
        title: 'Erro', 
        description: err.response?.data?.message || 'Falha ao atualizar senha.', 
        variant: 'destructive' 
      });
    }
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (passwords.newPassword !== passwords.confirmPassword) {
      toast({ title: 'Erro', description: 'As senhas não coincidem.', variant: 'destructive' });
      return;
    }
    mutation.mutate({
      currentPassword: passwords.currentPassword,
      newPassword: passwords.newPassword
    });
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <h2 className="text-xl font-semibold">Configurações</h2>
      
      <Card>
        <CardHeader>
          <CardTitle>Senha de Desinstalação</CardTitle>
          <CardDescription>
            Esta senha é exigida quando um administrador tenta desinstalar ou parar o agente Crivo nas máquinas.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="text-sm font-medium">Senha Atual</label>
              <Input
                type="password"
                required
                value={passwords.currentPassword}
                onChange={e => setPasswords({ ...passwords, currentPassword: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Nova Senha</label>
              <Input
                type="password"
                required
                value={passwords.newPassword}
                onChange={e => setPasswords({ ...passwords, newPassword: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium">Confirmar Nova Senha</label>
              <Input
                type="password"
                required
                value={passwords.confirmPassword}
                onChange={e => setPasswords({ ...passwords, confirmPassword: e.target.value })}
              />
            </div>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? 'Salvando...' : 'Atualizar Senha'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

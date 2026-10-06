import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sectorsApi } from '@/lib/api';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { toast } from '@/components/ui/toast';
import { Trash2, Building2 } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export default function SectorsPage() {
  const navigate = useNavigate();
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [formData, setFormData] = useState({ name: '', description: '' });
  
  const queryClient = useQueryClient();

  const { data: sectors, isLoading } = useQuery({
    queryKey: ['sectors'],
    queryFn: sectorsApi.getAll,
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => sectorsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sectors'] });
      setIsDialogOpen(false);
      setFormData({ name: '', description: '' });
      toast({ title: 'Sucesso', description: 'Setor criado com sucesso.', variant: 'success' });
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => sectorsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['sectors'] });
      toast({ title: 'Sucesso', description: 'Setor removido.', variant: 'success' });
    }
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate(formData);
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Setores</h2>
        <Button onClick={() => setIsDialogOpen(true)}>Novo Setor</Button>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {isLoading ? (
          <div>Carregando...</div>
        ) : sectors?.length ? (
          sectors.map((sector: any) => (
            <Card 
              key={sector.id} 
              className="relative hover:border-brand-primary/50 transition-colors cursor-pointer" 
              onClick={() => navigate(`/machines?sector=${sector.id}`)}
            >
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg flex items-center">
                    <Building2 className="mr-2 h-5 w-5 text-gray-500" />
                    {sector.name}
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0 text-red-500 hover:text-red-700 z-10"
                    onClick={(e) => {
                      e.stopPropagation();
                      if (confirm('Tem certeza que deseja remover este setor?')) {
                        deleteMutation.mutate(sector.id);
                      }
                    }}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
                <CardDescription>{sector.description || 'Sem descrição'}</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="mt-2 flex items-center justify-between text-sm text-gray-500">
                  <span>Visualizar Máquinas &rarr;</span>
                </div>
              </CardContent>
            </Card>
          ))
        ) : (
          <div className="col-span-full text-center text-gray-500">
            Nenhum setor encontrado.
          </div>
        )}
      </div>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Novo Setor</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="text-sm font-medium">Nome</label>
              <Input
                required
                value={formData.name}
                onChange={e => setFormData({ ...formData, name: e.target.value })}
                placeholder="Ex: Diretoria"
              />
            </div>
            <div>
              <label className="text-sm font-medium">Descrição (Opcional)</label>
              <Input
                value={formData.description}
                onChange={e => setFormData({ ...formData, description: e.target.value })}
              />
            </div>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createMutation.isPending}>Salvar</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

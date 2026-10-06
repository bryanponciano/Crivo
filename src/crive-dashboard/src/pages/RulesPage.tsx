import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { rulesApi, sectorsApi, machinesApi } from '@/lib/api';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Select } from '@/components/ui/select';
import { toast } from '@/components/ui/toast';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Trash2, Edit2 } from 'lucide-react';

export default function RulesPage() {
  const [activeTab, setActiveTab] = useState('0'); // 0: Global, 1: Sector, 2: Machine
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [expandedRules, setExpandedRules] = useState<Set<string>>(new Set());
  const [deleteTarget, setDeleteTarget] = useState<{ id: string; name: string } | null>(null);
  
  const queryClient = useQueryClient();

  const { data: rules, isLoading } = useQuery({
    queryKey: ['rules', activeTab],
    queryFn: () => rulesApi.getAll(parseInt(activeTab)),
  });

  const { data: sectors } = useQuery({
    queryKey: ['sectors'],
    queryFn: sectorsApi.getAll,
  });

  const { data: machines } = useQuery({
    queryKey: ['machines-all'],
    queryFn: () => machinesApi.getAll(),
  });

  const toggleMutation = useMutation({
    mutationFn: (id: string) => rulesApi.toggle(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rules'] });
      toast({ title: 'Sucesso', description: 'Regra atualizada.', variant: 'success' });
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => rulesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rules'] });
      toast({ title: 'Sucesso', description: 'Regra removida.', variant: 'success' });
    }
  });

  const [formData, setFormData] = useState({
    name: '',
    scopeType: 0,
    targetId: '',
    action: 0, // 0: Block, 1: Allow
    domains: '',
    priority: 1,
  });

  const createMutation = useMutation({
    mutationFn: (data: any) => rulesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rules'] });
      setIsDialogOpen(false);
      toast({ title: 'Sucesso', description: 'Regra criada com sucesso.', variant: 'success' });
    }
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const domainsList = formData.domains.split('\n').map(d => d.trim()).filter(d => d);
    createMutation.mutate({
      ...formData,
      domains: domainsList,
      targetId: formData.scopeType === 0 ? null : formData.targetId
    });
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Regras de Acesso</h2>
        <Button onClick={() => setIsDialogOpen(true)}>Nova Regra</Button>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList>
          <TabsTrigger value="0">Global</TabsTrigger>
          <TabsTrigger value="1">Por Setor</TabsTrigger>
          <TabsTrigger value="2">Exceções Individuais</TabsTrigger>
        </TabsList>

        <div className="mt-4 rounded-md border bg-white">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Nome</TableHead>
                <TableHead>Domínios</TableHead>
                <TableHead>Ação</TableHead>
                {activeTab !== '0' && <TableHead>Alvo</TableHead>}
                <TableHead>Ativo</TableHead>
                <TableHead className="text-right">Ações</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center">Carregando...</TableCell>
                </TableRow>
              ) : rules?.length ? (
                rules.map((rule: any) => (
                  <TableRow key={rule.id}>
                    <TableCell className="font-medium">{rule.name}</TableCell>
                    <TableCell>
                      <div
                        className="flex flex-wrap gap-1 cursor-pointer"
                        onClick={() => {
                          const next = new Set(expandedRules);
                          if (next.has(rule.id)) next.delete(rule.id);
                          else next.add(rule.id);
                          setExpandedRules(next);
                        }}
                        title="Clique para ver todos os domínios"
                      >
                        {(expandedRules.has(rule.id) ? rule.domains : rule.domains.slice(0, 3)).map((d: string) => (
                          <Badge key={d} variant="outline" className="text-xs">{d}</Badge>
                        ))}
                        {!expandedRules.has(rule.id) && rule.domains.length > 3 && (
                          <Badge variant="secondary" className="text-xs cursor-pointer hover:bg-blue-100">
                            +{rule.domains.length - 3} ver mais
                          </Badge>
                        )}
                        {expandedRules.has(rule.id) && rule.domains.length > 3 && (
                          <Badge variant="secondary" className="text-xs cursor-pointer hover:bg-blue-100">
                            ▲ recolher
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      {rule.action === 0 ? (
                        <Badge variant="destructive">Bloquear</Badge>
                      ) : (
                        <Badge variant="success">Liberar</Badge>
                      )}
                    </TableCell>
                    {activeTab !== '0' && (
                      <TableCell>{rule.targetName || rule.targetId}</TableCell>
                    )}
                    <TableCell>
                      <Switch
                        checked={rule.isActive}
                        onCheckedChange={() => toggleMutation.mutate(rule.id)}
                      />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => setDeleteTarget({ id: rule.id, name: rule.name })}
                      >
                        <Trash2 className="h-4 w-4 text-red-500" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={6} className="text-center text-muted-foreground">
                    Nenhuma regra encontrada.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </Tabs>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>Nova Regra</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="text-sm font-medium">Nome da Regra</label>
              <Input
                required
                value={formData.name}
                onChange={e => setFormData({ ...formData, name: e.target.value })}
              />
            </div>
            
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium">Escopo</label>
                <Select
                  value={formData.scopeType.toString()}
                  onChange={e => setFormData({ ...formData, scopeType: parseInt(e.target.value) })}
                >
                  <option value="0">Global</option>
                  <option value="1">Setor</option>
                  <option value="2">Máquina (Exceção)</option>
                </Select>
              </div>
              <div>
                <label className="text-sm font-medium">Ação</label>
                <Select
                  value={formData.action.toString()}
                  onChange={e => setFormData({ ...formData, action: parseInt(e.target.value) })}
                >
                  <option value="0">Bloquear</option>
                  <option value="1">Liberar</option>
                </Select>
              </div>
            </div>

            {formData.scopeType === 1 && (
              <div>
                <label className="text-sm font-medium">Setor Alvo</label>
                <Select
                  required
                  value={formData.targetId}
                  onChange={e => setFormData({ ...formData, targetId: e.target.value })}
                >
                  <option value="">Selecione um setor...</option>
                  {sectors?.map((s: any) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </Select>
              </div>
            )}

            {formData.scopeType === 2 && (
              <div>
                <label className="text-sm font-medium">Máquina Alvo</label>
                <Select
                  required
                  value={formData.targetId}
                  onChange={e => setFormData({ ...formData, targetId: e.target.value })}
                >
                  <option value="">Selecione uma máquina...</option>
                  {machines?.map((m: any) => (
                    <option key={m.id} value={m.id}>{m.hostname} - {m.employeeName}</option>
                  ))}
                </Select>
              </div>
            )}

            <div>
              <label className="text-sm font-medium">Domínios (um por linha)</label>
              <textarea
                required
                className="flex min-h-[100px] w-full rounded-md border border-input bg-white text-gray-900 px-3 py-2 text-sm ring-offset-background placeholder:text-gray-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                value={formData.domains}
                onChange={e => setFormData({ ...formData, domains: e.target.value })}
                placeholder="exemplo.com&#10;youtube.com"
              />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)}>Cancelar</Button>
              <Button type="submit" disabled={createMutation.isPending}>Salvar</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!deleteTarget}
        title="Excluir Regra"
        description={`Tem certeza que deseja excluir a regra "${deleteTarget?.name}"? Esta ação não pode ser desfeita.`}
        onConfirm={() => {
          if (deleteTarget) {
            deleteMutation.mutate(deleteTarget.id);
            setDeleteTarget(null);
          }
        }}
        onCancel={() => setDeleteTarget(null)}
        loading={deleteMutation.isPending}
      />
    </div>
  );
}

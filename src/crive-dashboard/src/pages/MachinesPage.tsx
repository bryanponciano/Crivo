import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { machinesApi, sectorsApi } from '@/lib/api';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Select } from '@/components/ui/select';
import { useSearchParams } from 'react-router-dom';
import { format } from 'date-fns';

export default function MachinesPage() {
  const [searchParams] = useSearchParams();
  const [sectorId, setSectorId] = useState(searchParams.get('sector') || '');

  const { data: sectors } = useQuery({
    queryKey: ['sectors'],
    queryFn: sectorsApi.getAll,
  });

  const { data: machines, isLoading } = useQuery({
    queryKey: ['machines', sectorId],
    queryFn: () => machinesApi.getAll(sectorId || undefined),
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-medium">Lista de Máquinas</h2>
        <div className="w-64">
          <Select value={sectorId} onChange={(e) => setSectorId(e.target.value)}>
            <option value="">Todos os Setores</option>
            {sectors?.map((s: any) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </Select>
        </div>
      </div>

      <div className="rounded-md border bg-white">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Status</TableHead>
              <TableHead>Patrimônio</TableHead>
              <TableHead>Colaborador</TableHead>
              <TableHead>Setor</TableHead>
              <TableHead>Versão</TableHead>
              <TableHead>Último Heartbeat</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center">Carregando...</TableCell>
              </TableRow>
            ) : machines?.length ? (
              machines.map((m: any) => (
                <TableRow key={m.id}>
                  <TableCell>
                    {m.status === 0 || m.status === 'Online' ? (
                      <Badge variant="success">Online</Badge>
                    ) : (
                      <Badge variant="destructive">Offline</Badge>
                    )}
                  </TableCell>
                  <TableCell className="font-medium">{m.hostname}</TableCell>
                  <TableCell>{m.employeeName || '-'}</TableCell>
                  <TableCell>{m.sectorName || '-'}</TableCell>
                  <TableCell>{m.agentVersion || '-'}</TableCell>
                  <TableCell>
                    {m.lastHeartbeat ? format(new Date(m.lastHeartbeat), 'dd/MM/yyyy HH:mm') : '-'}
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-muted-foreground">
                  Nenhuma máquina encontrada.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

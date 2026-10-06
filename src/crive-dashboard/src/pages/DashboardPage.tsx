import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Monitor, Activity, MonitorOff, ShieldAlert } from 'lucide-react';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { format } from 'date-fns';

export default function DashboardPage() {
  const { data: summary } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: dashboardApi.getSummary,
    refetchInterval: 30000,
  });

  const { data: blocksData } = useQuery({
    queryKey: ['dashboard-blocks'],
    queryFn: () => dashboardApi.getBlocks(1, 10),
  });

  return (
    <div className="space-y-6">
      {/* Stats */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Total Máquinas</CardTitle>
            <Monitor className="h-4 w-4 text-brand-accent" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary?.totalMachines || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Online</CardTitle>
            <Activity className="h-4 w-4 text-green-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{summary?.onlineMachines || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Offline</CardTitle>
            <MonitorOff className="h-4 w-4 text-red-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">{summary?.offlineMachines || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Bloqueios Hoje</CardTitle>
            <ShieldAlert className="h-4 w-4 text-orange-500" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{summary?.blocksToday || 0}</div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Blocks Table */}
      <Card>
        <CardHeader>
          <CardTitle>Bloqueios Recentes</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Data/Hora</TableHead>
                <TableHead>Colaborador</TableHead>
                <TableHead>Domínio</TableHead>
                <TableHead>Regra Aplicada</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {blocksData?.items?.length ? (
                blocksData.items.map((block: any) => (
                  <TableRow key={block.id}>
                    <TableCell>{format(new Date(block.timestamp), 'dd/MM/yyyy HH:mm:ss')}</TableCell>
                    <TableCell>{block.employeeName}</TableCell>
                    <TableCell className="font-mono text-xs">{block.domain}</TableCell>
                    <TableCell>{block.ruleName || '-'}</TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-muted-foreground">
                    Nenhum bloqueio registrado.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}

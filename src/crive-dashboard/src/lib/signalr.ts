import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useAuthStore } from '../stores/auth-store';

let connection: HubConnection | null = null;

export const startSignalR = async (queryClient: any) => {
  if (connection) return;

  const token = useAuthStore.getState().token;
  if (!token) return;

  connection = new HubConnectionBuilder()
    .withUrl('/hubs/dashboard', {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();

  connection.on('MachineStatusChanged', (machineId, status) => {
    queryClient.invalidateQueries({ queryKey: ['machines'] });
    queryClient.invalidateQueries({ queryKey: ['dashboard-summary'] });
  });

  connection.on('NewBlockEvent', (event) => {
    queryClient.invalidateQueries({ queryKey: ['dashboard-blocks'] });
  });

  connection.on('RuleSyncConfirmed', (machineId, ruleId) => {
    // Optionally trigger toast or refetch
  });

  try {
    await connection.start();
    console.log('SignalR Connected.');
  } catch (err) {
    console.error('SignalR Connection Error: ', err);
  }
};

export const stopSignalR = () => {
  if (connection) {
    connection.stop();
    connection = null;
  }
};

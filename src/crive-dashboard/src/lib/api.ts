import axios from 'axios';
import { useAuthStore } from '../stores/auth-store';

const api = axios.create({
  baseURL: '/api',
});

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const authApi = {
  login: (data: any) => api.post('/auth/login', data).then(res => res.data),
};

export const sectorsApi = {
  getAll: () => api.get('/sectors').then(res => res.data),
  create: (data: any) => api.post('/sectors', data).then(res => res.data),
  update: (id: string, data: any) => api.put(`/sectors/${id}`, data).then(res => res.data),
  delete: (id: string) => api.delete(`/sectors/${id}`).then(res => res.data),
};

export const machinesApi = {
  getAll: (sectorId?: string) => api.get('/machines', { params: { sectorId } }).then(res => res.data),
  getById: (id: string) => api.get(`/machines/${id}`).then(res => res.data),
  delete: (id: string) => api.delete(`/machines/${id}`).then(res => res.data),
};

export const rulesApi = {
  getAll: (scopeType?: number) => api.get('/rules', { params: { scopeType } }).then(res => res.data),
  create: (data: any) => api.post('/rules', data).then(res => res.data),
  update: (id: string, data: any) => api.put(`/rules/${id}`, data).then(res => res.data),
  toggle: (id: string) => api.put(`/rules/${id}/toggle`).then(res => res.data),
  delete: (id: string) => api.delete(`/rules/${id}`).then(res => res.data),
};

export const dashboardApi = {
  getSummary: () => api.get('/dashboard/summary').then(res => res.data),
  getBlocks: (page = 1, pageSize = 20) => api.get('/dashboard/blocks', { params: { page, pageSize } }).then(res => res.data),
};

export const tenantsApi = {
  updateUninstallPassword: (data: any) => api.put('/tenants/uninstall-password', data).then(res => res.data),
};

export default api;

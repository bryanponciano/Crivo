import { Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import LoginPage from '@/pages/LoginPage';
import DashboardPage from '@/pages/DashboardPage';
import MachinesPage from '@/pages/MachinesPage';
import RulesPage from '@/pages/RulesPage';
import SectorsPage from '@/pages/SectorsPage';
import SettingsPage from '@/pages/SettingsPage';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <DashboardLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="machines" element={<MachinesPage />} />
        <Route path="rules" element={<RulesPage />} />
        <Route path="sectors" element={<SectorsPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
    </Routes>
  );
}

export default App;

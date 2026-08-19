import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import { ToastProvider } from './context/ToastContext';
import { ConfirmProvider } from './context/ConfirmContext';
import AppLayout from './components/layout/AppLayout';
import AlertsPage from './pages/alerts/AlertsPage';
import DashboardPage from './pages/DashboardPage';
import DevicesPage from './pages/devices/DevicesPage';
import DeviceDetailPage from './pages/devices/DeviceDetailPage';
import MillsPage from './pages/organisation/MillsPage';
import AreasPage from './pages/areas/AreasPage';
import ConfigurationPage from './pages/configuration/ConfigurationPage';
import ReportsPage from './pages/reports/ReportsPage';
import UsersPage from './pages/users/UsersPage';
import WorkOrdersPage from './pages/workorders/WorkOrdersPage';
import EnergyPage from './pages/energy/EnergyPage';
import HealthPage from './pages/health/HealthPage';
import FloorPlanPage from './pages/floorplan/FloorPlanPage';
import AuditPage from './pages/audit/AuditPage';
import IntegrationsPage from './pages/integrations/IntegrationsPage';
import AskPage from './pages/ask/AskPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard',     element: <DashboardPage />,     handle: { titleKey: 'nav.dashboard' } },
      { path: 'alerts',        element: <AlertsPage />,        handle: { titleKey: 'nav.alerts' } },
      { path: 'devices',       element: <DevicesPage />,       handle: { titleKey: 'nav.devices' } },
      { path: 'devices/:id',   element: <DeviceDetailPage />,  handle: { titleKey: 'nav.devices' } },
      { path: 'mills',         element: <MillsPage />,         handle: { titleKey: 'nav.mills' } },
      { path: 'areas',         element: <AreasPage />,         handle: { titleKey: 'nav.areas' } },
      { path: 'workorders',    element: <WorkOrdersPage />,    handle: { titleKey: 'nav.workorders' } },
      { path: 'floorplan',     element: <FloorPlanPage />,     handle: { titleKey: 'nav.floorplan' } },
      { path: 'health',        element: <HealthPage />,        handle: { titleKey: 'nav.health' } },
      { path: 'ask',           element: <AskPage />,           handle: { titleKey: 'nav.ask' } },
      { path: 'energy',        element: <EnergyPage />,        handle: { titleKey: 'nav.energy' } },
      { path: 'reports',       element: <ReportsPage />,       handle: { titleKey: 'nav.reports' } },
      { path: 'users',         element: <UsersPage />,         handle: { titleKey: 'nav.users' } },
      { path: 'audit',         element: <AuditPage />,         handle: { titleKey: 'nav.audit' } },
      { path: 'integrations',  element: <IntegrationsPage />,  handle: { titleKey: 'nav.integrations' } },
      { path: 'configuration', element: <ConfigurationPage />, handle: { titleKey: 'nav.configuration' } },
    ],
  },
]);

export default function App() {
  return (
    <ToastProvider>
      <ConfirmProvider>
        <RouterProvider router={router} />
      </ConfirmProvider>
    </ToastProvider>
  );
}

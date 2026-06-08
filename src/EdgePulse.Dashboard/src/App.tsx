import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import AlertsPage from './pages/alerts/AlertsPage';
import DashboardPage from './pages/DashboardPage';
import DevicesPage from './pages/devices/DevicesPage';
import DeviceDetailPage from './pages/devices/DeviceDetailPage';
import MillsPage from './pages/organisation/MillsPage';
import AreasPage from './pages/areas/AreasPage';
import ConfigurationPage from './pages/configuration/ConfigurationPage';

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
      { path: 'configuration', element: <ConfigurationPage />, handle: { titleKey: 'nav.configuration' } },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}

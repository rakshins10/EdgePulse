import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import AlertsPage from './pages/alerts/AlertsPage';
import DashboardPage from './pages/DashboardPage';
import DevicesPage from './pages/devices/DevicesPage';
import DeviceDetailPage from './pages/devices/DeviceDetailPage';
import MillsPage from './pages/organisation/MillsPage';
import AreasPage from './pages/areas/AreasPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard',     element: <DashboardPage />,     handle: { title: 'Dashboard' } },
      { path: 'alerts',        element: <AlertsPage />,        handle: { title: 'Alerts' } },
      { path: 'devices',       element: <DevicesPage />,       handle: { title: 'Devices' } },
      { path: 'devices/:id',   element: <DeviceDetailPage />,  handle: { title: 'Device' } },
      { path: 'mills',         element: <MillsPage />,         handle: { title: 'Mills' } },
      { path: 'areas',         element: <AreasPage />,         handle: { title: 'Areas' } },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}

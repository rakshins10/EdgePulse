import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import AlertsPage from './pages/alerts/AlertsPage';
import DashboardPage from './pages/DashboardPage';
import PlaceholderPage from './pages/placeholder/PlaceholderPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      {
        path: 'dashboard',
        element: <DashboardPage />,
        handle: { title: 'Dashboard' },
      },
      {
        path: 'alerts',
        element: <AlertsPage />,
        handle: { title: 'Alerts' },
      },
      {
        path: 'devices',
        element: <PlaceholderPage title="Devices" />,
        handle: { title: 'Devices' },
      },
      {
        path: 'mills',
        element: <PlaceholderPage title="Mills" />,
        handle: { title: 'Mills' },
      },
      {
        path: 'areas',
        element: <PlaceholderPage title="Areas" />,
        handle: { title: 'Areas' },
      },
    ],
  },
]);

export default function App() {
  return <RouterProvider router={router} />;
}

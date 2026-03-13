import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

export default function AdminRoute() {
  const { user } = useAuth()
  return user?.role === 'Admin' ? <Outlet /> : <Navigate to="/contacts" replace />
}

import { Navigate } from 'react-router-dom'
import { isAuthenticated, getRole } from '../utils/auth'

interface PrivateRouteProps {
  children: React.ReactNode
}

export default function PrivateRoute({ children }: PrivateRouteProps) {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />
  }

  if (getRole() === 'Employee') {
    return <Navigate to="/employee/dashboard" replace />
  }

  return <>{children}</>
}

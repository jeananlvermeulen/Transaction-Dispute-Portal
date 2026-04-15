import { Navigate } from 'react-router-dom'
import { isAuthenticated, getRole } from '../utils/auth'

interface EmployeeRouteProps {
  children: React.ReactNode
}

export default function EmployeeRoute({ children }: EmployeeRouteProps) {
  if (!isAuthenticated()) {
    return <Navigate to="/employee/login" replace />
  }

  if (getRole() !== 'Employee') {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}

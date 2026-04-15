import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { LanguageProvider } from './context/LanguageContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import DashboardPage from './pages/DashboardPage'
import TransactionsPage from './pages/TransactionsPage'
import DisputesPage from './pages/DisputesPage'
import ProfilePage from './pages/ProfilePage'
import PrivateRoute from './components/PrivateRoute'
import EmployeeRoute from './components/EmployeeRoute'
import EmployeeLoginPage from './pages/employee/EmployeeLoginPage'
import EmployeeRegisterPage from './pages/employee/EmployeeRegisterPage'
import EmployeeDashboardPage from './pages/employee/EmployeeDashboardPage'

export default function App() {
  return (
    <LanguageProvider>
    <Router>
      <Routes>
        {/* Customer routes */}
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route path="/dashboard" element={<PrivateRoute><DashboardPage /></PrivateRoute>} />
        <Route path="/transactions" element={<PrivateRoute><TransactionsPage /></PrivateRoute>} />
        <Route path="/disputes" element={<PrivateRoute><DisputesPage /></PrivateRoute>} />
        <Route path="/profile" element={<PrivateRoute><ProfilePage /></PrivateRoute>} />

        {/* Employee routes */}
        <Route path="/employee/login" element={<EmployeeLoginPage />} />
        <Route path="/employee/register" element={<EmployeeRegisterPage />} />
        <Route path="/employee/dashboard" element={<EmployeeRoute><EmployeeDashboardPage /></EmployeeRoute>} />

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Router>
    </LanguageProvider>
  )
}

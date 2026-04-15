import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import apiClient from '../../services/api'
import capitecLogo from '../../assets/symbol.png'

const DEPARTMENTS = ['Customer Service', 'Fraud & Disputes', 'Operations', 'Compliance', 'IT Support', 'Management']

const COUNTRIES = [
  { name: 'South Africa', code: 'ZA', dial: '+27', format: '082 123 4567' },
  { name: 'United States', code: 'US', dial: '+1', format: '202 555 0123' },
  { name: 'United Kingdom', code: 'GB', dial: '+44', format: '07911 123456' },
  { name: 'Australia', code: 'AU', dial: '+61', format: '0412 345 678' },
  { name: 'Canada', code: 'CA', dial: '+1', format: '613 555 0123' },
  { name: 'Germany', code: 'DE', dial: '+49', format: '01512 3456789' },
  { name: 'France', code: 'FR', dial: '+33', format: '06 12 34 56 78' },
  { name: 'India', code: 'IN', dial: '+91', format: '098765 43210' },
  { name: 'Nigeria', code: 'NG', dial: '+234', format: '0802 123 4567' },
  { name: 'Kenya', code: 'KE', dial: '+254', format: '0712 345 678' },
  { name: 'Zimbabwe', code: 'ZW', dial: '+263', format: '071 234 5678' },
  { name: 'Zambia', code: 'ZM', dial: '+260', format: '095 123 4567' },
  { name: 'Botswana', code: 'BW', dial: '+267', format: '071 234 567' },
  { name: 'Namibia', code: 'NA', dial: '+264', format: '081 123 4567' },
  { name: 'Mozambique', code: 'MZ', dial: '+258', format: '082 123 4567' },
  { name: 'China', code: 'CN', dial: '+86', format: '0131 2345 6789' },
  { name: 'Brazil', code: 'BR', dial: '+55', format: '011 91234 5678' },
  { name: 'Netherlands', code: 'NL', dial: '+31', format: '06 12345678' },
  { name: 'Singapore', code: 'SG', dial: '+65', format: '8123 4567' },
  { name: 'UAE', code: 'AE', dial: '+971', format: '050 123 4567' },
]

export default function EmployeeRegisterPage() {
  const navigate = useNavigate()
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    phoneNumber: '',
    department: DEPARTMENTS[0],
  })
  const [selectedCountry, setSelectedCountry] = useState(COUNTRIES[0])
  const [step, setStep] = useState<'form' | 'verify'>('form')
  const [verifyCode, setVerifyCode] = useState('')
  const [verifying, setVerifying] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [success, setSuccess] = useState(false)

  const validatePassword = (password: string) => {
    if (password.length < 8) return 'Password must be at least 8 characters long'
    if (!/[A-Z]/.test(password)) return 'Password must contain at least one uppercase letter'
    if (!/\d/.test(password)) return 'Password must contain at least one digit'
    if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)) return 'Password must contain at least one special character'
    return null
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    const passwordError = validatePassword(formData.password)
    if (passwordError) {
      setError(passwordError)
      return
    }
    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match.')
      return
    }
    setLoading(true)
    try {
      await apiClient.sendEmployeeRegistrationCode(formData.email, formData.firstName)
      setStep('verify')
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to send verification code. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setVerifying(true)
    try {
      const digits = formData.phoneNumber.replace(/\D/g, '')
      const dc = selectedCountry.dial.replace(/\D/g, '')
      const localDigits = digits.startsWith(dc) ? digits.slice(dc.length) : digits.replace(/^0/, '')
      const fullPhone = `${selectedCountry.dial}${localDigits}`

      const data = await apiClient.registerEmployee(
        formData.email, formData.password, formData.confirmPassword,
        formData.firstName, formData.lastName,
        fullPhone,
        formData.department,
        verifyCode
      )
      if (data.success) {
        setSuccess(true)
      } else {
        setError(data.message || 'Registration failed.')
      }
    } catch (err: any) {
      const validationErrors = err.response?.data?.errors
      if (validationErrors) {
        setError(Object.values(validationErrors).flat().join(' '))
      } else {
        setError(err.response?.data?.message || 'Registration failed. Please try again.')
      }
    } finally {
      setVerifying(false)
    }
  }

  if (success) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="bg-white rounded-2xl shadow-xl p-10 w-full max-w-md text-center">
          <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h2 className="text-2xl font-bold text-gray-800 mb-2">Account Created</h2>
          <p className="text-gray-500 mb-6">Your employee account has been registered successfully.</p>
          <button
            onClick={() => navigate('/employee/login')}
            className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2.5 rounded-lg"
          >
            Go to Login
          </button>
        </div>
      </div>
    )
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 py-8"
      style={{ backgroundImage: `url(${capitecLogo})`, backgroundSize: '75%', backgroundRepeat: 'no-repeat', backgroundPosition: 'center', backgroundColor: '#f3f4f6' }}
    >
      <div className="bg-white rounded-lg shadow-xl p-8 w-full max-w-md">
        <div className="flex items-center justify-center gap-2 mb-2">
          <img src={capitecLogo} alt="Capitec" className="h-8 w-auto" />
          <h1 className="text-3xl font-bold text-gray-800">Capitec</h1>
        </div>
        <p className="text-center text-gray-600 mb-6">Employee Portal Registration</p>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 mb-4 text-sm">
            {error}
          </div>
        )}

        {step === 'verify' ? (
          <form onSubmit={handleVerify} className="space-y-4">
            <div className="bg-blue-50 border border-blue-200 rounded-lg px-4 py-3 text-sm text-blue-800">
              A 6-digit verification code has been sent to <strong>{formData.email}</strong>. Enter it below to create your account. The code expires in 10 minutes.
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Verification Code</label>
              <input
                type="text"
                value={verifyCode}
                onChange={e => setVerifyCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                maxLength={6}
                required
                autoComplete="one-time-code"
                className="w-full border border-gray-300 rounded-lg px-4 py-3 text-center text-2xl tracking-widest focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="000000"
              />
            </div>
            <button
              type="submit"
              disabled={verifying || verifyCode.length !== 6}
              className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-semibold py-2.5 rounded-lg transition-colors text-sm"
            >
              {verifying ? 'Creating account...' : 'Verify & Create Account'}
            </button>
            <button
              type="button"
              onClick={() => { setStep('form'); setError(''); setVerifyCode('') }}
              className="w-full text-sm text-gray-500 hover:text-gray-700 underline"
            >
              Back
            </button>
          </form>
        ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">First Name</label>
              <input
                type="text" required
                value={formData.firstName}
                onChange={e => setFormData({ ...formData, firstName: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
              <input
                type="text" required
                value={formData.lastName}
                onChange={e => setFormData({ ...formData, lastName: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Email address</label>
            <input
              type="email" required
              value={formData.email}
              onChange={e => setFormData({ ...formData, email: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Phone Number</label>
            <div className="flex gap-2">
              <select
                value={selectedCountry.code}
                onChange={e => setSelectedCountry(COUNTRIES.find(c => c.code === e.target.value) ?? COUNTRIES[0])}
                className="w-44 border border-gray-300 rounded-lg px-2 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
              >
                {COUNTRIES.map(c => (
                  <option key={c.code + c.dial} value={c.code}>{c.name} ({c.dial})</option>
                ))}
              </select>
              <input
                type="tel" required
                value={formData.phoneNumber}
                onChange={e => setFormData({ ...formData, phoneNumber: e.target.value })}
                placeholder={selectedCountry.format}
                className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Department</label>
            <select
              value={formData.department}
              onChange={e => setFormData({ ...formData, department: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {DEPARTMENTS.map(d => <option key={d} value={d}>{d}</option>)}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <div className="relative">
              <input
                type={showPassword ? 'text' : 'password'} required
                value={formData.password}
                onChange={e => setFormData({ ...formData, password: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Min 8 characters, 1 uppercase, 1 digit, 1 special char"
              />
              <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                {showPassword ? (
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" /></svg>
                ) : (
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                )}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Confirm Password</label>
            <div className="relative">
              <input
                type={showConfirm ? 'text' : 'password'} required
                value={formData.confirmPassword}
                onChange={e => setFormData({ ...formData, confirmPassword: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="••••••••"
              />
              <button type="button" onClick={() => setShowConfirm(!showConfirm)} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                {showConfirm ? (
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" /></svg>
                ) : (
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                )}
              </button>
            </div>
          </div>

          <button
            type="submit" disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-semibold py-2.5 rounded-lg transition-colors text-sm"
          >
            {loading ? 'Creating account...' : 'Create Account'}
          </button>
        </form>
        )}

        <p className="text-center text-sm text-gray-500 mt-6">
          Already have an account?{' '}
          <Link to="/employee/login" className="text-blue-600 hover:underline font-medium">Sign in</Link>
        </p>
      </div>
    </div>
  )
}

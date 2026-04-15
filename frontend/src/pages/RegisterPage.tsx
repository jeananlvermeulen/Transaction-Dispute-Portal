import React, { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import apiClient from '../services/api'
import capitecLogo from '../assets/symbol.png'
import { LANGUAGES } from '../utils/translations'
import { useLanguage } from '../context/LanguageContext'

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

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    password: '',
    confirmPassword: '',
  })
  const [selectedCountry, setSelectedCountry] = useState(COUNTRIES[0])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [step, setStep] = useState<'form' | 'verify'>('form')
  const [verifyCode, setVerifyCode] = useState('')
  const [verifying, setVerifying] = useState(false)
  const [showSuccess, setShowSuccess] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const navigate = useNavigate()
  const { lang, setLang, t } = useLanguage()

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

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
      setError('Passwords do not match')
      return
    }

    setLoading(true)
    try {
      await apiClient.sendRegistrationCode(formData.email, formData.firstName)
      setStep('verify')
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || 'An error occurred')
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

      const result = await apiClient.register(
        formData.email,
        formData.password,
        formData.confirmPassword,
        formData.firstName,
        formData.lastName,
        fullPhone,
        verifyCode
      )
      if (result.success) {
        setShowSuccess(true)
      } else {
        setError(result.message || 'Registration failed')
      }
    } catch (err: any) {
      const validationErrors = err?.response?.data?.errors
      if (validationErrors) {
        setError(Object.values(validationErrors).flat().join(' '))
      } else {
        setError(err?.response?.data?.message || err?.message || 'An error occurred')
      }
    } finally {
      setVerifying(false)
    }
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4"
      style={{ backgroundImage: `url(${capitecLogo})`, backgroundSize: '75%', backgroundRepeat: 'no-repeat', backgroundPosition: 'center', backgroundColor: '#f3f4f6' }}
    >
      {/* Language selector — top right */}
      <div className="absolute top-4 right-4">
        <select
          value={lang}
          onChange={e => setLang(e.target.value)}
          className="text-sm border border-gray-300 rounded-lg px-3 py-1.5 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm"
        >
          {LANGUAGES.map(l => (
            <option key={l.code} value={l.code}>{l.label}</option>
          ))}
        </select>
      </div>

      {showSuccess && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
          onClick={() => navigate('/login')}
        >
          <div className="bg-white rounded-2xl shadow-2xl p-10 flex flex-col items-center gap-4 max-w-sm mx-4">
            <div className="bg-green-100 rounded-full p-4">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-12 w-12 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="text-2xl font-bold text-gray-800">{t.accountCreated}</h2>
            <p className="text-gray-500 text-center">{t.accountCreatedMsg}</p>
            <p className="text-sm text-gray-400">{t.clickToLogin}</p>
          </div>
        </div>
      )}
      <div className="bg-white rounded-lg shadow-xl p-8 w-full max-w-md">
        <div className="flex items-center justify-center gap-2 mb-2">
          <img src={capitecLogo} alt="Capitec" className="h-8 w-auto" />
          <h1 className="text-3xl font-bold text-gray-800">Capitec</h1>
        </div>
        <p className="text-center text-gray-600 mb-8">{t.joinPortal}</p>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {step === 'verify' ? (
          <form onSubmit={handleVerify} className="space-y-4">
            <div className="bg-blue-50 border border-blue-200 rounded-lg px-4 py-3 text-sm text-blue-800">
              A 6-digit verification code has been sent to <strong>{formData.email}</strong>. Enter it below to create your account. The code expires in 10 minutes.
            </div>
            <div>
              <label className="block text-gray-700 font-semibold mb-2">Verification Code</label>
              <input
                type="text"
                value={verifyCode}
                onChange={e => setVerifyCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                maxLength={6}
                required
                autoComplete="one-time-code"
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-center text-2xl tracking-widest"
                placeholder="000000"
              />
            </div>
            <button
              type="submit"
              disabled={verifying || verifyCode.length !== 6}
              className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-gray-700 font-semibold mb-2">
                {t.firstName}
                <input
                  type="text"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleChange}
                  required
                  autoComplete="given-name"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </label>
            </div>
            <div>
              <label className="block text-gray-700 font-semibold mb-2">
                {t.lastName}
                <input
                  type="text"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleChange}
                  required
                  autoComplete="family-name"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </label>
            </div>
          </div>

          <div>
            <label className="block text-gray-700 font-semibold mb-2">
              {t.email}
              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleChange}
                required
                autoComplete="email"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </label>
          </div>

          <div>
            <label className="block text-gray-700 font-semibold mb-2">
              {t.phoneNumber}
            </label>
            <div className="flex flex-col sm:flex-row gap-2">
              <select
                value={selectedCountry.code}
                onChange={(e) => setSelectedCountry(COUNTRIES.find(c => c.code === e.target.value) ?? COUNTRIES[0])}
                className="w-full sm:w-40 px-2 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white text-sm"
              >
                {COUNTRIES.map((c) => (
                  <option key={c.code + c.dial} value={c.code}>
                    {c.name} ({c.dial})
                  </option>
                ))}
              </select>
              <input
                type="tel"
                name="phoneNumber"
                value={formData.phoneNumber}
                onChange={handleChange}
                required
                autoComplete="tel"
                placeholder={selectedCountry.format}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div>
            <label className="block text-gray-700 font-semibold mb-2">
              {t.password}
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  name="password"
                  value={formData.password}
                  onChange={handleChange}
                  required
                  autoComplete="new-password"
                  className="w-full px-4 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Min 8 characters, 1 uppercase, 1 digit, 1 special char"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((v) => !v)}
                  className="absolute inset-y-0 right-0 flex items-center px-3 text-gray-500 hover:text-gray-700"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? (
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                  ) : (
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  )}
                </button>
              </div>
            </label>
          </div>

          <div>
            <label className="block text-gray-700 font-semibold mb-2">
              {t.confirmPassword}
              <div className="relative">
                <input
                  type={showConfirmPassword ? 'text' : 'password'}
                  name="confirmPassword"
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  required
                  autoComplete="new-password"
                  className="w-full px-4 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmPassword((v) => !v)}
                  className="absolute inset-y-0 right-0 flex items-center px-3 text-gray-500 hover:text-gray-700"
                  aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                >
                  {showConfirmPassword ? (
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
                    </svg>
                  ) : (
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                  )}
                </button>
              </div>
            </label>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
          >
            {loading ? t.creatingAccount : t.createAccount}
          </button>
        </form>
        )}

        <div className="mt-6 text-center">
          <p className="text-gray-600">
            {t.alreadyHaveAccount}{' '}
            <Link to="/login" className="text-blue-500 hover:text-blue-700 font-semibold">
              {t.signIn}
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}

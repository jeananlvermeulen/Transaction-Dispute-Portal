import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../services/api'
import capitecLogo from '../assets/symbol.png'
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

// Parse a stored phone number (e.g. "+27821234567") into country + local number
function parsePhoneNumber(stored: string): { country: typeof COUNTRIES[0]; localNumber: string } {
  if (stored) {
    // Sort by dial length descending so longer codes match first (e.g. +234 before +23)
    const sorted = [...COUNTRIES].sort((a, b) => b.dial.length - a.dial.length)
    for (const c of sorted) {
      if (stored.startsWith(c.dial)) {
        return { country: c, localNumber: stored.slice(c.dial.length).trim() }
      }
    }
  }
  return { country: COUNTRIES[0], localNumber: stored || '' }
}

export default function ProfilePage() {
  const navigate = useNavigate()
  const { t } = useLanguage()

  const [user, setUser] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  // Profile form
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [selectedCountry, setSelectedCountry] = useState(COUNTRIES[0])
  const [localPhone, setLocalPhone] = useState('')
  const [profileSaving, setProfileSaving] = useState(false)
  const [profileSuccess, setProfileSuccess] = useState('')
  const [profileError, setProfileError] = useState('')
  const [profileStep, setProfileStep] = useState<'form' | 'verify'>('form')
  const [profileVerifyCode, setProfileVerifyCode] = useState('')
  const [profileVerifying, setProfileVerifying] = useState(false)

  // Password form
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmNewPassword, setConfirmNewPassword] = useState('')
  const [showCurrentPw, setShowCurrentPw] = useState(false)
  const [showNewPw, setShowNewPw] = useState(false)
  const [showConfirmPw, setShowConfirmPw] = useState(false)
  const [pwSaving, setPwSaving] = useState(false)
  const [pwSuccess, setPwSuccess] = useState('')
  const [pwError, setPwError] = useState('')
  const [pwStep, setPwStep] = useState<'form' | 'verify'>('form')
  const [verifyCode, setVerifyCode] = useState('')
  const [verifying, setVerifying] = useState(false)

  useEffect(() => {
    const load = async () => {
      try {
        const data = await apiClient.getUser()
        setUser(data)
        setFirstName(data.firstName || '')
        setLastName(data.lastName || '')
        const { country, localNumber } = parsePhoneNumber(data.phoneNumber || '')
        setSelectedCountry(country)
        setLocalPhone(localNumber)
      } catch {
        // handled by 401 interceptor
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const buildFullPhone = () => {
    const digits = localPhone.replace(/\D/g, '')
    const dc = selectedCountry.dial.replace(/\D/g, '')
    const localDigits = digits.startsWith(dc) ? digits.slice(dc.length) : digits.replace(/^0/, '')
    return `${selectedCountry.dial}${localDigits}`
  }

  const handleProfileSave = async (e: React.FormEvent) => {
    e.preventDefault()
    setProfileSaving(true)
    setProfileSuccess('')
    setProfileError('')
    try {
      await apiClient.requestProfileChange(firstName, lastName, buildFullPhone())
      setProfileStep('verify')
    } catch (err: any) {
      setProfileError(err?.response?.data?.message || 'Failed to send verification code')
    } finally {
      setProfileSaving(false)
    }
  }

  const handleProfileVerify = async (e: React.FormEvent) => {
    e.preventDefault()
    setProfileVerifying(true)
    setProfileError('')
    try {
      await apiClient.confirmProfileChange(profileVerifyCode)
      setProfileSuccess(t.profileUpdated)
      setProfileStep('form')
      setProfileVerifyCode('')
    } catch (err: any) {
      setProfileError(err?.response?.data?.message || 'Invalid or expired code')
    } finally {
      setProfileVerifying(false)
    }
  }

  const handlePasswordRequest = async (e: React.FormEvent) => {
    e.preventDefault()
    setPwSaving(true)
    setPwSuccess('')
    setPwError('')
    try {
      await apiClient.requestPasswordChange(currentPassword, newPassword, confirmNewPassword)
      setPwStep('verify')
    } catch (err: any) {
      setPwError(err?.response?.data?.message || 'Failed to send verification code')
    } finally {
      setPwSaving(false)
    }
  }

  const handlePasswordVerify = async (e: React.FormEvent) => {
    e.preventDefault()
    setVerifying(true)
    setPwError('')
    try {
      await apiClient.confirmPasswordChange(verifyCode)
      setPwSuccess(t.passwordChanged)
      setPwStep('form')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmNewPassword('')
      setVerifyCode('')
    } catch (err: any) {
      setPwError(err?.response?.data?.message || 'Invalid or expired code')
    } finally {
      setVerifying(false)
    }
  }

  const EyeIcon = ({ show, onToggle }: { show: boolean; onToggle: () => void }) => (
    <button type="button" onClick={onToggle} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
      {show ? (
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
  )

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <button onClick={() => navigate('/dashboard')} className="flex items-center gap-2 hover:opacity-80 transition-opacity">
              <img src={capitecLogo} alt="Capitec Bank" className="h-8 w-auto" />
              <h1 className="text-2xl font-bold text-blue-600">{t.profile}</h1>
            </button>
            <div className="hidden md:flex items-center gap-3">
              <button onClick={() => navigate('/dashboard')} className="flex items-center gap-1 text-gray-600 hover:text-blue-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                </svg>
                {t.dashboard}
              </button>
              <button onClick={async () => { await apiClient.logout(); navigate('/login') }} className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg">{t.logout}</button>
            </div>
            <button
              onClick={() => setMobileMenuOpen(o => !o)}
              className="md:hidden p-2 rounded-lg text-gray-600 hover:bg-gray-100"
              aria-label="Menu"
            >
              {mobileMenuOpen ? (
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              ) : (
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              )}
            </button>
          </div>
          {mobileMenuOpen && (
            <div className="md:hidden border-t border-gray-100 py-3 space-y-1">
              <button onClick={() => { navigate('/dashboard'); setMobileMenuOpen(false) }} className="w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-50 font-semibold rounded-lg flex items-center gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                </svg>
                {t.dashboard}
              </button>
              <div className="px-4 pt-1">
                <button onClick={async () => { await apiClient.logout(); navigate('/login') }} className="w-full bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg font-semibold">{t.logout}</button>
              </div>
            </div>
          )}
        </div>
      </nav>

      <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
        {loading ? (
          <div className="text-center py-8">{t.loading}</div>
        ) : (
          <>
            {/* Profile Info Card */}
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-800 mb-4">{t.editProfile}</h2>

              {profileSuccess && <div className="bg-green-100 text-green-700 px-4 py-2 rounded mb-4 text-sm">{profileSuccess}</div>}
              {profileError && <div className="bg-red-100 text-red-700 px-4 py-2 rounded mb-4 text-sm">{profileError}</div>}

              {profileStep === 'form' ? (
                <form onSubmit={handleProfileSave} className="space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-semibold text-gray-700 mb-1">{t.firstName}</label>
                      <input
                        type="text"
                        value={firstName}
                        onChange={e => setFirstName(e.target.value)}
                        required
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-semibold text-gray-700 mb-1">{t.lastName}</label>
                      <input
                        type="text"
                        value={lastName}
                        onChange={e => setLastName(e.target.value)}
                        required
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">{t.email}</label>
                    <input
                      type="email"
                      value={user?.email || ''}
                      disabled
                      className="w-full px-3 py-2 border border-gray-200 rounded-lg bg-gray-50 text-gray-500 text-sm cursor-not-allowed"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">{t.phoneNumber}</label>
                    <div className="flex flex-col sm:flex-row gap-2">
                      <select
                        value={selectedCountry.code}
                        onChange={e => setSelectedCountry(COUNTRIES.find(c => c.code === e.target.value) ?? COUNTRIES[0])}
                        className="w-full sm:w-44 px-2 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white text-sm"
                      >
                        {COUNTRIES.map(c => (
                          <option key={c.code + c.dial} value={c.code}>
                            {c.name} ({c.dial})
                          </option>
                        ))}
                      </select>
                      <input
                        type="tel"
                        value={localPhone}
                        onChange={e => setLocalPhone(e.target.value)}
                        placeholder={selectedCountry.format}
                        autoComplete="tel"
                        className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      />
                    </div>
                  </div>

                  <button
                    type="submit"
                    disabled={profileSaving}
                    className="w-full bg-blue-500 hover:bg-blue-600 disabled:opacity-50 text-white font-semibold py-2 rounded-lg transition text-sm"
                  >
                    {profileSaving ? 'Sending code...' : t.saveChanges}
                  </button>
                </form>
              ) : (
                <form onSubmit={handleProfileVerify} className="space-y-4">
                  <div className="bg-blue-50 border border-blue-200 rounded-lg px-4 py-3 text-sm text-blue-800">
                    A 6-digit verification code has been sent to your email address. Enter it below to confirm your profile changes. The code expires in 10 minutes.
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">Verification Code</label>
                    <input
                      type="text"
                      value={profileVerifyCode}
                      onChange={e => setProfileVerifyCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                      maxLength={6}
                      required
                      autoComplete="one-time-code"
                      className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-center text-2xl tracking-widest"
                      placeholder="000000"
                    />
                  </div>

                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => { setProfileStep('form'); setProfileError(''); setProfileVerifyCode('') }}
                      className="flex-1 border border-gray-300 text-gray-700 font-semibold py-2 rounded-lg hover:bg-gray-50 transition text-sm"
                    >
                      Back
                    </button>
                    <button
                      type="submit"
                      disabled={profileVerifying || profileVerifyCode.length !== 6}
                      className="flex-1 bg-blue-500 hover:bg-blue-600 disabled:opacity-50 text-white font-semibold py-2 rounded-lg transition text-sm"
                    >
                      {profileVerifying ? 'Verifying...' : 'Confirm Changes'}
                    </button>
                  </div>
                </form>
              )}
            </div>

            {/* Change Password Card */}
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-800 mb-4">{t.changePassword}</h2>

              {pwSuccess && <div className="bg-green-100 text-green-700 px-4 py-2 rounded mb-4 text-sm">{pwSuccess}</div>}
              {pwError && <div className="bg-red-100 text-red-700 px-4 py-2 rounded mb-4 text-sm">{pwError}</div>}

              {pwStep === 'form' ? (
                <form onSubmit={handlePasswordRequest} className="space-y-4">
                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">{t.currentPassword}</label>
                    <div className="relative">
                      <input
                        type={showCurrentPw ? 'text' : 'password'}
                        value={currentPassword}
                        onChange={e => setCurrentPassword(e.target.value)}
                        required
                        autoComplete="current-password"
                        className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      />
                      <EyeIcon show={showCurrentPw} onToggle={() => setShowCurrentPw(v => !v)} />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">{t.newPassword}</label>
                    <div className="relative">
                      <input
                        type={showNewPw ? 'text' : 'password'}
                        value={newPassword}
                        onChange={e => setNewPassword(e.target.value)}
                        required
                        autoComplete="new-password"
                        className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      />
                      <EyeIcon show={showNewPw} onToggle={() => setShowNewPw(v => !v)} />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">{t.confirmNewPassword}</label>
                    <div className="relative">
                      <input
                        type={showConfirmPw ? 'text' : 'password'}
                        value={confirmNewPassword}
                        onChange={e => setConfirmNewPassword(e.target.value)}
                        required
                        autoComplete="new-password"
                        className="w-full px-3 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                      />
                      <EyeIcon show={showConfirmPw} onToggle={() => setShowConfirmPw(v => !v)} />
                    </div>
                  </div>

                  <button
                    type="submit"
                    disabled={pwSaving}
                    className="w-full bg-blue-500 hover:bg-blue-600 disabled:opacity-50 text-white font-semibold py-2 rounded-lg transition text-sm"
                  >
                    {pwSaving ? t.saving : 'Send Verification Code'}
                  </button>
                </form>
              ) : (
                <form onSubmit={handlePasswordVerify} className="space-y-4">
                  <div className="bg-blue-50 border border-blue-200 rounded-lg px-4 py-3 text-sm text-blue-800">
                    A 6-digit verification code has been sent to your email address. Enter it below to confirm the password change. The code expires in 10 minutes.
                  </div>

                  <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">Verification Code</label>
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

                  <div className="flex gap-3">
                    <button
                      type="button"
                      onClick={() => { setPwStep('form'); setPwError(''); setVerifyCode('') }}
                      className="flex-1 border border-gray-300 text-gray-700 font-semibold py-2 rounded-lg hover:bg-gray-50 transition text-sm"
                    >
                      Back
                    </button>
                    <button
                      type="submit"
                      disabled={verifying || verifyCode.length !== 6}
                      className="flex-1 bg-blue-500 hover:bg-blue-600 disabled:opacity-50 text-white font-semibold py-2 rounded-lg transition text-sm"
                    >
                      {verifying ? 'Verifying...' : 'Confirm Password Change'}
                    </button>
                  </div>
                </form>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}

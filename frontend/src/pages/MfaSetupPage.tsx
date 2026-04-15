import React, { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../services/api'
import capitecLogo from '../assets/symbol.png'
import { useLanguage } from '../context/LanguageContext'

export default function MfaSetupPage() {
  const [step, setStep] = useState<'start' | 'setup' | 'verify'>('start')
  const [qrCode, setQrCode] = useState<string>('')
  const [mfaCode, setMfaCode] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const navigate = useNavigate()
  const { t } = useLanguage()

  const handleGenerateMfa = async () => {
    setLoading(true)
    setError('')
    try {
      const result = await apiClient.generateMfa()
      setQrCode(result.qrCode)
      setStep('setup')
    } catch (err) {
      setError('Failed to generate MFA QR code')
    } finally {
      setLoading(false)
    }
  }

  const handleEnableMfa = async () => {
    if (mfaCode.length !== 6) {
      setError('Please enter a 6-digit code')
      return
    }

    setLoading(true)
    setError('')
    try {
      const result = await apiClient.enableMfa(mfaCode)
      if (result.success) {
        setSuccess(true)
        setStep('verify')
        setTimeout(() => navigate('/dashboard'), 2000)
      } else {
        setError(result.message || 'Failed to enable MFA')
      }
    } catch (err) {
      setError('Failed to verify MFA code')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <button onClick={() => navigate('/dashboard')} className="flex items-center gap-2 hover:opacity-80 transition-opacity">
              <img src={capitecLogo} alt="Capitec" className="h-8 w-auto" />
              <h1 className="text-2xl font-bold text-blue-600">{t.securitySettings}</h1>
            </button>
            <div className="flex items-center gap-3">
              <button
                onClick={() => navigate('/dashboard')}
                className="flex items-center gap-1 text-gray-600 hover:text-blue-600"
              >
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                </svg>
                {t.dashboard}
              </button>
              <button onClick={async () => { await apiClient.logout(); navigate('/login') }} className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg">{t.logout}</button>
            </div>
          </div>
        </div>
      </nav>

      <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="bg-white rounded-lg shadow p-8">
          <h2 className="text-2xl font-bold text-gray-800 mb-4">
            Two-Factor Authentication
          </h2>

          {step === 'start' && (
            <div>
              <p className="text-gray-600 mb-6">
                Enable two-factor authentication (2FA) to add an extra layer of security to your account.
                You'll need an authenticator app like Google Authenticator, Microsoft Authenticator, or Authy.
              </p>

              <button
                onClick={handleGenerateMfa}
                disabled={loading}
                className="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-6 rounded-lg transition duration-200 disabled:opacity-50"
              >
                {loading ? 'Generating...' : 'Get Started'}
              </button>
            </div>
          )}

          {step === 'setup' && (
            <div>
              <p className="text-gray-600 mb-6">
                1. Open your authenticator app
                <br />
                2. Scan this QR code or enter the code manually
                <br />
                3. Enter the 6-digit code from your app
              </p>

              {qrCode && (
                <div className="bg-gray-100 p-6 rounded-lg text-center mb-6">
                  <p className="text-sm text-gray-600 mb-4">Scan with your authenticator app:</p>
                  <div className="inline-block bg-white p-4 rounded">
                    <img src={`https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(qrCode)}`} alt="QR Code" />
                  </div>
                </div>
              )}

              <div className="mb-6">
                <label className="block text-gray-700 font-semibold mb-2">
                  Enter 6-digit code
                </label>
                <input
                  type="text"
                  value={mfaCode}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => setMfaCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  maxLength={6}
                  className="w-full px-4 py-3 border border-gray-300 rounded-lg text-center text-2xl tracking-widest focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="000000"
                />
              </div>

              {error && <div className="bg-red-100 text-red-700 p-4 rounded mb-4">{error}</div>}

              <div className="flex gap-4">
                <button
                  onClick={handleEnableMfa}
                  disabled={loading || mfaCode.length !== 6}
                  className="flex-1 bg-green-500 hover:bg-green-600 text-white font-bold py-2 px-6 rounded-lg transition duration-200 disabled:opacity-50"
                >
                  {loading ? 'Verifying...' : 'Enable 2FA'}
                </button>
                <button
                  onClick={() => {
                    setStep('start')
                    setMfaCode('')
                    setError('')
                  }}
                  className="flex-1 bg-gray-500 hover:bg-gray-600 text-white font-bold py-2 px-6 rounded-lg transition duration-200"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          {step === 'verify' && success && (
            <div className="text-center">
              <div className="text-6xl mb-4">✓</div>
              <h3 className="text-2xl font-bold text-green-600 mb-2">
                Two-Factor Authentication Enabled!
              </h3>
              <p className="text-gray-600">
                Your account is now more secure. You'll need your authenticator app to sign in next time.
              </p>
              <p className="text-gray-500 text-sm mt-4">
                Redirecting to dashboard...
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

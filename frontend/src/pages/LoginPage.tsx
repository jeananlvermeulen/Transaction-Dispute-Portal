import React, { useState, useEffect } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import apiClient from '../services/api'
import capitecBankLogo from '../assets/symbol.png'
import { LANGUAGES } from '../utils/translations'
import { useLanguage } from '../context/LanguageContext'


export default function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [mfaCode, setMfaCode] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [requiresMfa, setRequiresMfa] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  // Forgot password
  const [forgotStep, setForgotStep] = useState<'off' | 'email' | 'code' | 'newpw' | 'done'>('off')
  const [forgotEmail, setForgotEmail] = useState('')
  const [forgotCode, setForgotCode] = useState('')
  const [forgotNewPw, setForgotNewPw] = useState('')
  const [forgotConfirmPw, setForgotConfirmPw] = useState('')
  const [forgotLoading, setForgotLoading] = useState(false)
  const [forgotError, setForgotError] = useState('')
  const [showForgotPw, setShowForgotPw] = useState(false)
  const [showForgotConfirm, setShowForgotConfirm] = useState(false)
  const navigate = useNavigate()
  const { lang, setLang, t } = useLanguage()

  useEffect(() => {
    const msg = sessionStorage.getItem('auth_msg')
    if (msg) {
      setError(msg)
      sessionStorage.removeItem('auth_msg')
    }
  }, [])

  useEffect(() => {
    if (!error) return
    const clear = () => setError('')
    document.addEventListener('click', clear)
    return () => document.removeEventListener('click', clear)
  }, [error])

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      const result = await apiClient.login(email, password)
      if (result.requiresMfa) {
        setRequiresMfa(true)
      } else if (result.success) {
        navigate('/dashboard')
      } else {
        setError(result.message || 'Login failed')
      }
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Invalid email or password.')
    } finally {
      setLoading(false)
    }
  }

  const handleMfaVerify = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      const result = await apiClient.verifyMfaAndLogin(email, mfaCode)
      if (result.success) {
        navigate('/dashboard')
      } else {
        setError(result.message || 'MFA verification failed')
      }
    } catch (err: any) {
      setError(err?.response?.data?.message || 'MFA verification failed.')
    } finally {
      setLoading(false)
    }
  }

  const handleForgotEmail = async (e: React.FormEvent) => {
    e.preventDefault()
    setForgotLoading(true)
    setForgotError('')
    try {
      await apiClient.forgotPassword(forgotEmail)
      setForgotStep('code')
    } catch {
      setForgotError('Failed to send code. Please try again.')
    } finally {
      setForgotLoading(false)
    }
  }

  const handleForgotCode = (e: React.FormEvent) => {
    e.preventDefault()
    setForgotStep('newpw')
  }

  const handleForgotReset = async (e: React.FormEvent) => {
    e.preventDefault()
    setForgotLoading(true)
    setForgotError('')
    try {
      await apiClient.resetPassword(forgotEmail, forgotCode, forgotNewPw, forgotConfirmPw)
      setForgotStep('done')
    } catch (err: any) {
      setForgotError(err?.response?.data?.message || 'Failed to reset password')
    } finally {
      setForgotLoading(false)
    }
  }

  const resetForgot = () => {
    setForgotStep('off')
    setForgotEmail('')
    setForgotCode('')
    setForgotNewPw('')
    setForgotConfirmPw('')
    setForgotError('')
  }

  const EyeIcon = ({ show, onToggle }: { show: boolean; onToggle: () => void }) => (
    <button type="button" onClick={onToggle} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
      {show ? (
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" /></svg>
      ) : (
        <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
      )}
    </button>
  )

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4"
      style={{ backgroundImage: `url(${capitecBankLogo})`, backgroundSize: '75%', backgroundRepeat: 'no-repeat', backgroundPosition: 'center', backgroundColor: '#f3f4f6' }}
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

      <div className="bg-white rounded-lg shadow-xl p-8 w-full max-w-md">
        <div className="flex items-center justify-center gap-2 mb-2">
          <img src={capitecBankLogo} alt="Capitec" className="h-8 w-auto" />
          <h1 className="text-3xl font-bold text-gray-800">Capitec</h1>
        </div>
        <p className="text-center text-gray-600 mb-8">{t.portalSubtitle}</p>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            <p>{error}</p>
            <p className="mt-1 text-sm">
              Forgot your password?{' '}
              <button
                type="button"
                onClick={() => setForgotStep('email')}
                className="underline font-semibold hover:text-red-900"
              >
                Click here to reset it.
              </button>
            </p>
          </div>
        )}

        {forgotStep !== 'off' ? (
          <div>
            {forgotStep === 'email' && (
              <form onSubmit={handleForgotEmail} className="space-y-4">
                <p className="text-gray-600 text-sm mb-2">Enter your email address and we'll send you a verification code.</p>
                {forgotError && <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">{forgotError}</div>}
                <div>
                  <label className="block text-gray-700 font-semibold mb-2">
                    Email
                    <input
                      type="email"
                      value={forgotEmail}
                      onChange={e => setForgotEmail(e.target.value)}
                      required
                      autoComplete="email"
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 mt-1"
                      placeholder="your@email.com"
                    />
                  </label>
                </div>
                <button
                  type="submit"
                  disabled={forgotLoading}
                  className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
                >
                  {forgotLoading ? 'Sending...' : 'Send Code'}
                </button>
                <button type="button" onClick={resetForgot} className="w-full text-gray-600 hover:text-gray-800 font-semibold py-2">
                  Back to Login
                </button>
              </form>
            )}
            {forgotStep === 'code' && (
              <form onSubmit={handleForgotCode} className="space-y-4">
                <div className="bg-blue-100 border border-blue-400 text-blue-700 px-4 py-3 rounded text-sm">
                  A 6-digit code has been sent to <strong>{forgotEmail}</strong>. It expires in 10 minutes.
                </div>
                {forgotError && <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">{forgotError}</div>}
                <div>
                  <label className="block text-gray-700 font-semibold mb-2">
                    Verification Code
                    <input
                      type="text"
                      value={forgotCode}
                      onChange={e => setForgotCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                      maxLength={6}
                      required
                      autoComplete="one-time-code"
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-center text-2xl tracking-widest mt-1"
                      placeholder="000000"
                    />
                  </label>
                </div>
                <button
                  type="submit"
                  disabled={forgotCode.length !== 6}
                  className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
                >
                  Verify Code
                </button>
                <button type="button" onClick={resetForgot} className="w-full text-gray-600 hover:text-gray-800 font-semibold py-2">
                  Back to Login
                </button>
              </form>
            )}
            {forgotStep === 'newpw' && (
              <form onSubmit={handleForgotReset} className="space-y-4">
                <p className="text-gray-600 text-sm">Enter your new password below.</p>
                {forgotError && <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">{forgotError}</div>}
                <div>
                  <label className="block text-gray-700 font-semibold mb-2">New Password</label>
                  <div className="relative">
                    <input
                      type={showForgotPw ? 'text' : 'password'}
                      value={forgotNewPw}
                      onChange={e => setForgotNewPw(e.target.value)}
                      required
                      className="w-full px-4 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="••••••••"
                    />
                    <EyeIcon show={showForgotPw} onToggle={() => setShowForgotPw(!showForgotPw)} />
                  </div>
                </div>
                <div>
                  <label className="block text-gray-700 font-semibold mb-2">Confirm New Password</label>
                  <div className="relative">
                    <input
                      type={showForgotConfirm ? 'text' : 'password'}
                      value={forgotConfirmPw}
                      onChange={e => setForgotConfirmPw(e.target.value)}
                      required
                      className="w-full px-4 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="••••••••"
                    />
                    <EyeIcon show={showForgotConfirm} onToggle={() => setShowForgotConfirm(!showForgotConfirm)} />
                  </div>
                </div>
                <button
                  type="submit"
                  disabled={forgotLoading}
                  className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
                >
                  {forgotLoading ? 'Resetting...' : 'Reset Password'}
                </button>
                <button type="button" onClick={resetForgot} className="w-full text-gray-600 hover:text-gray-800 font-semibold py-2">
                  Cancel
                </button>
              </form>
            )}
            {forgotStep === 'done' && (
              <div className="text-center space-y-4">
                <div className="bg-green-100 border border-green-400 text-green-700 px-4 py-3 rounded">
                  Your password has been updated successfully.
                </div>
                <button
                  type="button"
                  onClick={resetForgot}
                  className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200"
                >
                  Back to Login
                </button>
              </div>
            )}
          </div>
        ) : !requiresMfa ? (
          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label className="block text-gray-700 font-semibold mb-2">
                {t.email}
                <input
                  type="email"
                  value={email}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="your@email.com"
                />
              </label>
            </div>

            <div>
              <label className="block text-gray-700 font-semibold mb-2">{t.password}</label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
                  className="w-full px-4 py-2 pr-10 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="••••••••"
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

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
            >
              {loading ? t.signingIn : t.signIn}
            </button>
            <div className="text-center mt-2">
              <button
                type="button"
                onClick={() => setForgotStep('email')}
                className="text-blue-500 hover:text-blue-700 text-sm font-semibold"
              >
                Forgot password?
              </button>
            </div>
          </form>
        ) : (
          <form onSubmit={handleMfaVerify} className="space-y-4">
            <div className="bg-blue-100 border border-blue-400 text-blue-700 px-4 py-3 rounded">
              Multi-Factor Authentication enabled. Please enter your 6-digit code from your authenticator app.
            </div>
            <div>
              <label className="block text-gray-700 font-semibold mb-2">
                Authenticator Code
                <input
                  type="text"
                  value={mfaCode}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => setMfaCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  maxLength={6}
                  required
                  autoComplete="one-time-code"
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-center text-2xl tracking-widest"
                  placeholder="000000"
                />
              </label>
            </div>
            <button
              type="submit"
              disabled={loading || mfaCode.length !== 6}
              className="w-full bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg transition duration-200 disabled:opacity-50"
            >
              {loading ? 'Verifying...' : 'Verify'}
            </button>
            <button
              type="button"
              onClick={() => { setRequiresMfa(false); setMfaCode(''); setError('') }}
              className="w-full text-gray-600 hover:text-gray-800 font-semibold py-2"
            >
              Back to Login
            </button>
          </form>
        )}

        <div className="mt-6 text-center">
          <p className="text-gray-600">
            {t.noAccount}{' '}
            <Link to="/register" className="text-blue-500 hover:text-blue-700 font-semibold">
              {t.signUp}
            </Link>
          </p>
          <p className="text-gray-400 text-sm mt-3">
            Are you a Capitec employee?{' '}
            <Link to="/employee/login" className="text-blue-400 hover:text-blue-600 font-semibold">
              Employee Portal
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}

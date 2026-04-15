import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../services/api'
import capitecLogo from '../assets/symbol.png'
import { useLanguage } from '../context/LanguageContext'
import { LANGUAGES } from '../utils/translations'

interface Transaction {
  id: string
  amount: number
  currency: string
  description: string
  date: string
  status: string
}

interface Dispute {
  id: string
  reason: string
  status: string
  incidentReference: string
  createdAt: string
  updatedAt?: string
  resolvedAt?: string
}

const OPEN_STATUSES = ['Submitted', 'Pending', 'UnderReview']

const STATUS_COLORS: Record<string, string> = {
  Submitted: 'bg-gray-100 text-gray-700',
  Pending: 'bg-yellow-100 text-yellow-800',
  UnderReview: 'bg-blue-100 text-blue-800',
  Resolved: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-500',
}

function formatAmount(amount: number, currency: string) {
  return `${currency === 'ZAR' ? 'R' : currency} ${amount.toFixed(2)}`
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-ZA', { day: '2-digit', month: 'short', year: 'numeric' })
}

export default function DashboardPage() {
  const [user, setUser] = useState<any>(null)
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [disputes, setDisputes] = useState<Dispute[]>([])
  const [loading, setLoading] = useState(true)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const navigate = useNavigate()
  const { lang, setLang, t } = useLanguage()

  useEffect(() => {
    const loadAll = async () => {
      try {
        const [userData, txData, disputeData] = await Promise.all([
          apiClient.getUser(),
          apiClient.getTransactions(1, 5),
          apiClient.getDisputes(1, 100),
        ])
        setUser(userData)
        setTransactions(txData?.data?.transactions ?? txData?.transactions ?? [])
        setDisputes(disputeData?.data?.disputes ?? disputeData?.disputes ?? [])
      } catch (err) {
        console.error('Failed to load dashboard', err)
      } finally {
        setLoading(false)
      }
    }
    loadAll()
  }, [])

  const handleLogout = async () => {
    await apiClient.logout()
    navigate('/login')
  }

  const lastViewedHistory = parseInt(localStorage.getItem('disputes_history_last_viewed') ?? '0', 10)
  const HISTORICAL_STATUSES = ['Resolved', 'Rejected', 'Cancelled']
  const updatedDisputeCount = disputes.filter(d => {
    if (!HISTORICAL_STATUSES.includes(d.status)) return false
    const closedAt = d.resolvedAt ? new Date(d.resolvedAt + 'Z').getTime() : 0
    return closedAt > lastViewedHistory
  }).length

  const openDisputes = disputes.filter(d => OPEN_STATUSES.includes(d.status))
  const disputeStats = {
    open: openDisputes.length,
    resolved: disputes.filter(d => d.status === 'Resolved').length,
    rejected: disputes.filter(d => d.status === 'Rejected').length,
    cancelled: disputes.filter(d => d.status === 'Cancelled').length,
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <button onClick={() => navigate('/dashboard')} className="flex items-center gap-2 hover:opacity-80 transition-opacity">
              <img src={capitecLogo} alt="Capitec Bank" className="h-8 w-auto" />
              <h1 className="text-2xl font-bold text-blue-600">Capitec Portal</h1>
            </button>

            {/* Desktop nav */}
            <div className="hidden md:flex gap-4 items-center">
              <button onClick={() => navigate('/transactions')} className="text-gray-600 hover:text-blue-600 font-semibold">{t.transactions}</button>
              <button onClick={() => { localStorage.setItem('disputes_last_viewed', Date.now().toString()); navigate('/disputes') }} className="relative text-gray-600 hover:text-blue-600 font-semibold">
                {t.disputes}
                {updatedDisputeCount > 0 && (
                  <span className="absolute -top-1.5 -right-2.5 h-4 min-w-4 px-1 bg-red-500 text-white text-xs rounded-full flex items-center justify-center leading-none">
                    {updatedDisputeCount}
                  </span>
                )}
              </button>
              <button onClick={() => navigate('/profile')} className="flex items-center gap-1 text-gray-600 hover:text-blue-600 font-semibold">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
                {t.profile}
              </button>
              <select
                value={lang}
                onChange={e => setLang(e.target.value)}
                className="text-sm border border-gray-300 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {LANGUAGES.map(l => <option key={l.code} value={l.code}>{l.label}</option>)}
              </select>
              <button onClick={handleLogout} className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg">{t.logout}</button>
            </div>

            {/* Mobile hamburger */}
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

          {/* Mobile dropdown menu */}
          {mobileMenuOpen && (
            <div className="md:hidden border-t border-gray-100 py-3 space-y-1">
              <button onClick={() => { navigate('/transactions'); setMobileMenuOpen(false) }} className="w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-50 font-semibold rounded-lg">
                {t.transactions}
              </button>
              <button
                onClick={() => { localStorage.setItem('disputes_last_viewed', Date.now().toString()); navigate('/disputes'); setMobileMenuOpen(false) }}
                className="w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-50 font-semibold rounded-lg flex items-center gap-2"
              >
                {t.disputes}
                {updatedDisputeCount > 0 && (
                  <span className="h-5 min-w-5 px-1 bg-red-500 text-white text-xs rounded-full flex items-center justify-center leading-none">
                    {updatedDisputeCount}
                  </span>
                )}
              </button>
              <button onClick={() => { navigate('/profile'); setMobileMenuOpen(false) }} className="w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-50 font-semibold rounded-lg">
                {t.profile}
              </button>
              <div className="px-4 py-2">
                <select
                  value={lang}
                  onChange={e => setLang(e.target.value)}
                  className="w-full text-sm border border-gray-300 rounded-lg px-2 py-1.5 bg-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  {LANGUAGES.map(l => <option key={l.code} value={l.code}>{l.label}</option>)}
                </select>
              </div>
              <div className="px-4 pt-1">
                <button onClick={handleLogout} className="w-full bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg font-semibold">{t.logout}</button>
              </div>
            </div>
          )}
        </div>
      </nav>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {loading ? (
          <div className="text-center py-8">{t.loading}</div>
        ) : user ? (
          <div className="space-y-6">

            {/* Row 1: User info cards */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-gray-600 text-sm font-semibold">{t.welcome}</h2>
                <p className="text-2xl font-bold text-gray-800">{user.firstName} {user.lastName}</p>
              </div>
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-gray-600 text-sm font-semibold">{t.email}</h2>
                <p className="text-lg text-gray-800 truncate">{user.email}</p>
              </div>
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-gray-600 text-sm font-semibold">{t.accountNumber}</h2>
                <p className="text-lg text-gray-800">{user.accountNumber || 'N/A'}</p>
              </div>
            </div>

            {/* Row 2: Dispute stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="bg-white rounded-lg shadow p-5 border-l-4 border-blue-400">
                <p className="text-sm font-semibold text-blue-600">Open Disputes</p>
                <p className="text-3xl font-bold text-blue-700 mt-1">{disputeStats.open}</p>
                <p className="text-xs text-gray-400 mt-1">Submitted / Under Review</p>
              </div>
              <div className="bg-white rounded-lg shadow p-5 border-l-4 border-green-400">
                <p className="text-sm font-semibold text-green-600">Resolved</p>
                <p className="text-3xl font-bold text-green-700 mt-1">{disputeStats.resolved}</p>
                <p className="text-xs text-gray-400 mt-1">Outcome in your favour</p>
              </div>
              <div className="bg-white rounded-lg shadow p-5 border-l-4 border-red-400">
                <p className="text-sm font-semibold text-red-600">Rejected</p>
                <p className="text-3xl font-bold text-red-700 mt-1">{disputeStats.rejected}</p>
                <p className="text-xs text-gray-400 mt-1">Dispute not upheld</p>
              </div>
              <div className="bg-white rounded-lg shadow p-5 border-l-4 border-gray-300">
                <p className="text-sm font-semibold text-gray-500">Cancelled</p>
                <p className="text-3xl font-bold text-gray-600 mt-1">{disputeStats.cancelled}</p>
                <p className="text-xs text-gray-400 mt-1">Withdrawn by you</p>
              </div>
            </div>

            {/* Row 3: Recent transactions + Open disputes */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">

              {/* Recent transactions — spans 2 cols */}
              <div className="md:col-span-2 bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-base font-semibold text-gray-800">Recent Transactions</h2>
                  <button
                    onClick={() => navigate('/transactions')}
                    className="text-sm text-blue-500 hover:text-blue-700 font-semibold"
                  >
                    View all
                  </button>
                </div>
                {transactions.length === 0 ? (
                  <p className="text-sm text-gray-400 text-center py-6">No transactions yet.</p>
                ) : (
                  <ul className="divide-y divide-gray-100">
                    {transactions.map(tx => (
                      <li key={tx.id} className="flex items-center justify-between py-3">
                        <div className="min-w-0">
                          <p className="text-sm font-medium text-gray-800 truncate">{tx.description}</p>
                          <p className="text-xs text-gray-400">{formatDate(tx.date)}</p>
                        </div>
                        <span className="ml-4 text-sm font-bold text-gray-800 whitespace-nowrap">
                          {formatAmount(tx.amount, tx.currency)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              {/* Open disputes — spans 1 col */}
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between mb-4">
                  <h2 className="text-base font-semibold text-gray-800">Open Disputes</h2>
                  <button
                    onClick={() => navigate('/disputes')}
                    className="text-sm text-blue-500 hover:text-blue-700 font-semibold"
                  >
                    View all
                  </button>
                </div>
                {openDisputes.length === 0 ? (
                  <p className="text-sm text-gray-400 text-center py-6">No open disputes.</p>
                ) : (
                  <ul className="space-y-3">
                    {openDisputes.slice(0, 3).map(d => (
                      <li key={d.id} className="border border-gray-100 rounded-lg p-3">
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <p className="text-xs font-mono text-gray-500">{d.incidentReference}</p>
                            <p className="text-sm font-medium text-gray-800 mt-0.5">{d.reason}</p>
                          </div>
                          <span className={`text-xs font-semibold px-2 py-0.5 rounded-full whitespace-nowrap ${STATUS_COLORS[d.status] ?? 'bg-gray-100 text-gray-700'}`}>
                            {d.status}
                          </span>
                        </div>
                        <p className="text-xs text-gray-400 mt-1">{formatDate(d.createdAt)}</p>
                      </li>
                    ))}
                    {openDisputes.length > 3 && (
                      <p className="text-xs text-center text-gray-400">+{openDisputes.length - 3} more</p>
                    )}
                  </ul>
                )}
              </div>
            </div>

            {/* Row 4: Quick actions */}
            <div className="bg-blue-50 rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-800 mb-4">{t.quickActions}</h2>
              <div className="grid grid-cols-2 gap-4 max-w-sm">
                <button onClick={() => navigate('/transactions')} className="bg-blue-500 hover:bg-blue-600 text-white font-semibold py-3 px-4 rounded-lg transition">{t.viewTransactions}</button>
                <button onClick={() => navigate('/disputes')} className="bg-green-500 hover:bg-green-600 text-white font-semibold py-3 px-4 rounded-lg transition">{t.viewDisputes}</button>
              </div>
            </div>

          </div>
        ) : (
          <div className="text-center text-red-600">{t.failedToLoad}</div>
        )}
      </div>
    </div>
  )
}

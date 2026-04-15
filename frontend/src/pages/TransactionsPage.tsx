import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../services/api'
import capitecLogo from '../assets/symbol.png'
import { useLanguage } from '../context/LanguageContext'

export default function TransactionsPage() {
  const [transactions, setTransactions] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [simulating, setSimulating] = useState(false)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const navigate = useNavigate()
  const { t } = useLanguage()

  const handleLogout = async () => {
    await apiClient.logout()
    navigate('/login')
  }

  const SAMPLE_TRANSACTIONS = [
    { amount: -299.99, description: 'Woolworths Food' },
    { amount: -850.00, description: 'Electricity - City Power' },
    { amount: 1200.00, description: 'Salary - Capitec Bank' },
    { amount: -45.50, description: 'Uber Ride' },
    { amount: -320.00, description: 'Checkers Supermarket' },
    { amount: -999.00, description: 'Takealot Order' },
    { amount: 500.00, description: 'Refund - Takealot' },
    { amount: -75.00, description: 'Netflix Subscription' },
    { amount: -450.00, description: 'Clicks Pharmacy' },
    { amount: -180.00, description: 'Steers Fast Food' },
    { amount: -3200.00, description: 'Flight - Cape Town' },
    { amount: -650.00, description: 'Edgars Clothing' },
    { amount: -90.00, description: 'Spotify + Apple Music' },
    { amount: -1100.00, description: 'Pick n Pay Monthly Shop' },
    { amount: -500.00, description: 'Gym Membership' },
    { amount: -230.00, description: 'Engen Fuel' },
    { amount: 4500.00, description: 'Payment Received - Insurance' },
    { amount: -120.00, description: 'KFC Family Meal' },
    { amount: -899.00, description: 'Makro Electronics' },
    { amount: -75.00, description: 'DStv Subscription' },
  ]

  useEffect(() => { loadTransactions() }, [])

  const loadTransactions = async () => {
    try {
      const data = await apiClient.getTransactions()
      setTransactions(data.data?.transactions || [])
    } catch (err) {
      setError('Failed to load transactions')
    } finally {
      setLoading(false)
    }
  }

  const handleSimulate = async () => {
    setSimulating(true)
    setError('')
    try {
      await Promise.all(SAMPLE_TRANSACTIONS.map(tx => apiClient.createSimulatedTransaction(tx.amount, tx.description)))
      await loadTransactions()
    } catch (err) {
      setError('Failed to simulate transactions')
    } finally {
      setSimulating(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <button onClick={() => navigate('/dashboard')} className="flex items-center gap-2 hover:opacity-80 transition-opacity">
              <img src={capitecLogo} alt="Capitec Bank" className="h-8 w-auto" />
              <h1 className="text-2xl font-bold text-blue-600">{t.transactions}</h1>
            </button>
            <div className="hidden md:flex items-center gap-3">
              <button onClick={() => navigate('/dashboard')} className="flex items-center gap-1 text-gray-600 hover:text-blue-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                </svg>
                {t.dashboard}
              </button>
              <button onClick={handleLogout} className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg">{t.logout}</button>
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
                <button onClick={handleLogout} className="w-full bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg font-semibold">{t.logout}</button>
              </div>
            </div>
          )}
        </div>
      </nav>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="bg-white rounded-lg shadow p-6 mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold">{t.simulateTransactions}</h2>
            <p className="text-sm text-gray-500 mt-1">{t.simulateDesc}</p>
          </div>
          <button onClick={handleSimulate} disabled={simulating} className="bg-blue-500 hover:bg-blue-600 disabled:opacity-50 text-white px-6 py-2 rounded-lg font-semibold shrink-0">
            {simulating ? t.simulating : t.simulate}
          </button>
        </div>

        {error && <div className="bg-red-100 text-red-700 p-4 rounded mb-4">{error}</div>}

        <div className="bg-white rounded-lg shadow overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-100">
                <tr>
                  <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.date}</th>
                  <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.amount}</th>
                  <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.description}</th>
                  <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.status}</th>
                  <th className="px-6 py-3"></th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={5} className="px-6 py-4 text-center">{t.loading}</td></tr>
                ) : transactions.length === 0 ? (
                  <tr><td colSpan={5} className="px-6 py-4 text-center text-gray-600">{t.noTransactions}</td></tr>
                ) : (
                  transactions.map((tx) => (
                    <tr key={tx.id} className="border-t hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm">{new Date(tx.date).toLocaleDateString('en-GB').replace(/\//g, '-')}</td>
                      <td className={`px-6 py-4 font-semibold ${tx.amount < 0 ? 'text-red-600' : 'text-green-600'}`}>
                        {tx.amount < 0 ? `-R ${Math.abs(tx.amount).toFixed(2)}` : `+R ${tx.amount.toFixed(2)}`}
                      </td>
                      <td className="px-6 py-4 text-sm">{tx.description}</td>
                      <td className="px-6 py-4">
                        <span className="bg-green-100 text-green-800 px-3 py-1 rounded-full text-xs font-semibold">{tx.status}</span>
                      </td>
                      <td className="px-6 py-4 text-right">
                        <button
                          onClick={() => navigate('/disputes', { state: { transactionId: tx.id, description: tx.description, amount: tx.amount } })}
                          className="px-3 py-1.5 text-xs font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition"
                        >
                          Dispute
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>

    </div>
  )
}

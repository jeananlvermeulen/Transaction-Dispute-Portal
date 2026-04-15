import { useState, useEffect, useRef } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import apiClient from '../services/api'
import capitecLogo from '../assets/symbol.png'
import { useLanguage } from '../context/LanguageContext'

interface DisputeModalProps {
  transactionId: string
  description: string
  amount: number
  onClose: () => void
  onSuccess: () => void
}

function DisputeModal({ transactionId, description, amount, onClose, onSuccess }: DisputeModalProps) {
  const [step, setStep] = useState<'reason' | 'summary' | 'success'>('reason')
  const [selectedReason, setSelectedReason] = useState('')
  const [summary, setSummary] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')
  const { t } = useLanguage()

  const REASONS = [
    { value: 'Unauthorised', label: t.reasonUnauthorised, description: 'I did not authorise this transaction' },
    { value: 'IncorrectAmount', label: t.reasonIncorrectAmount, description: 'The amount charged is wrong' },
    { value: 'DoublePayment', label: t.reasonDoublePayment, description: 'I was charged twice for the same transaction' },
    { value: 'Other', label: t.reasonOther, description: 'Another reason not listed above' },
  ]

  const handleReasonNext = () => {
    if (!selectedReason) return
    setStep('summary')
  }

  const handleSubmit = async () => {
    setError('')
    setSubmitting(true)
    try {
      await apiClient.createDispute(
        transactionId,
        selectedReason,
        undefined,
        summary
      )
      setStep('success')
      onSuccess()
    } catch (err: any) {
      const validationErrors = err?.response?.data?.errors
      if (validationErrors) {
        setError(Object.values(validationErrors).flat().join(' '))
      } else {
        setError(err?.response?.data?.message || 'Failed to submit dispute')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg">

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-lg font-bold text-gray-800">{t.disputeStep1Title}</h2>
          {step !== 'success' && (
            <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
        </div>

        {/* Transaction info */}
        {step !== 'success' && (
          <div className="mx-6 mt-4 bg-gray-50 rounded-lg px-4 py-3 flex justify-between items-center text-sm">
            <span className="text-gray-600 font-medium">{description}</span>
            <span className="font-bold text-gray-800">R {amount}</span>
          </div>
        )}

        {/* Step: Reason */}
        {step === 'reason' && (
          <div className="px-6 py-4">
            <p className="text-sm text-gray-500 mb-4">{t.selectReason}</p>
            <div className="space-y-3">
              {REASONS.map((r) => (
                <label
                  key={r.value}
                  className={`flex items-start gap-3 p-4 border-2 rounded-xl cursor-pointer transition ${
                    selectedReason === r.value
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="reason"
                    value={r.value}
                    checked={selectedReason === r.value}
                    onChange={() => setSelectedReason(r.value)}
                    className="mt-0.5 accent-blue-500"
                  />
                  <div>
                    <p className="font-semibold text-gray-800 text-sm">{r.label}</p>
                    <p className="text-gray-500 text-xs mt-0.5">{r.description}</p>
                  </div>
                </label>
              ))}
            </div>

            <button
              onClick={handleReasonNext}
              disabled={!selectedReason}
              className="mt-5 w-full bg-blue-500 hover:bg-blue-600 disabled:opacity-40 text-white font-semibold py-2 rounded-lg transition"
            >
              {t.next}
            </button>
          </div>
        )}

        {/* Step: Summary */}
        {step === 'summary' && (
          <div className="px-6 py-4">
            <div className="mb-4 bg-blue-50 border border-blue-100 rounded-lg px-4 py-2 flex items-center gap-2 text-sm">
              <span className="text-blue-500 font-semibold">Reason:</span>
              <span className="text-blue-800">{REASONS.find(r => r.value === selectedReason)?.label}</span>
            </div>
            <p className="text-sm text-gray-500 mb-1">{t.writeSummary}</p>
            <p className="text-xs text-gray-400 mb-3">Min 10 characters, max 500</p>
            <textarea
              value={summary}
              onChange={(e) => setSummary(e.target.value)}
              rows={5}
              maxLength={500}
              placeholder={t.summaryPlaceholder}
              className="w-full px-4 py-3 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
            <div className="flex justify-between text-xs text-gray-400 mt-1 mb-4">
              <span>{summary.length < 10 ? `${10 - summary.length} more characters needed` : ''}</span>
              <span>{summary.length}/500</span>
            </div>

            {error && <p className="text-red-600 text-sm mb-3">{error}</p>}

            <div className="flex gap-3">
              <button
                onClick={() => { setStep('reason'); setError('') }}
                className="flex-1 border border-gray-300 text-gray-700 font-semibold py-2 rounded-lg hover:bg-gray-50 transition"
              >
                {t.back}
              </button>
              <button
                onClick={handleSubmit}
                disabled={summary.length < 10 || submitting}
                className="flex-1 bg-blue-500 hover:bg-blue-600 disabled:opacity-40 text-white font-semibold py-2 rounded-lg transition"
              >
                {submitting ? t.submitting : t.submitDispute}
              </button>
            </div>
          </div>
        )}

        {/* Step: Success */}
        {step === 'success' && (
          <div className="px-6 py-8 flex flex-col items-center text-center">
            <div className="bg-green-100 rounded-full p-4 mb-4">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-12 w-12 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h3 className="text-xl font-bold text-gray-800 mb-2">{t.disputeSubmitted}</h3>
            <p className="text-gray-500 text-sm mb-6">{t.disputeSubmittedMsg}</p>
            <button
              onClick={onClose}
              className="w-full bg-blue-500 hover:bg-blue-600 text-white font-semibold py-2 rounded-lg transition"
            >
              {t.close}
            </button>
          </div>
        )}

      </div>
    </div>
  )
}

function DetailModal({ disputeId, onClose }: { disputeId: string; onClose: () => void }) {
  const [detail, setDetail] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const { t } = useLanguage()

  useEffect(() => {
    apiClient.getDisputeDetail(disputeId)
      .then((data: any) => setDetail(data.data ?? data))
      .catch(() => setDetail(null))
      .finally(() => setLoading(false))
  }, [disputeId])

  const statusColor = (s: string) => {
    if (s === 'Submitted') return 'bg-blue-100 text-blue-800'
    if (s === 'UnderReview') return 'bg-yellow-100 text-yellow-800'
    if (s === 'Resolved') return 'bg-green-100 text-green-800'
    if (s === 'Rejected') return 'bg-red-100 text-red-800'
    if (s === 'Cancelled') return 'bg-gray-100 text-gray-600'
    return 'bg-gray-100 text-gray-800'
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b sticky top-0 bg-white">
          <h2 className="text-lg font-bold text-gray-800">{t.detailsOfDispute}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {loading ? (
          <div className="px-6 py-8 text-center text-gray-500">{t.loading}</div>
        ) : !detail ? (
          <div className="px-6 py-8 text-center text-red-500">{t.failedToLoad}</div>
        ) : (
          <div className="px-6 py-5 space-y-5">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase">{t.reference}</p>
                <p className="font-mono font-bold text-gray-800 mt-1">{detail.dispute?.incidentReference}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase">{t.status}</p>
                <span className={`inline-block mt-1 px-3 py-1 rounded-full text-xs font-semibold ${statusColor(detail.dispute?.status)}`}>
                  {detail.dispute?.status}
                </span>
              </div>
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase">{t.reason}</p>
                <p className="text-gray-800 mt-1">{detail.dispute?.reason}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase">{t.date}</p>
                <p className="text-gray-800 mt-1">{new Date(detail.dispute?.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}</p>
              </div>
              {detail.dispute?.resolvedAt && (
                <div>
                  <p className="text-xs text-gray-500 font-semibold uppercase">Resolved</p>
                  <p className="text-gray-800 mt-1">{new Date(detail.dispute.resolvedAt).toLocaleDateString('en-GB').replace(/\//g, '-')}</p>
                </div>
              )}
            </div>

            <div>
              <p className="text-xs text-gray-500 font-semibold uppercase mb-1">{t.summary}</p>
              <p className="text-gray-700 bg-gray-50 rounded-lg px-4 py-3 text-sm">{detail.dispute?.summary}</p>
            </div>

            {detail.dispute?.customReason && (
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase mb-1">Additional Reason</p>
                <p className="text-gray-700 bg-gray-50 rounded-lg px-4 py-3 text-sm">{detail.dispute.customReason}</p>
              </div>
            )}

            {detail.dispute?.cancellationReason && (
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase mb-1">Cancellation Reason</p>
                <p className="text-gray-700 bg-gray-50 border border-gray-200 rounded-lg px-4 py-3 text-sm">{detail.dispute.cancellationReason}</p>
              </div>
            )}

            {detail.dispute?.transaction && (
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase mb-2">{t.transactions}</p>
                <div className="bg-gray-50 rounded-lg px-4 py-3 flex justify-between items-center text-sm">
                  <span className="text-gray-700">{detail.dispute.transaction.description}</span>
                  <span className="font-bold text-gray-800">R {detail.dispute.transaction.amount}</span>
                </div>
              </div>
            )}

            {detail.statusHistory?.length > 0 && (
              <div>
                <p className="text-xs text-gray-500 font-semibold uppercase mb-2">Status History</p>
                <div className="space-y-2">
                  {detail.statusHistory.map((h: any) => (
                    <div key={h.id} className="bg-gray-50 rounded-lg px-4 py-3 text-sm">
                      <div className="flex justify-between items-center">
                        <span className="font-semibold text-gray-700">{h.newStatus}</span>
                        <span className="text-gray-400 text-xs">{new Date(h.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}</span>
                      </div>
                      {h.notes && <p className="text-gray-500 text-xs mt-1">{h.notes}</p>}
                      <p className="text-gray-400 text-xs mt-1">By: {h.employeeName}</p>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}

function CancelDisputeModal({ disputeId, onClose, onSuccess }: { disputeId: string; onClose: () => void; onSuccess: () => void }) {
  const [step, setStep] = useState<'confirm' | 'reason'>('confirm')
  const [reason, setReason] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async () => {
    if (!reason.trim()) return
    setSubmitting(true)
    setError('')
    try {
      await apiClient.cancelDispute(disputeId, reason.trim())
      onSuccess()
      onClose()
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to cancel dispute.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md">
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-lg font-bold text-gray-800">Cancel Dispute</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {step === 'confirm' ? (
          <div className="px-6 py-6">
            <p className="text-gray-700 mb-6">Are you sure you want to cancel this dispute?</p>
            <div className="flex gap-3">
              <button
                onClick={() => setStep('reason')}
                className="flex-1 bg-red-500 hover:bg-red-600 text-white font-semibold py-2 rounded-lg transition"
              >
                Yes
              </button>
              <button
                onClick={onClose}
                className="flex-1 border border-gray-300 text-gray-700 font-semibold py-2 rounded-lg hover:bg-gray-50 transition"
              >
                No
              </button>
            </div>
          </div>
        ) : (
          <div className="px-6 py-6">
            <p className="text-sm text-gray-600 mb-2">Please provide a short reason for cancelling this dispute:</p>
            <textarea
              value={reason}
              onChange={e => setReason(e.target.value.slice(0, 300))}
              rows={4}
              maxLength={300}
              placeholder="e.g. The issue was resolved directly with the merchant."
              className="w-full px-4 py-3 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            />
            <div className="flex justify-between text-xs text-gray-400 mt-1 mb-4">
              <span>{reason.trim().length < 5 ? `${5 - reason.trim().length} more characters needed` : ''}</span>
              <span>{reason.length}/300</span>
            </div>
            {error && <p className="text-red-600 text-sm mb-3">{error}</p>}
            <div className="flex gap-3">
              <button
                onClick={() => { setStep('confirm'); setError('') }}
                className="flex-1 border border-gray-300 text-gray-700 font-semibold py-2 rounded-lg hover:bg-gray-50 transition"
              >
                Back
              </button>
              <button
                onClick={handleSubmit}
                disabled={reason.trim().length < 5 || submitting}
                className="flex-1 bg-red-500 hover:bg-red-600 disabled:opacity-40 text-white font-semibold py-2 rounded-lg transition"
              >
                {submitting ? 'Cancelling...' : 'Submit & Cancel Dispute'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

const ACTIVE_STATUSES = ['Submitted', 'Pending', 'UnderReview']
const HISTORICAL_STATUSES = ['Resolved', 'Rejected', 'Cancelled']
const LS_HISTORY_KEY = 'disputes_history_last_viewed'

export default function DisputesPage() {
  const [disputes, setDisputes] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [tab, setTab] = useState<'active' | 'historical'>('active')
  const [modalTx, setModalTx] = useState<{ transactionId: string; description: string; amount: number } | null>(null)
  const [openMenuId, setOpenMenuId] = useState<string | null>(null)
  const [menuPos, setMenuPos] = useState<{ top: number; right: number } | null>(null)
  const [detailDisputeId, setDetailDisputeId] = useState<string | null>(null)
  const [cancelDisputeId, setCancelDisputeId] = useState<string | null>(null)
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest')
  const [filterDate, setFilterDate] = useState('')
  const [filterStatus, setFilterStatus] = useState('')
  const [bannerDismissed, setBannerDismissed] = useState(false)
  const [, setLastViewedHistory] = useState<number>(0)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useLanguage()

  const handleLogout = async () => {
    await apiClient.logout()
    navigate('/login')
  }

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpenMenuId(null)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  useEffect(() => {
    const state = location.state as any
    if (state?.transactionId) {
      setModalTx({ transactionId: state.transactionId, description: state.description, amount: state.amount })
      navigate('/disputes', { replace: true, state: null })
    }
    loadDisputes()
  }, [])

  const storedLastViewed = parseInt(localStorage.getItem(LS_HISTORY_KEY) ?? '0', 10)
  const newHistoricalDisputes = disputes.filter(d => {
    if (!HISTORICAL_STATUSES.includes(d.status)) return false
    const closedAt = d.resolvedAt ? new Date(d.resolvedAt + 'Z').getTime() : new Date(d.createdAt + 'Z').getTime()
    return closedAt > storedLastViewed
  })
  const newHistoricalCount = newHistoricalDisputes.length

  // Reset filters when switching tabs
  const handleTabChange = (newTab: 'active' | 'historical') => {
    setTab(newTab)
    setFilterDate('')
    setFilterStatus('')
    if (newTab === 'historical') {
      const now = Date.now()
      localStorage.setItem(LS_HISTORY_KEY, now.toString())
      setLastViewedHistory(now)
      setBannerDismissed(true)
    }
  }

  const tabStatuses = tab === 'active' ? ACTIVE_STATUSES : HISTORICAL_STATUSES
  const statusOptions = tabStatuses

  const filteredDisputes = disputes
    .filter(d => tabStatuses.includes(d.status))
    .filter(d => {
      if (filterDate) {
        const disputeDate = new Date(d.createdAt).toISOString().slice(0, 10)
        if (disputeDate !== filterDate) return false
      }
      if (filterStatus && d.status !== filterStatus) return false
      return true
    })
    .sort((a, b) => {
      const diff = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
      return sortOrder === 'newest' ? -diff : diff
    })

  const loadDisputes = async () => {
    try {
      const data = await apiClient.getDisputes(1, 200)
      setDisputes(data.data?.disputes || [])
    } catch (err) {
      setError('Failed to load disputes')
    } finally {
      setLoading(false)
    }
  }

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Submitted': return 'bg-blue-100 text-blue-800'
      case 'Pending': return 'bg-yellow-100 text-yellow-800'
      case 'UnderReview': return 'bg-orange-100 text-orange-800'
      case 'Resolved': return 'bg-green-100 text-green-800'
      case 'Rejected': return 'bg-red-100 text-red-800'
      case 'Cancelled': return 'bg-gray-100 text-gray-600'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  return (
    <div className="min-h-screen bg-gray-100">
      {modalTx && (
        <DisputeModal
          transactionId={modalTx.transactionId}
          description={modalTx.description}
          amount={modalTx.amount}
          onClose={() => { setModalTx(null); loadDisputes() }}
          onSuccess={loadDisputes}
        />
      )}

      {detailDisputeId && (
        <DetailModal
          disputeId={detailDisputeId}
          onClose={() => setDetailDisputeId(null)}
        />
      )}

      {cancelDisputeId && (
        <CancelDisputeModal
          disputeId={cancelDisputeId}
          onClose={() => setCancelDisputeId(null)}
          onSuccess={loadDisputes}
        />
      )}

      <nav className="bg-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center gap-4">
              <button onClick={() => navigate('/dashboard')} className="flex items-center gap-2 hover:opacity-80 transition-opacity">
                <img src={capitecLogo} alt="Capitec Bank" className="h-8 w-auto" />
                <h1 className="text-2xl font-bold text-blue-600">{t.disputes}</h1>
              </button>
              {/* Desktop tabs */}
              <div className="hidden md:flex gap-1">
                <button
                  onClick={() => handleTabChange('active')}
                  className={`px-4 py-1.5 rounded-lg text-sm font-semibold transition ${tab === 'active' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:text-blue-600 hover:bg-gray-100'}`}
                >
                  Active
                </button>
                <button
                  onClick={() => handleTabChange('historical')}
                  className={`relative px-4 py-1.5 rounded-lg text-sm font-semibold transition ${tab === 'historical' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:text-blue-600 hover:bg-gray-100'}`}
                >
                  Historical
                  {newHistoricalCount > 0 && tab !== 'historical' && (
                    <span className="absolute -top-1.5 -right-1.5 h-4 min-w-4 px-1 bg-red-500 text-white text-xs rounded-full flex items-center justify-center leading-none">
                      {newHistoricalCount}
                    </span>
                  )}
                </button>
              </div>
            </div>

            {/* Desktop right items */}
            <div className="hidden md:flex items-center gap-3">
              <button onClick={() => navigate('/dashboard')} className="flex items-center gap-1 text-gray-600 hover:text-blue-600">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                </svg>
                {t.dashboard}
              </button>
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

          {/* Mobile tabs bar */}
          <div className="flex md:hidden gap-2 pb-2 border-t border-gray-100 pt-2">
            <button
              onClick={() => { handleTabChange('active'); setMobileMenuOpen(false) }}
              className={`flex-1 py-2 rounded-lg text-sm font-semibold transition ${tab === 'active' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'}`}
            >
              Active
            </button>
            <button
              onClick={() => { handleTabChange('historical'); setMobileMenuOpen(false) }}
              className={`relative flex-1 py-2 rounded-lg text-sm font-semibold transition ${tab === 'historical' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'}`}
            >
              Historical
              {newHistoricalCount > 0 && tab !== 'historical' && (
                <span className="absolute top-0.5 right-4 h-4 min-w-4 px-1 bg-red-500 text-white text-xs rounded-full flex items-center justify-center leading-none">
                  {newHistoricalCount}
                </span>
              )}
            </button>
          </div>

          {/* Mobile dropdown menu */}
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
        {error && <div className="bg-red-100 text-red-700 p-4 rounded mb-4">{error}</div>}

        {tab === 'active' && newHistoricalCount > 0 && !bannerDismissed && (
          <div className="bg-blue-50 border border-blue-200 rounded-lg px-4 py-3 mb-4 flex items-center justify-between gap-4">
            <div className="flex items-center gap-2 text-blue-800 text-sm">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5 text-blue-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M12 2a10 10 0 100 20A10 10 0 0012 2z" />
              </svg>
              <span>
                {newHistoricalCount === 1
                  ? '1 dispute has been resolved or rejected and moved to the '
                  : `${newHistoricalCount} disputes have been resolved or rejected and moved to the `}
                <button
                  onClick={() => handleTabChange('historical')}
                  className="font-semibold underline hover:text-blue-600"
                >
                  Historical tab
                </button>.
              </span>
            </div>
            <button
              onClick={() => setBannerDismissed(true)}
              className="text-blue-400 hover:text-blue-700 shrink-0 text-lg leading-none"
              aria-label="Dismiss"
            >
              ✕
            </button>
          </div>
        )}

        {/* Sort & Filter bar */}
        <div className="bg-white rounded-lg shadow px-4 py-3 mb-4 flex flex-wrap gap-3 items-center">
          <div className="flex items-center gap-2">
            <label className="text-sm font-medium text-gray-600">Sort:</label>
            <select
              value={sortOrder}
              onChange={e => setSortOrder(e.target.value as 'newest' | 'oldest')}
              className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="newest">Newest First</option>
              <option value="oldest">Oldest First</option>
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className="text-sm font-medium text-gray-600">Date:</label>
            <input
              type="date"
              value={filterDate}
              onChange={e => setFilterDate(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {filterDate && (
              <button onClick={() => setFilterDate('')} className="text-gray-400 hover:text-gray-600 text-sm font-bold">✕</button>
            )}
          </div>
          <div className="flex items-center gap-2">
            <label className="text-sm font-medium text-gray-600">Status:</label>
            <select
              value={filterStatus}
              onChange={e => setFilterStatus(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All</option>
              {statusOptions.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          {(filterDate || filterStatus || sortOrder !== 'newest') && (
            <button
              onClick={() => { setFilterDate(''); setFilterStatus(''); setSortOrder('newest') }}
              className="text-sm text-blue-600 hover:text-blue-800 font-medium"
            >
              Clear filters
            </button>
          )}
        </div>

        <div className="bg-white rounded-lg shadow overflow-hidden">
          {loading ? (
            <div className="px-6 py-4 text-center">{t.loading}</div>
          ) : filteredDisputes.length === 0 ? (
            <div className="px-6 py-4 text-center text-gray-600">{t.noDisputes}</div>
          ) : (
            <>
              {/* Mobile card view */}
              <div className="md:hidden divide-y divide-gray-100">
                {filteredDisputes.map((dispute) => (
                  <div key={dispute.id} className="px-4 py-4">
                    <div className="flex items-center justify-between mb-1">
                      <span className="font-mono text-sm font-semibold text-gray-800">{dispute.incidentReference}</span>
                      <span className={`px-3 py-1 rounded-full text-xs font-semibold ${getStatusColor(dispute.status)}`}>
                        {dispute.status}
                      </span>
                    </div>
                    <p className="text-sm text-gray-600 mb-2">{dispute.reason}</p>
                    <div className="flex items-center justify-between">
                      <span className="text-xs text-gray-400">
                        {new Date(dispute.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}
                      </span>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() => setDetailDisputeId(dispute.id)}
                          className="text-blue-600 font-medium text-xs bg-blue-50 px-3 py-1 rounded-lg"
                        >
                          {t.detailsOfDispute}
                        </button>
                        {ACTIVE_STATUSES.includes(dispute.status) && (
                          <button
                            onClick={() => setCancelDisputeId(dispute.id)}
                            className="text-red-600 font-medium text-xs bg-red-50 px-3 py-1 rounded-lg"
                          >
                            Cancel
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {/* Desktop table view */}
              <div className="hidden md:block overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-gray-100">
                    <tr>
                      <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.reference}</th>
                      <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.reason}</th>
                      <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.status}</th>
                      <th className="px-6 py-3 text-left font-semibold text-gray-700">{t.date}</th>
                      <th className="px-6 py-3"></th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredDisputes.map((dispute) => (
                      <tr key={dispute.id} className="border-t hover:bg-gray-50">
                        <td className="px-6 py-4 font-mono text-sm font-semibold">{dispute.incidentReference}</td>
                        <td className="px-6 py-4 text-sm">{dispute.reason}</td>
                        <td className="px-6 py-4">
                          <span className={`px-3 py-1 rounded-full text-xs font-semibold ${getStatusColor(dispute.status)}`}>
                            {dispute.status}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-sm">{new Date(dispute.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}</td>
                        <td className="px-6 py-4 text-right">
                          <div className="relative inline-block" ref={openMenuId === dispute.id ? menuRef : null}>
                            <button
                              onClick={(e) => {
                                if (openMenuId === dispute.id) {
                                  setOpenMenuId(null)
                                  setMenuPos(null)
                                } else {
                                  const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect()
                                  const dropdownHeight = 40
                                  const spaceBelow = window.innerHeight - rect.bottom
                                  const top = spaceBelow < dropdownHeight + 8
                                    ? rect.top - dropdownHeight - 4
                                    : rect.bottom + 4
                                  setMenuPos({ top, right: window.innerWidth - rect.right })
                                  setOpenMenuId(dispute.id)
                                }
                              }}
                              className="p-1 rounded hover:bg-gray-200 text-gray-500 hover:text-gray-700"
                              aria-label="Options"
                            >
                              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
                                <circle cx="12" cy="5" r="1.5" />
                                <circle cx="12" cy="12" r="1.5" />
                                <circle cx="12" cy="19" r="1.5" />
                              </svg>
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>
      </div>

      {openMenuId && menuPos && (
        <div
          ref={menuRef}
          style={{ position: 'fixed', top: menuPos.top, right: menuPos.right, zIndex: 50 }}
          className="w-48 bg-white border border-gray-200 rounded-lg shadow-lg"
        >
          <button
            onClick={() => { setOpenMenuId(null); setMenuPos(null); setDetailDisputeId(openMenuId) }}
            className="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-t-lg"
          >
            {t.detailsOfDispute}
          </button>
          {ACTIVE_STATUSES.includes(disputes.find(d => d.id === openMenuId)?.status ?? '') && (
            <button
              onClick={() => { setOpenMenuId(null); setMenuPos(null); setCancelDisputeId(openMenuId) }}
              className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 rounded-b-lg"
            >
              Cancel dispute
            </button>
          )}
        </div>
      )}
    </div>
  )
}

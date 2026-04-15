import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import apiClient from '../../services/api'
import { getFirstName, getEmployeeCode } from '../../utils/auth'
import capitecLogo from '../../assets/symbol.png'

const STATUS_OPTIONS = ['Pending', 'UnderReview', 'Resolved', 'Rejected']
const ACTIVE_STATUSES = ['Submitted', 'Pending', 'UnderReview']
const HISTORICAL_STATUSES = ['Resolved', 'Rejected', 'Cancelled']

const STATUS_COLORS: Record<string, string> = {
  Submitted: 'bg-gray-100 text-gray-700',
  Pending: 'bg-yellow-100 text-yellow-800',
  UnderReview: 'bg-blue-100 text-blue-800',
  Resolved: 'bg-green-100 text-green-800',
  Rejected: 'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-500',
}
const STAT_CARD_COLORS: Record<string, { border: string; label: string; count: string }> = {
  Pending: { border: 'border-yellow-200', label: 'text-yellow-600', count: 'text-yellow-700' },
  UnderReview: { border: 'border-blue-200', label: 'text-blue-600', count: 'text-blue-700' },
  Resolved: { border: 'border-green-200', label: 'text-green-600', count: 'text-green-700' },
  Rejected: { border: 'border-red-200', label: 'text-red-600', count: 'text-red-700' },
  Cancelled: { border: 'border-gray-200', label: 'text-gray-500', count: 'text-gray-600' },
}

interface Dispute {
  id: string
  transactionId: string
  reason: string
  summary: string
  summaryEnglish?: string
  summaryLanguage?: string
  status: string
  createdAt: string
  customerEmail?: string
  cancellationReason?: string
  cancellationReasonEnglish?: string
  cancellationReasonLanguage?: string
}

interface UpdateModalState {
  disputeId: string
  currentStatus: string
}

export default function EmployeeDashboardPage() {
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<'active' | 'historical'>('active')
  const [disputes, setDisputes] = useState<Dispute[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [updateModal, setUpdateModal] = useState<UpdateModalState | null>(null)
  const [newStatus, setNewStatus] = useState('')
  const [notes, setNotes] = useState('')
  const [updating, setUpdating] = useState(false)
  const [updateSuccess, setUpdateSuccess] = useState(false)
  const [bookCall, setBookCall] = useState(false)
  const [pageNumber, setPageNumber] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const pageSize = 20
  const [refSearch, setRefSearch] = useState('')
  const [refResult, setRefResult] = useState<any>(null)
  const [refLoading, setRefLoading] = useState(false)
  const [refError, setRefError] = useState('')
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest'>('newest')
  const [filterDate, setFilterDate] = useState('')
  const [filterStatus, setFilterStatus] = useState('')
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const employeeName = getFirstName() || 'Employee'
  const employeeCode = getEmployeeCode() || ''

  const fetchDisputes = async () => {
    setLoading(true)
    setError('')
    try {
      const data = await apiClient.getAllDisputes(pageNumber, pageSize)
      if (data.success) {
        setDisputes(data.data?.disputes || [])
        setTotalCount(data.data?.totalCount || 0)
      }
    } catch {
      setError('Failed to load disputes.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchDisputes()
  }, [pageNumber])

  // Reset page and filters when switching tabs
  const handleTabChange = (tab: 'active' | 'historical') => {
    setActiveTab(tab)
    setPageNumber(1)
    setFilterDate('')
    setFilterStatus('')
  }

  const handleRefSearch = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!refSearch.trim()) return
    setRefLoading(true)
    setRefError('')
    setRefResult(null)
    try {
      const data = await apiClient.getDisputeByReference(refSearch.trim().toUpperCase())
      if (data.success) {
        setRefResult(data.data)
      } else {
        setRefError(data.message || 'No dispute found.')
      }
    } catch (err: any) {
      setRefError(err?.response?.data?.message || 'No dispute found with that reference number.')
    } finally {
      setRefLoading(false)
    }
  }

  const handleLogout = async () => {
    await apiClient.logout()
    navigate('/employee/login')
  }

  const openUpdateModal = (dispute: Dispute) => {
    setUpdateModal({ disputeId: dispute.id, currentStatus: dispute.status })
    setNewStatus(dispute.status)
    setNotes('')
    setBookCall(false)
    setUpdateSuccess(false)
  }

  const handleUpdateStatus = async () => {
    if (!updateModal) return
    setUpdating(true)
    try {
      await apiClient.updateDisputeStatus(updateModal.disputeId, newStatus, notes, bookCall && newStatus === 'UnderReview')
      setUpdateSuccess(true)
      await fetchDisputes()
      if ((newStatus === 'Resolved' || newStatus === 'Rejected') && refResult?.id === updateModal.disputeId) {
        setRefResult(null)
        setRefSearch('')
        setRefError('')
      }
    } catch {
      // error handled by global interceptor if 401
    } finally {
      setUpdating(false)
    }
  }

  const activeDisputes = disputes.filter(d => ACTIVE_STATUSES.includes(d.status))
  const historicalDisputes = disputes.filter(d => HISTORICAL_STATUSES.includes(d.status))
  const displayedDisputes = activeTab === 'active' ? activeDisputes : historicalDisputes

  const statusOptions = activeTab === 'active' ? ACTIVE_STATUSES : HISTORICAL_STATUSES

  const filteredDisputes = displayedDisputes
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

  const totalPages = Math.ceil(totalCount / pageSize)

  const DisputeTable = ({ rows }: { rows: Dispute[] }) => (
    rows.length === 0 ? (
      <div className="p-10 text-center text-gray-400">No disputes found.</div>
    ) : activeTab === 'active' ? (
      /* Active disputes — card grid on all screen sizes */
      <div className="p-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {rows.map(dispute => (
          <div key={dispute.id} className="border border-gray-200 rounded-xl p-4 hover:shadow-md transition-shadow bg-white flex flex-col gap-2">
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-gray-700 truncate max-w-[65%]">
                {dispute.customerEmail || '—'}
              </span>
              <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[dispute.status] || 'bg-gray-100 text-gray-700'}`}>
                {dispute.status === 'UnderReview' ? 'Under Review' : dispute.status}
              </span>
            </div>
            <p className="text-sm text-gray-600">{dispute.reason}</p>
            {(dispute.summaryEnglish || dispute.summary) && (
              <p className="text-xs text-gray-400 line-clamp-2">
                {dispute.summaryEnglish || dispute.summary}
                {dispute.summaryEnglish && dispute.summaryEnglish !== dispute.summary && (
                  <span className="ml-1 text-blue-400">· Translated{dispute.summaryLanguage ? ` (${dispute.summaryLanguage})` : ''}</span>
                )}
              </p>
            )}
            <div className="flex items-center justify-between mt-auto pt-2 border-t border-gray-100">
              <span className="text-xs text-gray-400">
                {new Date(dispute.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}
              </span>
              <button
                onClick={() => openUpdateModal(dispute)}
                className="text-blue-600 hover:text-blue-700 font-medium text-xs bg-blue-50 hover:bg-blue-100 px-3 py-1 rounded-lg transition"
              >
                Update Status
              </button>
            </div>
          </div>
        ))}
      </div>
    ) : (
      /* Historical disputes — mobile cards + desktop table */
      <>
        <div className="md:hidden divide-y divide-gray-100">
          {rows.map(dispute => (
            <div key={dispute.id} className="px-4 py-4">
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm font-medium text-gray-700 truncate max-w-[60%]">
                  {dispute.customerEmail || '—'}
                </span>
                <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[dispute.status] || 'bg-gray-100 text-gray-700'}`}>
                  {dispute.status === 'UnderReview' ? 'Under Review' : dispute.status}
                </span>
              </div>
              <p className="text-sm text-gray-600 mb-1">{dispute.reason}</p>
              {(dispute.summaryEnglish || dispute.summary) && (
                <p className="text-xs text-gray-400 truncate mb-2">
                  {dispute.summaryEnglish || dispute.summary}
                  {dispute.summaryEnglish && dispute.summaryEnglish !== dispute.summary && (
                    <span className="ml-1 text-blue-400">· Translated</span>
                  )}
                </p>
              )}
              <div className="flex items-center justify-between">
                <span className="text-xs text-gray-400">
                  {new Date(dispute.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}
                </span>
                {(dispute.cancellationReasonEnglish || dispute.cancellationReason) && (
                  <span className="text-xs text-gray-400 text-right">
                    <span className="block truncate max-w-[50%]">{dispute.cancellationReasonEnglish || dispute.cancellationReason}</span>
                    {dispute.cancellationReasonEnglish && dispute.cancellationReasonEnglish !== dispute.cancellationReason && (
                      <span className="text-blue-400">Translated{dispute.cancellationReasonLanguage ? `: ${dispute.cancellationReasonLanguage}` : ''}</span>
                    )}
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>

        <div className="hidden md:block overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-xs text-gray-500 uppercase">
              <tr>
                <th className="px-4 py-3 text-left">ID</th>
                <th className="px-4 py-3 text-left">Customer</th>
                <th className="px-4 py-3 text-left">Reason</th>
                <th className="px-4 py-3 text-left">Summary</th>
                <th className="px-4 py-3 text-left">Status</th>
                <th className="px-4 py-3 text-left">Date</th>
                <th className="px-4 py-3 text-left">Cancellation Reason</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {rows.map(dispute => (
                <tr key={dispute.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-xs text-gray-500">
                    {dispute.id.substring(0, 8)}...
                  </td>
                  <td className="px-4 py-3 text-gray-700">{dispute.customerEmail || '—'}</td>
                  <td className="px-4 py-3 text-gray-700">{dispute.reason}</td>
                  <td className="px-4 py-3 text-gray-500 max-w-xs">
                    <span className="block truncate">{dispute.summaryEnglish || dispute.summary || '—'}</span>
                    {dispute.summaryEnglish && dispute.summaryEnglish !== dispute.summary && (
                      <span className="text-xs text-blue-400">
                        Translated{dispute.summaryLanguage ? `: ${dispute.summaryLanguage}` : ''}
                      </span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[dispute.status] || 'bg-gray-100 text-gray-700'}`}>
                      {dispute.status === 'UnderReview' ? 'Under Review' : dispute.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-500">
                    {new Date(dispute.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}
                  </td>
                  <td className="px-4 py-3 text-gray-500 text-xs max-w-xs">
                    <span className="block truncate">{dispute.cancellationReasonEnglish || dispute.cancellationReason || '—'}</span>
                    {dispute.cancellationReasonEnglish && dispute.cancellationReasonEnglish !== dispute.cancellationReason && (
                      <span className="text-xs text-blue-400">
                        Translated{dispute.cancellationReasonLanguage ? `: ${dispute.cancellationReasonLanguage}` : ''}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </>
    )
  )

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b border-gray-200">
        <div className="px-4 sm:px-6 py-4 flex items-center justify-between">
          {/* Logo — always visible */}
          <div className="flex items-center gap-3">
            <img src={capitecLogo} alt="Capitec" className="h-8 w-auto" />
            <div>
              <h1 className="text-lg font-bold text-gray-800">Employee Portal</h1>
              <p className="text-xs text-gray-500">Dispute Management</p>
            </div>
          </div>

          {/* Desktop tabs */}
          <div className="hidden md:flex items-center gap-6">
            <button
              onClick={() => handleTabChange('active')}
              className={`text-sm font-semibold ${activeTab === 'active' ? 'text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
            >
              Active Disputes
            </button>
            <button
              onClick={() => handleTabChange('historical')}
              className={`text-sm font-semibold ${activeTab === 'historical' ? 'text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
            >
              Historical Disputes
            </button>
          </div>

          {/* Desktop right */}
          <div className="hidden md:flex items-center gap-4">
            <span className="text-sm text-gray-600">Welcome, <strong>{employeeName}</strong></span>
            {employeeCode && (
              <span className="text-xs bg-blue-50 text-blue-700 border border-blue-200 rounded px-2 py-0.5 font-mono font-semibold">
                {employeeCode}
              </span>
            )}
            <button onClick={handleLogout} className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg text-sm">
              Logout
            </button>
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

        {/* Mobile tab bar */}
        <div className="flex md:hidden gap-2 px-4 pb-2 border-t border-gray-100 pt-2">
          <button
            onClick={() => { handleTabChange('active'); setMobileMenuOpen(false) }}
            className={`flex-1 py-2 rounded-lg text-sm font-semibold transition ${activeTab === 'active' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'}`}
          >
            Active
          </button>
          <button
            onClick={() => { handleTabChange('historical'); setMobileMenuOpen(false) }}
            className={`flex-1 py-2 rounded-lg text-sm font-semibold transition ${activeTab === 'historical' ? 'bg-blue-500 text-white' : 'text-gray-600 hover:bg-gray-100'}`}
          >
            Historical
          </button>
        </div>

        {/* Mobile dropdown */}
        {mobileMenuOpen && (
          <div className="md:hidden border-t border-gray-100 px-4 py-3 space-y-2">
            <p className="text-sm text-gray-600 px-1">Welcome, <strong>{employeeName}</strong>{employeeCode && <span className="ml-2 text-xs bg-blue-50 text-blue-700 border border-blue-200 rounded px-2 py-0.5 font-mono font-semibold">{employeeCode}</span>}</p>
            <button onClick={handleLogout} className="w-full bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-lg font-semibold text-sm">
              Logout
            </button>
          </div>
        )}
      </header>

      <main className="max-w-7xl mx-auto px-6 py-8">
        {/* Reference number lookup */}
        <div className="bg-white rounded-xl border border-gray-200 px-6 py-4 mb-6">
          <h2 className="text-sm font-semibold text-gray-700 mb-3">Look Up Dispute by Reference Number</h2>
          <form onSubmit={handleRefSearch} className="flex gap-3">
            <input
              type="text"
              value={refSearch}
              onChange={e => setRefSearch(e.target.value.toUpperCase())}
              placeholder="e.g. A1B2C3D4"
              className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            <button
              type="submit"
              disabled={refLoading || !refSearch.trim()}
              className="bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white font-semibold px-5 py-2 rounded-lg text-sm"
            >
              {refLoading ? 'Searching...' : 'Search'}
            </button>
            {refResult && (
              <button type="button" onClick={() => { setRefResult(null); setRefSearch(''); setRefError('') }}
                className="text-gray-500 hover:text-gray-700 text-sm px-3">
                Clear
              </button>
            )}
          </form>

          {refError && (
            <p className="mt-3 text-sm text-red-600">{refError}</p>
          )}

          {refResult && (
            <div className="mt-4 border border-gray-100 rounded-lg overflow-hidden">
              {/* Mobile card */}
              <div className="md:hidden bg-blue-50 px-4 py-4">
                <div className="flex items-center justify-between mb-1">
                  <span className="font-mono font-semibold text-blue-700 text-sm">{refResult.incidentReference}</span>
                  <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[refResult.status] || 'bg-gray-100 text-gray-700'}`}>
                    {refResult.status === 'UnderReview' ? 'Under Review' : refResult.status}
                  </span>
                </div>
                <p className="text-sm text-gray-700 mb-1">{refResult.customerEmail || '—'}</p>
                <p className="text-sm text-gray-600 mb-1">{refResult.reason}</p>
                {(refResult.summaryEnglish || refResult.summary) && (
                  <p className="text-xs text-gray-400 truncate mb-2">
                    {refResult.summaryEnglish || refResult.summary}
                    {refResult.summaryEnglish && refResult.summaryEnglish !== refResult.summary && (
                      <span className="ml-1 text-blue-400">· Translated</span>
                    )}
                  </p>
                )}
                <div className="flex items-center justify-between">
                  <span className="text-xs text-gray-400">{new Date(refResult.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}</span>
                  <button
                    onClick={() => openUpdateModal(refResult)}
                    className="text-blue-600 font-medium text-xs bg-white px-3 py-1 rounded-lg border border-blue-200"
                  >
                    Update Status
                  </button>
                </div>
              </div>

              {/* Desktop table */}
              <table className="hidden md:table w-full text-sm">
                <thead className="bg-gray-50 text-xs text-gray-500 uppercase">
                  <tr>
                    <th className="px-4 py-2 text-left">Reference</th>
                    <th className="px-4 py-2 text-left">Customer</th>
                    <th className="px-4 py-2 text-left">Reason</th>
                    <th className="px-4 py-2 text-left">Summary</th>
                    <th className="px-4 py-2 text-left">Status</th>
                    <th className="px-4 py-2 text-left">Date</th>
                    <th className="px-4 py-2 text-left">Action</th>
                  </tr>
                </thead>
                <tbody>
                  <tr className="bg-blue-50">
                    <td className="px-4 py-3 font-mono font-semibold text-blue-700">{refResult.incidentReference}</td>
                    <td className="px-4 py-3 text-gray-700">{refResult.customerEmail || '—'}</td>
                    <td className="px-4 py-3 text-gray-700">{refResult.reason}</td>
                    <td className="px-4 py-3 text-gray-500 max-w-xs">
                      <span className="block truncate">{refResult.summaryEnglish || refResult.summary || '—'}</span>
                      {refResult.summaryEnglish && refResult.summaryEnglish !== refResult.summary && (
                        <span className="text-xs text-blue-400">
                          Translated{refResult.summaryLanguage ? `: ${refResult.summaryLanguage}` : ''}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[refResult.status] || 'bg-gray-100 text-gray-700'}`}>
                        {refResult.status === 'UnderReview' ? 'Under Review' : refResult.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-500">{new Date(refResult.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-')}</td>
                    <td className="px-4 py-3">
                      <button
                        onClick={() => openUpdateModal(refResult)}
                        className="text-blue-600 hover:text-blue-800 font-medium text-xs"
                      >
                        Update Status
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Stat cards */}
        <div className={`grid gap-4 mb-8 ${activeTab === 'historical' ? 'grid-cols-3' : 'grid-cols-2'}`}>
          {(activeTab === 'active' ? ['Pending', 'UnderReview'] : ['Resolved', 'Rejected', 'Cancelled']).map(status => {
            const count = disputes.filter(d => d.status === status).length
            const colors = STAT_CARD_COLORS[status]
            return (
              <div key={status} className={`bg-white rounded-xl border ${colors.border} p-4`}>
                <p className={`text-xs font-medium mb-1 ${colors.label}`}>{status === 'UnderReview' ? 'Under Review' : status}</p>
                <p className={`text-2xl font-bold ${colors.count}`}>{count}</p>
              </div>
            )
          })}
        </div>

        {/* Table */}
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-100">
            <div className="flex items-center justify-between mb-3">
              <h2 className="font-semibold text-gray-800">
                {activeTab === 'active' ? 'Active Disputes' : 'Historical Disputes'}
              </h2>
              <span className="text-sm text-gray-500">{filteredDisputes.length} shown</span>
            </div>
            {/* Sort & Filter bar */}
            <div className="flex flex-wrap gap-3 items-center">
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
                  {statusOptions.map(s => <option key={s} value={s}>{s === 'UnderReview' ? 'Under Review' : s}</option>)}
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
          </div>

          {loading ? (
            <div className="p-10 text-center text-gray-400">Loading disputes...</div>
          ) : error ? (
            <div className="p-10 text-center text-red-500">{error}</div>
          ) : (
            <DisputeTable rows={filteredDisputes} />
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between">
              <button
                onClick={() => setPageNumber(p => Math.max(1, p - 1))}
                disabled={pageNumber === 1}
                className="text-sm px-3 py-1.5 border border-gray-200 rounded-lg disabled:opacity-40 hover:bg-gray-50"
              >
                Previous
              </button>
              <span className="text-sm text-gray-500">Page {pageNumber} of {totalPages}</span>
              <button
                onClick={() => setPageNumber(p => Math.min(totalPages, p + 1))}
                disabled={pageNumber === totalPages}
                className="text-sm px-3 py-1.5 border border-gray-200 rounded-lg disabled:opacity-40 hover:bg-gray-50"
              >
                Next
              </button>
            </div>
          )}
        </div>
      </main>

      {/* Update Status Modal */}
      {updateModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md p-6">
            {updateSuccess ? (
              <div className="text-center py-4">
                <div className="w-14 h-14 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                  <svg className="w-7 h-7 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <h3 className="text-lg font-bold text-gray-800 mb-2">Status Updated</h3>
                <p className="text-gray-500 text-sm mb-6">The dispute status has been updated successfully.</p>
                <button
                  onClick={() => setUpdateModal(null)}
                  className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2.5 rounded-lg"
                >
                  Done
                </button>
              </div>
            ) : (
              <>
                <h3 className="text-lg font-bold text-gray-800 mb-1">Update Dispute Status</h3>
                <p className="text-sm text-gray-500 mb-5">
                  ID: <span className="font-mono">{updateModal.disputeId.substring(0, 8)}...</span>
                </p>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">New Status</label>
                    <select
                      value={newStatus}
                      onChange={e => setNewStatus(e.target.value)}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    >
                      {STATUS_OPTIONS.map(s => (
                        <option key={s} value={s}>{s === 'UnderReview' ? 'Under Review' : s}</option>
                      ))}
                    </select>
                  </div>
                  {newStatus === 'UnderReview' && (
                    <div className="flex items-start gap-3 bg-blue-50 border border-blue-200 rounded-lg px-4 py-3">
                      <input
                        type="checkbox"
                        id="bookCall"
                        checked={bookCall}
                        onChange={e => setBookCall(e.target.checked)}
                        className="mt-0.5 h-4 w-4 text-blue-600 border-gray-300 rounded cursor-pointer"
                      />
                      <label htmlFor="bookCall" className="text-sm text-blue-800 cursor-pointer">
                        <span className="font-medium">Book a call with the customer</span>
                        <span className="block text-blue-600 text-xs mt-0.5">The customer will be notified that an employee will call them within 15 minutes.</span>
                      </label>
                    </div>
                  )}
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Notes (optional)</label>
                    <textarea
                      value={notes}
                      onChange={e => setNotes(e.target.value)}
                      rows={3}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                      placeholder="Add any notes about this status change..."
                    />
                  </div>
                </div>

                <div className="flex gap-3 mt-6">
                  <button
                    onClick={() => setUpdateModal(null)}
                    className="flex-1 border border-gray-300 text-gray-700 font-medium py-2.5 rounded-lg hover:bg-gray-50 text-sm"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={handleUpdateStatus}
                    disabled={updating}
                    className="flex-1 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-semibold py-2.5 rounded-lg text-sm"
                  >
                    {updating ? 'Updating...' : 'Update Status'}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { LanguageProvider } from '../context/LanguageContext'
import TransactionsPage from '../pages/TransactionsPage'

// Mock the API module
vi.mock('../services/api', () => ({
  default: {
    getTransactions: vi.fn(),
    createSimulatedTransaction: vi.fn(),
    clearToken: vi.fn(),
  },
}))

// Mock the logo asset
vi.mock('../assets/symbol.png', () => ({ default: 'mocked-logo.png' }))

import apiClient from '../services/api'

const mockApi = apiClient as unknown as {
  getTransactions: ReturnType<typeof vi.fn>
  createSimulatedTransaction: ReturnType<typeof vi.fn>
}

function renderPage() {
  return render(
    <MemoryRouter>
      <LanguageProvider>
        <TransactionsPage />
      </LanguageProvider>
    </MemoryRouter>
  )
}

describe('TransactionsPage — amount display', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('displays a negative amount with a minus prefix and red colour', async () => {
    mockApi.getTransactions.mockResolvedValue({
      data: {
        transactions: [
          {
            id: 'tx-1',
            amount: -299.99,
            currency: 'ZAR',
            description: 'Woolworths Food',
            date: '2024-01-15T10:00:00Z',
            status: 'Completed',
          },
        ],
      },
    })

    renderPage()

    await waitFor(() => {
      const cell = screen.getByText('-R 299.99')
      expect(cell).toBeInTheDocument()
      expect(cell).toHaveClass('text-red-600')
    })
  })

  it('displays a positive amount with a plus prefix and green colour', async () => {
    mockApi.getTransactions.mockResolvedValue({
      data: {
        transactions: [
          {
            id: 'tx-2',
            amount: 1200.00,
            currency: 'ZAR',
            description: 'Salary - Capitec Bank',
            date: '2024-01-01T08:00:00Z',
            status: 'Completed',
          },
        ],
      },
    })

    renderPage()

    await waitFor(() => {
      const cell = screen.getByText('+R 1200.00')
      expect(cell).toBeInTheDocument()
      expect(cell).toHaveClass('text-green-600')
    })
  })

  it('formats amounts to two decimal places', async () => {
    mockApi.getTransactions.mockResolvedValue({
      data: {
        transactions: [
          {
            id: 'tx-3',
            amount: -45.5,
            currency: 'ZAR',
            description: 'Uber Ride',
            date: '2024-01-10T12:00:00Z',
            status: 'Completed',
          },
        ],
      },
    })

    renderPage()

    await waitFor(() => {
      expect(screen.getByText('-R 45.50')).toBeInTheDocument()
    })
  })

  it('renders description and status alongside the amount', async () => {
    mockApi.getTransactions.mockResolvedValue({
      data: {
        transactions: [
          {
            id: 'tx-4',
            amount: -650.00,
            currency: 'ZAR',
            description: 'Edgars Clothing',
            date: '2024-01-12T09:00:00Z',
            status: 'Completed',
          },
        ],
      },
    })

    renderPage()

    await waitFor(() => {
      expect(screen.getByText('Edgars Clothing')).toBeInTheDocument()
      expect(screen.getByText('Completed')).toBeInTheDocument()
    })
  })

  it('shows empty state when no transactions exist', async () => {
    mockApi.getTransactions.mockResolvedValue({
      data: { transactions: [] },
    })

    renderPage()

    await waitFor(() => {
      // The translated "no transactions" text is rendered in English by default
      expect(screen.getByText(/no transactions/i)).toBeInTheDocument()
    })
  })

  it('shows error message when transactions fail to load', async () => {
    mockApi.getTransactions.mockRejectedValue(new Error('Network error'))

    renderPage()

    await waitFor(() => {
      expect(screen.getByText(/failed to load transactions/i)).toBeInTheDocument()
    })
  })

  it('renders a Dispute button for each transaction', async () => {
    mockApi.getTransactions.mockResolvedValue({
      data: {
        transactions: [
          {
            id: 'tx-5',
            amount: -899.00,
            currency: 'ZAR',
            description: 'Makro Electronics',
            date: '2024-01-20T14:00:00Z',
            status: 'Completed',
          },
          {
            id: 'tx-6',
            amount: 500.00,
            currency: 'ZAR',
            description: 'Refund - Takealot',
            date: '2024-01-18T11:00:00Z',
            status: 'Completed',
          },
        ],
      },
    })

    renderPage()

    await waitFor(() => {
      const disputeButtons = screen.getAllByRole('button', { name: /dispute/i })
      expect(disputeButtons).toHaveLength(2)
    })
  })
})

import { describe, it, expect, beforeEach, vi } from 'vitest'

// Mock axios before importing apiClient
vi.mock('axios', () => {
  const mockClient = {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    interceptors: {
      response: { use: vi.fn() },
      request: { use: vi.fn() },
    },
  }
  return {
    default: {
      create: vi.fn(() => mockClient),
      get: vi.fn(),
    },
  }
})

import axios from 'axios'
import apiClient from '../services/api'

const mockAxiosInstance = (axios.create as ReturnType<typeof vi.fn>).mock.results[0].value

describe('ApiClient', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  // ── auth endpoints ────────────────────────────────────────────────────────

  describe('auth endpoints', () => {
    it('sendRegistrationCode calls POST /auth/register/send-code', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.sendRegistrationCode('user@example.com', 'John')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/register/send-code', {
        email: 'user@example.com',
        firstName: 'John',
      })
    })

    it('register calls POST /auth/register with all fields including verificationCode', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.register('user@example.com', 'Password1!', 'Password1!', 'John', 'Doe', '+27821234567', '123456')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/register', {
        email: 'user@example.com',
        password: 'Password1!',
        confirmPassword: 'Password1!',
        firstName: 'John',
        lastName: 'Doe',
        phoneNumber: '+27821234567',
        verificationCode: '123456',
      })
    })

    it('login calls POST /auth/login with credentials', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.login('user@example.com', 'Password1!')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/login', {
        email: 'user@example.com',
        password: 'Password1!',
      })
    })

    it('verifyMfaAndLogin calls POST /auth/mfa/verify', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.verifyMfaAndLogin('user@example.com', '123456')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/mfa/verify', {
        email: 'user@example.com',
        mfaCode: '123456',
      })
    })

    it('logout calls POST /auth/logout', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: {} })
      await apiClient.logout()
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/logout', {})
    })

    it('forgotPassword calls POST /auth/forgot-password', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.forgotPassword('user@example.com')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/forgot-password', {
        email: 'user@example.com',
      })
    })

    it('resetPassword calls POST /auth/reset-password', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.resetPassword('user@example.com', '123456', 'NewPass1!', 'NewPass1!')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/auth/reset-password', {
        email: 'user@example.com',
        code: '123456',
        newPassword: 'NewPass1!',
        confirmNewPassword: 'NewPass1!',
      })
    })
  })

  // ── transaction endpoints ─────────────────────────────────────────────────

  describe('transaction endpoints', () => {
    it('getTransactions calls GET /transactions with pagination params', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { success: true, data: { transactions: [] } } })
      await apiClient.getTransactions()
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/transactions', expect.objectContaining({
        params: { pageNumber: 1, pageSize: 10 },
      }))
    })

    it('getTransactions passes custom pagination params', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { data: { transactions: [] } } })
      await apiClient.getTransactions(2, 5)
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/transactions', expect.objectContaining({
        params: { pageNumber: 2, pageSize: 5 },
      }))
    })

    it('createSimulatedTransaction calls POST /transactions/simulate with amount and description', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.createSimulatedTransaction(-299.99, 'Woolworths Food')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/transactions/simulate', {
        amount: -299.99,
        description: 'Woolworths Food',
      })
    })

    it('createSimulatedTransaction accepts negative amounts for outgoing transactions', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.createSimulatedTransaction(-850, 'Electricity - City Power')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/transactions/simulate', {
        amount: -850,
        description: 'Electricity - City Power',
      })
    })

    it('getTransaction calls GET /transactions/:id', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { success: true, data: {} } })
      await apiClient.getTransaction('tx-123')
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/transactions/tx-123')
    })
  })

  // ── dispute endpoints ─────────────────────────────────────────────────────

  describe('dispute endpoints', () => {
    it('getDisputes calls GET /disputes with pagination params', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { data: { disputes: [] } } })
      await apiClient.getDisputes()
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/disputes', expect.objectContaining({
        params: { pageNumber: 1, pageSize: 10 },
      }))
    })

    it('createDispute calls POST /disputes with dispute data', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.createDispute('tx-id', 'Unauthorised', undefined, 'Dispute summary text here')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/disputes', {
        transactionId: 'tx-id',
        reason: 'Unauthorised',
        customReason: undefined,
        summary: 'Dispute summary text here',
      })
    })

    it('updateDisputeStatus calls PUT /disputes/:id/status', async () => {
      mockAxiosInstance.put.mockResolvedValue({ data: { success: true } })
      await apiClient.updateDisputeStatus('dispute-id', 'Resolved', 'Case closed', false)
      expect(mockAxiosInstance.put).toHaveBeenCalledWith('/disputes/dispute-id/status', {
        newStatus: 'Resolved',
        notes: 'Case closed',
        bookCall: false,
      })
    })

    it('getDisputeDetail calls GET /disputes/:id/detail', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { success: true, data: {} } })
      await apiClient.getDisputeDetail('dispute-id')
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/disputes/dispute-id/detail')
    })

    it('cancelDispute calls POST /disputes/:id/cancel', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.cancelDispute('dispute-id', 'Fraudulent charge')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/disputes/dispute-id/cancel', {
        cancellationReason: 'Fraudulent charge',
      })
    })

    it('getDisputeByReference calls GET /employee/disputes/reference/:ref', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { success: true, data: {} } })
      await apiClient.getDisputeByReference('ABC12345')
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/employee/disputes/reference/ABC12345')
    })
  })

  // ── employee endpoints ────────────────────────────────────────────────────

  describe('employee endpoints', () => {
    it('loginEmployee calls POST /employee/login', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.loginEmployee('emp@example.com', 'EmpPass1!')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/employee/login', {
        email: 'emp@example.com',
        password: 'EmpPass1!',
      })
    })

    it('getAllDisputes calls GET /employee/disputes with pagination', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { data: { disputes: [] } } })
      await apiClient.getAllDisputes(1, 20)
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/employee/disputes', expect.objectContaining({
        params: { pageNumber: 1, pageSize: 20 },
      }))
    })

    it('sendEmployeeRegistrationCode calls POST /employee/register/send-code', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.sendEmployeeRegistrationCode('emp@example.com', 'Alice')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/employee/register/send-code', {
        email: 'emp@example.com',
        firstName: 'Alice',
      })
    })
  })

  // ── user endpoints ────────────────────────────────────────────────────────

  describe('user endpoints', () => {
    it('getUser calls GET /users/me', async () => {
      mockAxiosInstance.get.mockResolvedValue({ data: { success: true } })
      await apiClient.getUser()
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/users/me')
    })

    it('updateUser calls PUT /users/profile', async () => {
      mockAxiosInstance.put.mockResolvedValue({ data: { success: true } })
      await apiClient.updateUser('John', 'Doe', '+27821234567')
      expect(mockAxiosInstance.put).toHaveBeenCalledWith('/users/profile', {
        firstName: 'John',
        lastName: 'Doe',
        phoneNumber: '+27821234567',
      })
    })

    it('changePassword calls PUT /users/change-password', async () => {
      mockAxiosInstance.put.mockResolvedValue({ data: { success: true } })
      await apiClient.changePassword('OldPass1!', 'NewPass1!', 'NewPass1!')
      expect(mockAxiosInstance.put).toHaveBeenCalledWith('/users/change-password', {
        currentPassword: 'OldPass1!',
        newPassword: 'NewPass1!',
        confirmNewPassword: 'NewPass1!',
      })
    })

    it('requestPasswordChange calls POST /users/request-password-change', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.requestPasswordChange('OldPass1!', 'NewPass1!', 'NewPass1!')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/users/request-password-change', {
        currentPassword: 'OldPass1!',
        newPassword: 'NewPass1!',
        confirmNewPassword: 'NewPass1!',
      })
    })

    it('confirmPasswordChange calls POST /users/confirm-password-change', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.confirmPasswordChange('123456')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/users/confirm-password-change', {
        code: '123456',
      })
    })

    it('requestProfileChange calls POST /users/request-profile-change', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.requestProfileChange('Jane', 'Smith', '+27831234567')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/users/request-profile-change', {
        firstName: 'Jane',
        lastName: 'Smith',
        phoneNumber: '+27831234567',
      })
    })

    it('confirmProfileChange calls POST /users/confirm-profile-change', async () => {
      mockAxiosInstance.post.mockResolvedValue({ data: { success: true } })
      await apiClient.confirmProfileChange('654321')
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/users/confirm-profile-change', {
        code: '654321',
      })
    })
  })
})

import axios, { AxiosInstance } from 'axios'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'
const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS'])

// Read the CSRF token the BFF sets as a non-HttpOnly cookie
function getCsrfCookie(): string | null {
  if (typeof document === 'undefined') return null
  const entry = document.cookie.split(';').find(c => c.trim().startsWith('csrf_token='))
  return entry ? entry.split('=').slice(1).join('=').trim() : null
}

interface AuthResponse {
  success: boolean
  userId?: string
  message?: string
  requiresMfa?: boolean
  mfaQrCode?: string
}

class ApiClient {
  private client: AxiosInstance

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      withCredentials: true, // send HttpOnly auth cookie on every request
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Attach CSRF token to every state-changing request
    this.client.interceptors.request.use(async config => {
      const method = (config.method ?? 'get').toUpperCase()
      if (!SAFE_METHODS.has(method)) {
        let csrf = getCsrfCookie()
        if (!csrf) {
          // Fetch it from the BFF (sets the cookie) then read it
          await axios.get(`${API_BASE_URL}/csrf-token`, { withCredentials: true })
          csrf = getCsrfCookie()
        }
        if (csrf) {
          config.headers['X-CSRF-Token'] = csrf
        }
      }
      return config
    })

    // On 401, clear session cookie and redirect to the appropriate login page
    this.client.interceptors.response.use(
      response => response,
      (error: any) => {
        const url: string = error.config?.url ?? ''
        const isAuthEndpoint =
          url.includes('/auth/login') ||
          url.includes('/employee/login') ||
          url.includes('/auth/mfa/verify')
        if (error.response?.status === 401 && !isAuthEndpoint) {
          sessionStorage.setItem('auth_msg', 'Your session has expired. Please log in again.')
          const currentPath = window.location.pathname
          window.location.href = currentPath.startsWith('/employee') ? '/employee/login' : '/login'
        }
        return Promise.reject(error)
      }
    )

    // Warm up the CSRF cookie on startup
    void this.initializeCsrf()
  }

  async initializeCsrf(): Promise<void> {
    try {
      if (!getCsrfCookie()) {
        await this.client.get('/csrf-token')
      }
    } catch {
      // Non-fatal — interceptor will retry on first POST
    }
  }

  async logout(): Promise<void> {
    try {
      await this.client.post('/auth/logout', {})
    } catch {
      // Even if the BFF call fails, proceed with redirect
    }
  }

  // ── Auth endpoints ────────────────────────────────────────────────────────

  async sendRegistrationCode(email: string, firstName: string) {
    const response = await this.client.post('/auth/register/send-code', { email, firstName })
    return response.data
  }

  async register(email: string, password: string, confirmPassword: string, firstName: string, lastName: string, phoneNumber: string, verificationCode: string) {
    const response = await this.client.post<AuthResponse>('/auth/register', {
      email, password, confirmPassword, firstName, lastName, phoneNumber, verificationCode,
    })
    return response.data
  }

  async login(email: string, password: string) {
    const response = await this.client.post<AuthResponse>('/auth/login', { email, password })
    return response.data
  }

  async generateMfa() {
    const response = await this.client.post('/auth/mfa/generate', {})
    return response.data
  }

  async enableMfa(mfaCode: string) {
    const response = await this.client.post('/auth/mfa/enable', JSON.stringify(mfaCode), {
      headers: { 'Content-Type': 'application/json' },
    })
    return response.data
  }

  async verifyMfaAndLogin(email: string, mfaCode: string) {
    const response = await this.client.post<AuthResponse>('/auth/mfa/verify', { email, mfaCode })
    return response.data
  }

  // ── Transaction endpoints ─────────────────────────────────────────────────

  async getTransactions(pageNumber: number = 1, pageSize: number = 10) {
    const response = await this.client.get('/transactions', { params: { pageNumber, pageSize } })
    return response.data
  }

  async getTransaction(id: string) {
    const response = await this.client.get(`/transactions/${id}`)
    return response.data
  }

  async createSimulatedTransaction(amount: number, description: string) {
    const response = await this.client.post('/transactions/simulate', { amount, description })
    return response.data
  }

  // ── Dispute endpoints ─────────────────────────────────────────────────────

  async getDisputes(pageNumber: number = 1, pageSize: number = 10) {
    const response = await this.client.get('/disputes', { params: { pageNumber, pageSize } })
    return response.data
  }

  async getDispute(id: string) {
    const response = await this.client.get(`/disputes/${id}`)
    return response.data
  }

  async createDispute(transactionId: string, reason: string, customReason?: string, summary?: string) {
    const response = await this.client.post('/disputes', { transactionId, reason, customReason, summary })
    return response.data
  }

  async getDisputeDetail(id: string) {
    const response = await this.client.get(`/disputes/${id}/detail`)
    return response.data
  }

  async cancelDispute(disputeId: string, cancellationReason: string) {
    const response = await this.client.post(`/disputes/${disputeId}/cancel`, { cancellationReason })
    return response.data
  }

  async updateDisputeStatus(disputeId: string, newStatus: string, notes?: string, bookCall?: boolean) {
    const response = await this.client.put(`/disputes/${disputeId}/status`, { newStatus, notes, bookCall })
    return response.data
  }

  // ── Employee endpoints ────────────────────────────────────────────────────

  async sendEmployeeRegistrationCode(email: string, firstName: string) {
    const response = await this.client.post('/employee/register/send-code', { email, firstName })
    return response.data
  }

  async registerEmployee(email: string, password: string, confirmPassword: string, firstName: string, lastName: string, phoneNumber: string, department: string, verificationCode: string) {
    const response = await this.client.post('/employee/register', {
      email, password, confirmPassword, firstName, lastName, phoneNumber, department, verificationCode,
    })
    return response.data
  }

  async loginEmployee(email: string, password: string) {
    const response = await this.client.post('/employee/login', { email, password })
    return response.data
  }

  async getDisputeByReference(reference: string) {
    const response = await this.client.get(`/employee/disputes/reference/${encodeURIComponent(reference)}`)
    return response.data
  }

  async getAllDisputes(pageNumber: number = 1, pageSize: number = 20) {
    const response = await this.client.get('/employee/disputes', { params: { pageNumber, pageSize } })
    return response.data
  }

  // ── User endpoints ────────────────────────────────────────────────────────

  async getUser() {
    const response = await this.client.get('/users/me')
    return response.data
  }

  async updateUser(firstName: string, lastName: string, phoneNumber: string) {
    const response = await this.client.put('/users/profile', { firstName, lastName, phoneNumber })
    return response.data
  }

  async changePassword(currentPassword: string, newPassword: string, confirmNewPassword: string) {
    const response = await this.client.put('/users/change-password', {
      currentPassword, newPassword, confirmNewPassword,
    })
    return response.data
  }

  async requestProfileChange(firstName: string, lastName: string, phoneNumber: string) {
    const response = await this.client.post('/users/request-profile-change', { firstName, lastName, phoneNumber })
    return response.data
  }

  async confirmProfileChange(code: string) {
    const response = await this.client.post('/users/confirm-profile-change', { code })
    return response.data
  }

  async requestPasswordChange(currentPassword: string, newPassword: string, confirmNewPassword: string) {
    const response = await this.client.post('/users/request-password-change', {
      currentPassword, newPassword, confirmNewPassword,
    })
    return response.data
  }

  async confirmPasswordChange(code: string) {
    const response = await this.client.post('/users/confirm-password-change', { code })
    return response.data
  }

  async forgotPassword(email: string) {
    const response = await this.client.post('/auth/forgot-password', { email })
    return response.data
  }

  async resetPassword(email: string, code: string, newPassword: string, confirmNewPassword: string) {
    const response = await this.client.post('/auth/reset-password', {
      email, code, newPassword, confirmNewPassword,
    })
    return response.data
  }
}

export default new ApiClient()

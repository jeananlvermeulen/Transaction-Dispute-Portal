import 'dotenv/config'
import express, { Request, Response, NextFunction } from 'express'
import cookieParser from 'cookie-parser'
import cors from 'cors'
import axios from 'axios'
import https from 'https'
import { randomUUID } from 'crypto'
import rateLimit from 'express-rate-limit'

// ─── Config ────────────────────────────────────────────────────────────────
const PORT = parseInt(process.env.BFF_PORT ?? '4000', 10)
const BACKEND_URL = (process.env.BACKEND_URL ?? 'https://localhost:53839').replace(/\/$/, '')
const FRONTEND_ORIGIN = process.env.FRONTEND_ORIGIN ?? 'http://localhost:3000'
const IS_PROD = process.env.NODE_ENV === 'production'

const AUTH_COOKIE = 'auth_token'
const SESSION_COOKIE = 'auth_session'
const CSRF_COOKIE = 'csrf_token'

const SAFE_METHODS = new Set(['GET', 'HEAD', 'OPTIONS'])

// Allow self-signed cert in dev; enforce in production
const httpsAgent = new https.Agent({ rejectUnauthorized: IS_PROD })

// ─── App setup ─────────────────────────────────────────────────────────────
const app = express()

app.use(cors({
  origin: FRONTEND_ORIGIN,
  credentials: true,
}))

app.use(cookieParser())
app.use(express.json({ limit: '2mb' }))

// ─── Rate limiters ─────────────────────────────────────────────────────────
const authLimiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 15,
  standardHeaders: true,
  legacyHeaders: false,
  message: { message: 'Too many requests, please try again later.' },
})

const passwordLimiter = rateLimit({
  windowMs: 60 * 60 * 1000, // 1 hour
  max: 10,
  standardHeaders: true,
  legacyHeaders: false,
  message: { message: 'Too many password reset attempts, please try again later.' },
})

// ─── CSRF — double-submit cookie ───────────────────────────────────────────
function validateCsrf(req: Request, res: Response, next: NextFunction): void {
  if (SAFE_METHODS.has(req.method)) { next(); return }
  const cookieToken: string | undefined = req.cookies[CSRF_COOKIE]
  const headerToken = req.headers['x-csrf-token'] as string | undefined
  if (!cookieToken || cookieToken !== headerToken) {
    res.status(403).json({ message: 'Invalid CSRF token' })
    return
  }
  next()
}

app.get('/api/csrf-token', (_req, res) => {
  const token = randomUUID()
  res.cookie(CSRF_COOKIE, token, {
    httpOnly: false,     // must be readable by JS
    sameSite: 'strict',
    secure: IS_PROD,
    maxAge: 2 * 60 * 60 * 1000, // 2 hours
  })
  res.json({ csrfToken: token })
})

// ─── Cookie helpers ────────────────────────────────────────────────────────
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
const GIVEN_NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    return JSON.parse(Buffer.from(token.split('.')[1], 'base64url').toString('utf-8'))
  } catch {
    return null
  }
}

const cookieOpts = (maxAge: number): express.CookieOptions => ({
  sameSite: 'strict',
  secure: IS_PROD,
  maxAge,
})

function setAuthCookies(res: Response, token: string): void {
  const payload = decodeJwtPayload(token) ?? {}
  const role = String(payload[ROLE_CLAIM] ?? '')
  const firstName = String(payload[GIVEN_NAME_CLAIM] ?? '')
  const employeeCode = String(payload['EmployeeCode'] ?? '')
  const maxAge = 60 * 60 * 1000 // 1 hour

  // JWT itself — HttpOnly, JS cannot read
  res.cookie(AUTH_COOKIE, token, { ...cookieOpts(maxAge), httpOnly: true })

  // Non-sensitive session metadata — readable by JS for routing/display
  const sessionData = Buffer.from(JSON.stringify({ role, firstName, employeeCode })).toString('base64')
  res.cookie(SESSION_COOKIE, sessionData, { ...cookieOpts(maxAge), httpOnly: false })
}

function clearAuthCookies(res: Response): void {
  res.clearCookie(AUTH_COOKIE)
  res.clearCookie(SESSION_COOKIE)
}

// ─── Generic proxy helper ──────────────────────────────────────────────────
async function proxyToBackend(req: Request, res: Response): Promise<void> {
  const token: string | undefined = req.cookies[AUTH_COOKIE]
  const targetUrl = `${BACKEND_URL}${req.originalUrl}`

  const headers: Record<string, string> = {}
  if (req.headers['content-type']) {
    headers['Content-Type'] = req.headers['content-type']
  }
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  try {
    const response = await axios({
      method: req.method as 'get' | 'post' | 'put' | 'patch' | 'delete',
      url: targetUrl,
      headers,
      data: SAFE_METHODS.has(req.method) ? undefined : req.body,
      params: req.query,
      httpsAgent,
      validateStatus: () => true,
    })
    res.status(response.status).json(response.data)
  } catch {
    res.status(502).json({ message: 'Upstream service unavailable' })
  }
}

// ─── Auth routes — token extracted, stored in HttpOnly cookie ──────────────

app.post('/api/auth/login', authLimiter, validateCsrf, async (req, res) => {
  try {
    const response = await axios.post(`${BACKEND_URL}/api/auth/login`, req.body, {
      httpsAgent,
      validateStatus: () => true,
    })
    const data: Record<string, unknown> = response.data
    if (typeof data.token === 'string') {
      setAuthCookies(res, data.token)
      const { token: _t, ...safe } = data
      return void res.status(response.status).json(safe)
    }
    res.status(response.status).json(data)
  } catch {
    res.status(502).json({ message: 'Login service unavailable' })
  }
})

app.post('/api/auth/mfa/verify', authLimiter, validateCsrf, async (req, res) => {
  try {
    const response = await axios.post(`${BACKEND_URL}/api/auth/mfa/verify`, req.body, {
      httpsAgent,
      validateStatus: () => true,
    })
    const data: Record<string, unknown> = response.data
    if (typeof data.token === 'string') {
      setAuthCookies(res, data.token)
      const { token: _t, ...safe } = data
      return void res.status(response.status).json(safe)
    }
    res.status(response.status).json(data)
  } catch {
    res.status(502).json({ message: 'MFA service unavailable' })
  }
})

app.post('/api/employee/login', authLimiter, validateCsrf, async (req, res) => {
  try {
    const response = await axios.post(`${BACKEND_URL}/api/employee/login`, req.body, {
      httpsAgent,
      validateStatus: () => true,
    })
    const data: Record<string, unknown> = response.data
    if (typeof data.token === 'string') {
      setAuthCookies(res, data.token)
      const { token: _t, ...safe } = data
      return void res.status(response.status).json(safe)
    }
    res.status(response.status).json(data)
  } catch {
    res.status(502).json({ message: 'Login service unavailable' })
  }
})

app.post('/api/auth/logout', validateCsrf, (_req, res) => {
  clearAuthCookies(res)
  res.json({ success: true })
})

// ─── Rate-limited public auth routes (proxied) ────────────────────────────
app.use('/api/auth/register/send-code', authLimiter, validateCsrf, proxyToBackend)
app.use('/api/auth/register', authLimiter, validateCsrf, proxyToBackend)
app.use('/api/auth/forgot-password', passwordLimiter, validateCsrf, proxyToBackend)
app.use('/api/auth/reset-password', passwordLimiter, validateCsrf, proxyToBackend)
app.use('/api/employee/register/send-code', authLimiter, validateCsrf, proxyToBackend)
app.use('/api/employee/register', authLimiter, validateCsrf, proxyToBackend)

// ─── Generic proxy — all remaining /api/* routes ──────────────────────────
app.use('/api', validateCsrf, proxyToBackend)

// ─── Start ─────────────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`BFF listening on http://localhost:${PORT}`)
  console.log(`Proxying to backend: ${BACKEND_URL}`)
  console.log(`Accepting requests from: ${FRONTEND_ORIGIN}`)
})

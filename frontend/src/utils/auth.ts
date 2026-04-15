// Session data is stored in a non-HttpOnly cookie set by the BFF.
// The JWT itself lives in an HttpOnly cookie and is never accessible to JS.

interface SessionData {
  role?: string
  firstName?: string
  employeeCode?: string
}

function getSessionFromCookie(): SessionData | null {
  if (typeof document === 'undefined') return null
  const entry = document.cookie.split(';').find(c => c.trim().startsWith('auth_session='))
  if (!entry) return null
  try {
    const encoded = decodeURIComponent(entry.split('=').slice(1).join('=').trim())
    return JSON.parse(atob(encoded)) as SessionData
  } catch {
    return null
  }
}

export function getRole(): string | null {
  return getSessionFromCookie()?.role ?? null
}

export function getFirstName(): string | null {
  return getSessionFromCookie()?.firstName ?? null
}

export function getEmployeeCode(): string | null {
  return getSessionFromCookie()?.employeeCode ?? null
}

// Alias kept for call sites that used getEmployeeId
export function getEmployeeId(): string | null {
  return getEmployeeCode()
}

export function isAuthenticated(): boolean {
  return getSessionFromCookie() !== null
}

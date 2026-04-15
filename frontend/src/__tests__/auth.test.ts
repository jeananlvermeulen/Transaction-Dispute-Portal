import { describe, it, expect, beforeEach } from 'vitest'
import { getRole, getFirstName, getEmployeeCode, getEmployeeId, isAuthenticated } from '../utils/auth'

// Helper: write a valid auth_session cookie as the BFF would set it
function setSessionCookie(data: Record<string, unknown>) {
  const encoded = btoa(JSON.stringify(data))
  document.cookie = `auth_session=${encodeURIComponent(encoded)}`
}

function clearSessionCookie() {
  document.cookie = 'auth_session=; expires=Thu, 01 Jan 1970 00:00:00 GMT'
}

describe('auth utils', () => {
  beforeEach(() => {
    clearSessionCookie()
  })

  describe('getRole', () => {
    it('returns the role from the session cookie', () => {
      setSessionCookie({ role: 'Employee' })
      expect(getRole()).toBe('Employee')
    })

    it('returns null when the cookie is absent', () => {
      expect(getRole()).toBeNull()
    })

    it('returns null when the role key is missing from the session', () => {
      setSessionCookie({ firstName: 'Alice' })
      expect(getRole()).toBeNull()
    })
  })

  describe('getFirstName', () => {
    it('returns the first name from the session cookie', () => {
      setSessionCookie({ firstName: 'Alice' })
      expect(getFirstName()).toBe('Alice')
    })

    it('returns null when the cookie is absent', () => {
      expect(getFirstName()).toBeNull()
    })

    it('returns null when firstName key is missing from the session', () => {
      setSessionCookie({ role: 'Customer' })
      expect(getFirstName()).toBeNull()
    })
  })

  describe('getEmployeeCode', () => {
    it('returns the employee code from the session cookie', () => {
      setSessionCookie({ employeeCode: 'EMP-123456' })
      expect(getEmployeeCode()).toBe('EMP-123456')
    })

    it('returns null when the cookie is absent', () => {
      expect(getEmployeeCode()).toBeNull()
    })

    it('returns null when employeeCode key is missing from the session', () => {
      setSessionCookie({ role: 'Employee' })
      expect(getEmployeeCode()).toBeNull()
    })
  })

  describe('getEmployeeId', () => {
    it('returns the employee code via the getEmployeeId alias', () => {
      setSessionCookie({ employeeCode: 'EMP-654321' })
      expect(getEmployeeId()).toBe('EMP-654321')
    })

    it('returns null when the cookie is absent', () => {
      expect(getEmployeeId()).toBeNull()
    })
  })

  describe('isAuthenticated', () => {
    it('returns true when a valid session cookie is present', () => {
      setSessionCookie({ role: 'Customer' })
      expect(isAuthenticated()).toBe(true)
    })

    it('returns false when no session cookie is present', () => {
      expect(isAuthenticated()).toBe(false)
    })
  })
})

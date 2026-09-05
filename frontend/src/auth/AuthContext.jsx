import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import {
  ApiError,
  login as loginRequest,
  refreshSession,
  register as registerRequest,
  revokeSession,
  setSessionListener,
} from '../services/authApi'

const AuthContext = createContext(null)

export const AuthProvider = ({ children }) => {
  const [session, setSession] = useState(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    setSessionListener(setSession)
    refreshSession()
      .catch((error) => {
        if (!(error instanceof ApiError) || error.status !== 401) console.error(error)
      })
      .finally(() => setIsLoading(false))
    return () => setSessionListener(null)
  }, [])

  useEffect(() => {
    if (!session?.expiresAt) return undefined
    const delay = Math.max(new Date(session.expiresAt).getTime() - Date.now() - 30_000, 1_000)
    const timer = window.setTimeout(() => refreshSession().catch(() => setSession(null)), delay)
    return () => window.clearTimeout(timer)
  }, [session?.expiresAt])

  const login = useCallback(async (credentials) => {
    const nextSession = await loginRequest(credentials)
    setSession(nextSession)
    return nextSession
  }, [])

  const register = useCallback(async (account) => {
    const nextSession = await registerRequest(account)
    setSession(nextSession)
    return nextSession
  }, [])

  const logout = useCallback(async () => {
    try {
      await revokeSession()
    } catch {
      // Local logout still succeeds when the API is temporarily unavailable.
      setSession(null)
    }
  }, [])

  const value = useMemo(() => ({
    session,
    user: session?.user || null,
    isAuthenticated: Boolean(session?.user),
    isLoading,
    login,
    register,
    logout,
  }), [session, isLoading, login, register, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// The hook intentionally lives beside its provider so they share one private context.
// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider.')
  return context
}

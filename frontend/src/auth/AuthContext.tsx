import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { LoginResponse } from '../api/types'
import { clearToken, isAuthenticated, setToken } from './token'

interface AuthContextValue {
  authenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [authenticated, setAuthenticated] = useState(isAuthenticated())

  const value = useMemo<AuthContextValue>(
    () => ({
      authenticated,
      async login(username, password) {
        const { data } = await api.post<LoginResponse>('/auth/login', { username, password })
        setToken(data.accessToken)
        setAuthenticated(true)
      },
      logout() {
        clearToken()
        setAuthenticated(false)
      },
    }),
    [authenticated],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return ctx
}

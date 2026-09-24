import { useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { LoginResponse } from '../api/types'
import { clearToken, isAuthenticated, setToken } from './token'
import { AuthContext, type AuthContextValue } from './useAuth'

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

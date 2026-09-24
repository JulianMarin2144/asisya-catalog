import { useState, type FormEvent } from 'react'
import { isAxiosError } from 'axios'
import { Navigate, useNavigate } from 'react-router-dom'
import { getApiErrorMessage } from '../api/errors'
import { useAuth } from '../auth/useAuth'

export function LoginPage() {
  const { authenticated, login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  if (authenticated) {
    return <Navigate to="/products" replace />
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await login(username.trim(), password)
      navigate('/products', { replace: true })
    } catch (err) {
      setError(
        isAxiosError(err) && err.response?.status === 401
          ? 'Usuario o contraseña incorrectos.'
          : getApiErrorMessage(err, 'No se pudo iniciar sesión. Inténtalo de nuevo.'),
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="page auth-page">
      <form className="card form" onSubmit={onSubmit} aria-describedby={error ? 'login-error' : undefined}>
        <h1>Asisya Catalog</h1>
        <p className="muted">Inicia sesión para gestionar productos</p>
        <label>
          Usuario
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
            maxLength={64}
            required
            autoFocus
          />
        </label>
        <label>
          Contraseña
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
          />
        </label>
        {error && (
          <p id="login-error" className="error" role="alert">
            {error}
          </p>
        )}
        <button type="submit" disabled={loading}>
          {loading ? 'Entrando…' : 'Entrar'}
        </button>
        {import.meta.env.DEV && (
          <p className="muted small">
            Demo: admin / Admin123! · API <code>{import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5192'}</code>
          </p>
        )}
      </form>
    </main>
  )
}

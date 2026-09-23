import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { authenticated, login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('admin')
  const [password, setPassword] = useState('Admin123!')
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
      await login(username, password)
      navigate('/products', { replace: true })
    } catch {
      setError('Credenciales inválidas o API no disponible.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="page auth-page">
      <form className="card form" onSubmit={onSubmit}>
        <h1>Asisya Catalog</h1>
        <p className="muted">Inicia sesión para gestionar productos</p>
        <label>
          Usuario
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" required />
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
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={loading}>
          {loading ? 'Entrando…' : 'Entrar'}
        </button>
        <p className="muted small">
          Demo: admin / Admin123! · API <code>{import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5192'}</code>
        </p>
        <Link to="/products" className="muted small" style={{ pointerEvents: 'none', opacity: 0.4 }}>
          Rutas de productos protegidas
        </Link>
      </form>
    </main>
  )
}

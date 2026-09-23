import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { CategoryDto, PagedResult, ProductDto } from '../api/types'
import { useAuth } from '../auth/AuthContext'

export function ProductsPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [data, setData] = useState<PagedResult<ProductDto> | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const { data: result } = await api.get<PagedResult<ProductDto>>('/Products', {
        params: {
          page,
          pageSize,
          search: search || undefined,
          categoryId: categoryId || undefined,
        },
      })
      setData(result)
    } catch {
      setError('No se pudo cargar el listado de productos.')
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, search, categoryId])

  useEffect(() => {
    api
      .get<CategoryDto[]>('/Category')
      .then((res) => setCategories(res.data))
      .catch(() => setCategories([]))
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  async function onDelete(id: string, name: string) {
    if (!window.confirm(`¿Eliminar el producto "${name}"?`)) {
      return
    }
    try {
      await api.delete(`/Product/${id}`)
      await load()
    } catch {
      setError('No se pudo eliminar el producto.')
    }
  }

  const totalPages = data?.totalPages ?? 0

  return (
    <main className="page">
      <header className="toolbar">
        <div>
          <h1>Productos</h1>
          <p className="muted">Catálogo Asisya</p>
        </div>
        <div className="actions">
          <Link className="button" to="/products/new">
            Nuevo producto
          </Link>
          <button
            type="button"
            className="button secondary"
            onClick={() => {
              logout()
              navigate('/login', { replace: true })
            }}
          >
            Salir
          </button>
        </div>
      </header>

      <section className="card filters">
        <label>
          Buscar
          <input
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            placeholder="Nombre…"
          />
        </label>
        <label>
          Categoría
          <select
            value={categoryId}
            onChange={(e) => {
              setPage(1)
              setCategoryId(e.target.value)
            }}
          >
            <option value="">Todas</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
      </section>

      {error && <p className="error">{error}</p>}
      {loading && <p className="muted">Cargando…</p>}

      {!loading && data && (
        <>
          <div className="table-wrap card">
            <table>
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Categoría</th>
                  <th>Precio</th>
                  <th>Stock</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="muted">
                      No hay productos.
                    </td>
                  </tr>
                )}
                {data.items.map((p) => (
                  <tr key={p.id}>
                    <td>{p.name}</td>
                    <td>{p.categoryName}</td>
                    <td>{p.price.toFixed(2)}</td>
                    <td>{p.stock}</td>
                    <td className="row-actions">
                      <Link to={`/products/${p.id}/edit`}>Editar</Link>
                      <button type="button" className="link danger" onClick={() => void onDelete(p.id, p.name)}>
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="pager">
            <span className="muted">
              Página {data.page} de {Math.max(totalPages, 1)} · {data.totalCount} total
            </span>
            <div className="actions">
              <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                Anterior
              </button>
              <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                Siguiente
              </button>
            </div>
          </footer>
        </>
      )}
    </main>
  )
}

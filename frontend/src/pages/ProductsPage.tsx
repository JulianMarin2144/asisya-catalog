import { useEffect, useState } from 'react'
import { isCancel } from 'axios'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { getApiErrorMessage } from '../api/errors'
import type { CategoryDto, PagedResult, ProductDto } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { useDebouncedValue } from '../hooks/useDebouncedValue'

const PAGE_SIZE = 10
const SEARCH_DEBOUNCE_MS = 300
const priceFormat = new Intl.NumberFormat('es-CO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function ProductsPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim(), SEARCH_DEBOUNCE_MS)
  const [categoryId, setCategoryId] = useState('')
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [data, setData] = useState<PagedResult<ProductDto> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)
  const [settledQuery, setSettledQuery] = useState<string | null>(null)
  const [pendingDelete, setPendingDelete] = useState<ProductDto | null>(null)
  const [deleting, setDeleting] = useState(false)

  const query = JSON.stringify([page, debouncedSearch, categoryId, reloadKey])
  const loading = settledQuery !== query

  useEffect(() => {
    const controller = new AbortController()
    api
      .get<PagedResult<ProductDto>>('/Products', {
        params: {
          page,
          pageSize: PAGE_SIZE,
          search: debouncedSearch || undefined,
          categoryId: categoryId || undefined,
        },
        signal: controller.signal,
      })
      .then((res) => {
        setData(res.data)
        setError(null)
        setSettledQuery(query)
      })
      .catch((err: unknown) => {
        if (isCancel(err)) {
          return
        }
        setError(getApiErrorMessage(err, 'No se pudo cargar el listado de productos.'))
        setSettledQuery(query)
      })
    return () => controller.abort()
  }, [query, page, debouncedSearch, categoryId])

  useEffect(() => {
    api
      .get<CategoryDto[]>('/Category')
      .then((res) => setCategories(res.data))
      .catch(() => setCategories([]))
  }, [])

  async function confirmDelete() {
    if (!pendingDelete) {
      return
    }
    setDeleting(true)
    try {
      await api.delete(`/Product/${pendingDelete.id}`)
      setPendingDelete(null)
      if (data?.items.length === 1 && page > 1) {
        setPage((p) => p - 1)
      } else {
        setReloadKey((k) => k + 1)
      }
    } catch (err) {
      setPendingDelete(null)
      setError(getApiErrorMessage(err, 'No se pudo eliminar el producto.'))
    } finally {
      setDeleting(false)
    }
  }

  const totalPages = data?.totalPages ?? 0
  const status = loading
    ? 'Cargando productos…'
    : data
      ? `Página ${data.page} de ${Math.max(totalPages, 1)} · ${data.totalCount} productos`
      : ''

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

      <section className="card filters" aria-label="Filtros">
        <label>
          Buscar
          <input
            type="search"
            value={search}
            maxLength={200}
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

      <p className="muted status" role="status" aria-live="polite">
        {status}
      </p>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      {data && (
        <>
          <div className="table-wrap card" aria-busy={loading}>
            <table>
              <caption className="sr-only">Listado de productos</caption>
              <thead>
                <tr>
                  <th scope="col">Nombre</th>
                  <th scope="col">Categoría</th>
                  <th scope="col" className="numeric">
                    Precio
                  </th>
                  <th scope="col" className="numeric">
                    Stock
                  </th>
                  <th scope="col">
                    <span className="sr-only">Acciones</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="muted">
                      {debouncedSearch || categoryId ? 'Ningún producto coincide con los filtros.' : 'No hay productos.'}
                    </td>
                  </tr>
                )}
                {data.items.map((p) => (
                  <tr key={p.id}>
                    <td>{p.name}</td>
                    <td>{p.categoryName}</td>
                    <td className="numeric">{priceFormat.format(p.price)}</td>
                    <td className="numeric">{p.stock}</td>
                    <td className="row-actions">
                      <Link to={`/products/${p.id}/edit`} aria-label={`Editar ${p.name}`}>
                        Editar
                      </Link>
                      <button
                        type="button"
                        className="link danger"
                        aria-label={`Eliminar ${p.name}`}
                        onClick={() => setPendingDelete(p)}
                      >
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <footer className="pager">
            <div className="actions">
              <button type="button" disabled={loading || page <= 1} onClick={() => setPage((p) => p - 1)}>
                Anterior
              </button>
              <button type="button" disabled={loading || page >= totalPages} onClick={() => setPage((p) => p + 1)}>
                Siguiente
              </button>
            </div>
          </footer>
        </>
      )}

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Eliminar producto"
        message={pendingDelete ? `¿Eliminar el producto "${pendingDelete.name}"? Esta acción no se puede deshacer.` : ''}
        confirmLabel={deleting ? 'Eliminando…' : 'Eliminar'}
        busy={deleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setPendingDelete(null)}
      />
    </main>
  )
}

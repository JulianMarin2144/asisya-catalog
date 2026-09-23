import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { api } from '../api/client'
import type { CategoryDto, ProductDetailDto, ProductFormValues } from '../api/types'

export function ProductFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [categories, setCategories] = useState<CategoryDto[]>([])
  const [loading, setLoading] = useState(isEdit)
  const [error, setError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProductFormValues>({
    defaultValues: {
      name: '',
      description: '',
      price: 1,
      stock: 0,
      categoryId: '',
    },
  })

  useEffect(() => {
    api
      .get<CategoryDto[]>('/Category')
      .then((res) => setCategories(res.data))
      .catch(() => setError('No se pudieron cargar las categorías.'))
  }, [])

  useEffect(() => {
    if (!id) {
      return
    }
    setLoading(true)
    api
      .get<ProductDetailDto>(`/Products/${id}`)
      .then((res) => {
        reset({
          name: res.data.name,
          description: res.data.description ?? '',
          price: res.data.price,
          stock: res.data.stock,
          categoryId: res.data.categoryId,
        })
      })
      .catch(() => setError('No se pudo cargar el producto.'))
      .finally(() => setLoading(false))
  }, [id, reset])

  async function onSubmit(values: ProductFormValues) {
    setError(null)
    const payload = {
      name: values.name.trim(),
      description: values.description?.trim() || null,
      price: Number(values.price),
      stock: Number(values.stock),
      categoryId: values.categoryId,
    }

    try {
      if (isEdit && id) {
        await api.put(`/Product/${id}`, payload)
      } else {
        await api.post('/Product', payload)
      }
      navigate('/products')
    } catch {
      setError(isEdit ? 'No se pudo actualizar el producto.' : 'No se pudo crear el producto.')
    }
  }

  return (
    <main className="page">
      <header className="toolbar">
        <div>
          <h1>{isEdit ? 'Editar producto' : 'Nuevo producto'}</h1>
          <p className="muted">
            <Link to="/products">← Volver al listado</Link>
          </p>
        </div>
      </header>

      {loading && <p className="muted">Cargando…</p>}
      {error && <p className="error">{error}</p>}

      {!loading && (
        <form className="card form" onSubmit={handleSubmit(onSubmit)}>
          <label>
            Nombre
            <input
              {...register('name', { required: 'El nombre es obligatorio' })}
              placeholder="Nombre del producto"
            />
            {errors.name && <span className="error">{errors.name.message}</span>}
          </label>

          <label>
            Descripción
            <textarea {...register('description')} rows={3} />
          </label>

          <label>
            Precio
            <input
              type="number"
              step="0.01"
              {...register('price', {
                required: 'El precio es obligatorio',
                valueAsNumber: true,
                validate: (v) => v > 0 || 'El precio debe ser mayor que 0',
              })}
            />
            {errors.price && <span className="error">{errors.price.message}</span>}
          </label>

          <label>
            Stock
            <input
              type="number"
              step="1"
              {...register('stock', {
                required: 'El stock es obligatorio',
                valueAsNumber: true,
                validate: (v) => v >= 0 || 'El stock no puede ser negativo',
              })}
            />
            {errors.stock && <span className="error">{errors.stock.message}</span>}
          </label>

          <label>
            Categoría
            <select
              {...register('categoryId', { required: 'La categoría es obligatoria' })}
            >
              <option value="">Selecciona…</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            {errors.categoryId && <span className="error">{errors.categoryId.message}</span>}
          </label>

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear producto'}
          </button>
        </form>
      )}
    </main>
  )
}

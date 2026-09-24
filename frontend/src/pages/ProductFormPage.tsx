import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { api } from '../api/client'
import { getApiErrorMessage } from '../api/errors'
import type { CategoryDto, ProductDetailDto, ProductFormValues } from '../api/types'

const NAME_MAX_LENGTH = 200
const DESCRIPTION_MAX_LENGTH = 1000

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
      .catch((err: unknown) => setError(getApiErrorMessage(err, 'No se pudieron cargar las categorías.')))
  }, [])

  useEffect(() => {
    if (!id) {
      return
    }
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
      .catch((err: unknown) => setError(getApiErrorMessage(err, 'No se pudo cargar el producto.')))
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
    } catch (err) {
      setError(getApiErrorMessage(err, isEdit ? 'No se pudo actualizar el producto.' : 'No se pudo crear el producto.'))
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

      {loading && (
        <p className="muted" role="status">
          Cargando…
        </p>
      )}
      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      {!loading && (
        <form className="card form" onSubmit={handleSubmit(onSubmit)} noValidate>
          <label>
            Nombre
            <input
              {...register('name', {
                validate: (v) => v.trim().length > 0 || 'El nombre es obligatorio',
                maxLength: { value: NAME_MAX_LENGTH, message: `Máximo ${NAME_MAX_LENGTH} caracteres` },
              })}
              maxLength={NAME_MAX_LENGTH}
              placeholder="Nombre del producto"
              aria-invalid={errors.name ? true : undefined}
              aria-describedby={errors.name ? 'name-error' : undefined}
            />
            {errors.name && (
              <span id="name-error" className="error">
                {errors.name.message}
              </span>
            )}
          </label>

          <label>
            Descripción
            <textarea
              {...register('description', {
                maxLength: { value: DESCRIPTION_MAX_LENGTH, message: `Máximo ${DESCRIPTION_MAX_LENGTH} caracteres` },
              })}
              maxLength={DESCRIPTION_MAX_LENGTH}
              rows={3}
              aria-invalid={errors.description ? true : undefined}
              aria-describedby={errors.description ? 'description-error' : undefined}
            />
            {errors.description && (
              <span id="description-error" className="error">
                {errors.description.message}
              </span>
            )}
          </label>

          <label>
            Precio
            <input
              type="number"
              step="0.01"
              min="0.01"
              inputMode="decimal"
              {...register('price', {
                valueAsNumber: true,
                validate: (v) => (Number.isFinite(v) && v > 0) || 'El precio debe ser mayor que 0',
              })}
              aria-invalid={errors.price ? true : undefined}
              aria-describedby={errors.price ? 'price-error' : undefined}
            />
            {errors.price && (
              <span id="price-error" className="error">
                {errors.price.message}
              </span>
            )}
          </label>

          <label>
            Stock
            <input
              type="number"
              step="1"
              min="0"
              inputMode="numeric"
              {...register('stock', {
                valueAsNumber: true,
                validate: (v) => (Number.isInteger(v) && v >= 0) || 'El stock debe ser un entero mayor o igual a 0',
              })}
              aria-invalid={errors.stock ? true : undefined}
              aria-describedby={errors.stock ? 'stock-error' : undefined}
            />
            {errors.stock && (
              <span id="stock-error" className="error">
                {errors.stock.message}
              </span>
            )}
          </label>

          <label>
            Categoría
            <select
              {...register('categoryId', { required: 'La categoría es obligatoria' })}
              aria-invalid={errors.categoryId ? true : undefined}
              aria-describedby={errors.categoryId ? 'category-error' : undefined}
            >
              <option value="">Selecciona…</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            {errors.categoryId && (
              <span id="category-error" className="error">
                {errors.categoryId.message}
              </span>
            )}
          </label>

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear producto'}
          </button>
        </form>
      )}
    </main>
  )
}

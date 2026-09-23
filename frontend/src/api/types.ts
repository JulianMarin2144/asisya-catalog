export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
}

export interface CategoryDto {
  id: string
  name: string
  description?: string | null
  photoUrl: string
}

export interface ProductDto {
  id: string
  name: string
  description?: string | null
  price: number
  stock: number
  categoryId: string
  categoryName: string
}

export interface ProductDetailDto extends ProductDto {
  categoryPhotoUrl: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface ProductFormValues {
  name: string
  description?: string
  price: number
  stock: number
  categoryId: string
}

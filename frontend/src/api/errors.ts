import { isAxiosError } from 'axios'

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError<ProblemDetails>(error)) {
    return fallback
  }

  if (!error.response) {
    return 'No se pudo conectar con la API. Verifica tu conexión e inténtalo de nuevo.'
  }

  const { status, data } = error.response
  if (status === 429) {
    return 'Demasiados intentos. Espera un minuto e inténtalo de nuevo.'
  }

  if (status >= 500) {
    return fallback
  }

  const validationMessages = data?.errors ? Object.values(data.errors).flat() : []
  return data?.detail ?? validationMessages[0] ?? data?.title ?? fallback
}

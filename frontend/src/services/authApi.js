const API_BASE_URL = (import.meta.env.VITE_AUTH_API_URL || import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '')

let accessToken = null
let accessTokenExpiresAt = null
let refreshPromise = null
let sessionListener = () => {}

export class ApiError extends Error {
  constructor(message, status = 0, errors = {}, data = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
    this.data = data
  }
}

const request = async (path, options = {}) => {
  let response
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      credentials: 'include',
      ...options,
      headers: {
        ...(options.body ? { 'Content-Type': 'application/json' } : {}),
        ...options.headers,
      },
    })
  } catch {
    throw new ApiError('Cannot connect to the service. Please try again.')
  }

  const contentType = response.headers.get('content-type') || ''
  const data = contentType.includes('application/json') ? await response.json() : null
  if (!response.ok) {
    throw new ApiError(
      data?.message || data?.title || 'The request could not be completed.',
      response.status,
      data?.errors || {},
      data || {},
    )
  }
  return data
}

const rememberSession = (session) => {
  accessToken = session?.token || null
  accessTokenExpiresAt = session?.expiresAt ? new Date(session.expiresAt).getTime() : null
  sessionListener(session || null)
  return session
}

export const setSessionListener = (listener) => {
  sessionListener = listener || (() => {})
}

export const login = async (credentials) => rememberSession(await request('/auth/login', {
  method: 'POST',
  body: JSON.stringify(credentials),
}))

export const register = async (account) => rememberSession(await request('/auth/register', {
  method: 'POST',
  body: JSON.stringify(account),
}))

export const refreshSession = async () => {
  if (!refreshPromise) {
    refreshPromise = request('/auth/refresh', { method: 'POST' })
      .then(rememberSession)
      .catch((error) => {
        rememberSession(null)
        throw error
      })
      .finally(() => { refreshPromise = null })
  }
  return refreshPromise
}

export const revokeSession = async () => {
  try {
    await request('/auth/revoke', { method: 'POST' })
  } finally {
    rememberSession(null)
  }
}

export const authenticatedRequest = async (path, options = {}) => {
  if (!accessToken || (accessTokenExpiresAt && accessTokenExpiresAt <= Date.now() + 30_000)) {
    await refreshSession()
  }

  const send = () => request(path, {
    ...options,
    headers: { ...options.headers, Authorization: `Bearer ${accessToken}` },
  })

  try {
    return await send()
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) throw error
    await refreshSession()
    return send()
  }
}

export const authenticatedServiceRequest = async (baseUrl, path, options = {}) => {
  if (!accessToken || (accessTokenExpiresAt && accessTokenExpiresAt <= Date.now() + 30_000)) await refreshSession()
  const send = async () => {
    const response = await fetch(`${baseUrl.replace(/\/$/, '')}${path}`, {
      ...options,
      headers: { ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...options.headers, Authorization: `Bearer ${accessToken}` },
    })
    const data = response.status === 204 ? null : await response.json().catch(() => null)
    if (!response.ok) throw new ApiError(data?.detail || data?.title || 'The request could not be completed.', response.status, data?.errors || {}, data || {})
    return data
  }
  try { return await send() } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) throw error
    await refreshSession()
    return send()
  }
}

export const getReviews = () => request('/reviews')
export const createReview = (review) => request('/reviews', {
  method: 'POST',
  body: JSON.stringify(review),
})
export const sendContactMessage = (message) => request('/contact', {
  method: 'POST',
  body: JSON.stringify(message),
})

export const getDashboard = (rolePath) => authenticatedRequest(`/dashboard/${rolePath}`)
export const createUser = (user) => authenticatedRequest('/users', {
  method: 'POST',
  body: JSON.stringify(user),
})
export const approveStaffId = (approval) => authenticatedRequest('/users/staff-invitations', {
  method: 'POST',
  body: JSON.stringify(approval),
})
export const getStaffInvitations = () => authenticatedRequest('/users/staff-invitations')
export const updateManagedUser = (userId, user) => authenticatedRequest(`/users/${userId}/managed`, {
  method: 'PUT',
  body: JSON.stringify(user),
})
export const setUserStatus = (userId, isActive) => authenticatedRequest(`/users/${userId}/status`, {
  method: 'PATCH',
  body: JSON.stringify({ isActive }),
})

export async function apiFetch(path, options = {}) {
  const token = localStorage.getItem('token')
  const headers = { 'Content-Type': 'application/json', ...options.headers }
  if (token) headers['Authorization'] = `Bearer ${token}`

  const res = await fetch(path, { ...options, headers })

  if (res.status === 204) return null

  let data
  try { data = await res.json() } catch { data = null }

  if (!res.ok) {
    throw new Error(data?.message ?? data?.title ?? `HTTP ${res.status}`)
  }
  return data
}

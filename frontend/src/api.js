const AUTH = '/auth'
const USERS = '/users'

export async function register(email, password) {
  const res = await fetch(`${AUTH}/api/registration/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.message ?? 'Registration failed')
  return data
}

export async function login(email, password) {
  const res = await fetch(`${AUTH}/api/login/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.message ?? 'Login failed')
  return data.token
}

export async function getMe(token) {
  const res = await fetch(`${AUTH}/api/login/me`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.message ?? 'Failed to fetch user')
  return data
}

export async function getUserProfile(userId, token) {
  const res = await fetch(`${USERS}/api/users/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  const data = await res.json()
  if (!res.ok) throw new Error(data.message ?? 'Failed to fetch profile')
  return data
}

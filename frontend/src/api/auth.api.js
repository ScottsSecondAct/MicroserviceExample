import { apiFetch } from './apiClient.js'

const BASE = '/auth/api'

export const authApi = {
  login: (email, password) =>
    apiFetch(`${BASE}/login/login`, {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  register: (email, password) =>
    apiFetch(`${BASE}/registration/register`, {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  me: () => apiFetch(`${BASE}/login/me`),
}

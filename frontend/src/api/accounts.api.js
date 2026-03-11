import { apiFetch } from './apiClient.js'

const BASE = '/accounts/api/accounts'

export const accountsApi = {
  list: () => apiFetch(BASE),
  get: (id) => apiFetch(`${BASE}/${id}`),
  create: (data) => apiFetch(BASE, { method: 'POST', body: JSON.stringify(data) }),
  update: (id, data) => apiFetch(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id) => apiFetch(`${BASE}/${id}`, { method: 'DELETE' }),
}

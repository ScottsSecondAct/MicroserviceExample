import { apiFetch } from './apiClient.js'

const BASE = '/activities/api/activities'

export const activitiesApi = {
  list: (params = {}) => {
    const entries = Object.entries(params).filter(([, v]) => v != null && v !== '')
    const qs = entries.length ? '?' + new URLSearchParams(entries).toString() : ''
    return apiFetch(`${BASE}${qs}`)
  },
  create: (data) => apiFetch(BASE, { method: 'POST', body: JSON.stringify(data) }),
  update: (id, data) => apiFetch(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id) => apiFetch(`${BASE}/${id}`, { method: 'DELETE' }),
}

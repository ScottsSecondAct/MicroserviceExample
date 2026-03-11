import { apiFetch } from './apiClient.js'

const BASE = '/deals/api/deals'
const PIPELINE = '/deals/api/pipeline'

export const dealsApi = {
  list: (params = {}) => {
    const entries = Object.entries(params).filter(([, v]) => v != null && v !== '')
    const qs = entries.length ? '?' + new URLSearchParams(entries).toString() : ''
    return apiFetch(`${BASE}${qs}`)
  },
  get: (id) => apiFetch(`${BASE}/${id}`),
  create: (data) => apiFetch(BASE, { method: 'POST', body: JSON.stringify(data) }),
  update: (id, data) => apiFetch(`${BASE}/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  delete: (id) => apiFetch(`${BASE}/${id}`, { method: 'DELETE' }),
  addContact: (dealId, data) => apiFetch(`${BASE}/${dealId}/contacts`, { method: 'POST', body: JSON.stringify(data) }),
  removeContact: (dealId, contactId) => apiFetch(`${BASE}/${dealId}/contacts/${contactId}`, { method: 'DELETE' }),
  getPipeline: () => apiFetch(`${PIPELINE}`),
}

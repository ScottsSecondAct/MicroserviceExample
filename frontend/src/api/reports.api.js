import { apiFetch } from './apiClient.js'

const BASE = '/reports/api/reports'

export const reportsApi = {
  pipeline: () => apiFetch(`${BASE}/pipeline`),
  activities: () => apiFetch(`${BASE}/activities`),
  contacts: () => apiFetch(`${BASE}/contacts`),
  dashboard: () => apiFetch(`${BASE}/dashboard`),
}

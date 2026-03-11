import { apiFetch } from './apiClient.js'

const BASE = '/users/api/users'

export const usersApi = {
  getProfile: (userId) => apiFetch(`${BASE}/${userId}`),
  getTeam: () => apiFetch(`${BASE}/team`),
}

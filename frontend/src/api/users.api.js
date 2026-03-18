import { apiFetch } from './apiClient.js'

const BASE = '/users/api/users'
const ADMIN_BASE = '/admin/api/admin'

export const usersApi = {
  getProfile: (userId) => apiFetch(`${BASE}/${userId}`),
  getTeam: () => apiFetch(`${BASE}/team`),
  getAuditLog: () => apiFetch(`${BASE}/audit`),
}

export const adminApi = {
  listUsers: () => apiFetch(`${ADMIN_BASE}/users`),
  updateRole: (userId, role) =>
    apiFetch(`${ADMIN_BASE}/users/${userId}/role`, {
      method: 'PUT',
      body: JSON.stringify({ role }),
    }),
  setActive: (userId, isActive) =>
    apiFetch(`${ADMIN_BASE}/users/${userId}/active`, {
      method: 'PUT',
      body: JSON.stringify({ isActive }),
    }),
  inviteUser: (email) =>
    apiFetch('/auth/api/users/invite', {
      method: 'POST',
      body: JSON.stringify({ email }),
    }),
  resendInvite: (userId) =>
    apiFetch(`${BASE}/${userId}/resend-invite`, {
      method: 'POST',
    }),
}

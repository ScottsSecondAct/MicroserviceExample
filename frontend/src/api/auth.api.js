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
  changePassword: (newPassword) =>
    apiFetch(`${BASE}/auth/change-password`, {
      method: 'POST',
      body: JSON.stringify({ newPassword }),
    }),
  forgotPassword: (email) =>
    apiFetch(`${BASE}/auth/forgot-password`, {
      method: 'POST',
      body: JSON.stringify({ email }),
    }),
  resetPassword: (token, newPassword) =>
    apiFetch(`${BASE}/auth/reset-password`, {
      method: 'POST',
      body: JSON.stringify({ token, newPassword }),
    }),
  acceptInvite: (token, password) =>
    apiFetch(`${BASE}/registration/accept-invite`, {
      method: 'POST',
      body: JSON.stringify({ token, password }),
    }),
}

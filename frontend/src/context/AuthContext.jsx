import { createContext, useContext, useState, useEffect, useRef } from 'react'
import { authApi } from '../api/auth.api.js'

const AuthContext = createContext(null)

const REFRESH_MARGIN_MS = 5 * 60 * 1000 // refresh 5 min before expiry

function parseToken(token) {
  if (!token) return null
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return {
      userId: payload['UserId'],
      email: payload['sub'],
      role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
      mustChangePassword: payload['MustChangePassword'] === 'true',
      exp: payload['exp'],
    }
  } catch {
    return null
  }
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem('token'))
  const refreshTimerRef = useRef(null)

  const user = parseToken(token)

  function scheduleRefresh(currentToken) {
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current)
    const payload = parseToken(currentToken)
    if (!payload?.exp) return
    const delay = payload.exp * 1000 - Date.now() - REFRESH_MARGIN_MS
    if (delay <= 0) return
    refreshTimerRef.current = setTimeout(async () => {
      const storedRefreshToken = localStorage.getItem('refreshToken')
      if (!storedRefreshToken) return
      try {
        const data = await authApi.refresh(storedRefreshToken)
        login(data.token, data.refreshToken)
      } catch {
        logout()
      }
    }, delay)
  }

  function login(newToken, newRefreshToken) {
    localStorage.setItem('token', newToken)
    if (newRefreshToken) localStorage.setItem('refreshToken', newRefreshToken)
    setToken(newToken)
    scheduleRefresh(newToken)
  }

  function logout() {
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current)
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    setToken(null)
  }

  useEffect(() => {
    if (token) scheduleRefresh(token)
    return () => {
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current)
    }
  }, [])

  return (
    <AuthContext.Provider value={{ token, user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  return useContext(AuthContext)
}

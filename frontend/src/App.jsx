import { useState } from 'react'
import Login from './pages/Login.jsx'
import Register from './pages/Register.jsx'
import Profile from './pages/Profile.jsx'

export default function App() {
  const [token, setToken] = useState(() => localStorage.getItem('token'))
  const [page, setPage] = useState('login')

  function handleLogin(newToken) {
    localStorage.setItem('token', newToken)
    setToken(newToken)
  }

  function handleLogout() {
    localStorage.removeItem('token')
    setToken(null)
    setPage('login')
  }

  if (token) {
    return <Profile token={token} onLogout={handleLogout} />
  }

  return (
    <div className="container">
      <nav>
        <button
          className={page === 'login' ? 'active' : ''}
          onClick={() => setPage('login')}
        >
          Login
        </button>
        <button
          className={page === 'register' ? 'active' : ''}
          onClick={() => setPage('register')}
        >
          Register
        </button>
      </nav>
      {page === 'login' ? (
        <Login onLogin={handleLogin} />
      ) : (
        <Register onRegistered={() => setPage('login')} />
      )}
    </div>
  )
}

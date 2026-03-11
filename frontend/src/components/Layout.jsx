import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <>
      <nav className="top-nav">
        <div className="nav-brand">CRM</div>
        <div className="nav-links">
          <NavLink to="/contacts">Contacts</NavLink>
          <NavLink to="/accounts">Accounts</NavLink>
          <NavLink to="/deals">Pipeline</NavLink>
          <NavLink to="/profile">Profile</NavLink>
        </div>
        <div className="nav-user">
          <span className="nav-email">{user?.email}</span>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </nav>
      <main className="main-content">
        <Outlet />
      </main>
    </>
  )
}

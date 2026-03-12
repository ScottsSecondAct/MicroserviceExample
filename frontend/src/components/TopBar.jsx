import { useState, useRef, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

function getInitials(email) {
  if (!email) return '?'
  const local = email.split('@')[0]
  const parts = local.split(/[._-]/)
  if (parts.length >= 2 && parts[0].length > 0 && parts[1].length > 0) {
    return (parts[0][0] + parts[1][0]).toUpperCase()
  }
  return local.slice(0, 2).toUpperCase()
}

const SearchIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <circle cx="11" cy="11" r="8" />
    <path d="M21 21l-4.35-4.35" />
  </svg>
)

const BellIcon = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
    <path d="M13.73 21a2 2 0 0 1-3.46 0" />
  </svg>
)

const ChevronIcon = () => (
  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M6 9l6 6 6-6" />
  </svg>
)

export default function TopBar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [dropdownOpen, setDropdownOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const dropdownRef = useRef(null)

  useEffect(() => {
    function handleClickOutside(e) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target)) {
        setDropdownOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function handleLogout() {
    setDropdownOpen(false)
    logout()
    navigate('/login')
  }

  function handleSearchSubmit(e) {
    e.preventDefault()
    // Global search — wire up to search results page when available
  }

  const initials = getInitials(user?.email)

  return (
    <header className="top-bar">
      <div className="top-bar-left">
        <form className="top-bar-search" onSubmit={handleSearchSubmit} role="search">
          <span className="top-bar-search-icon" aria-hidden="true">
            <SearchIcon />
          </span>
          <input
            type="search"
            className="top-bar-search-input"
            placeholder="Search contacts, accounts, deals…"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            aria-label="Global search"
          />
        </form>
      </div>

      <div className="top-bar-right">
        <button
          className="top-bar-icon-btn"
          title="Notifications"
          aria-label="Notifications"
        >
          <BellIcon />
          <span className="notif-badge" aria-label="3 notifications">3</span>
        </button>

        <div className="avatar-menu" ref={dropdownRef}>
          <button
            className="avatar-btn"
            onClick={() => setDropdownOpen(o => !o)}
            aria-haspopup="true"
            aria-expanded={dropdownOpen}
            title={user?.email}
          >
            <span className="avatar-initials">{initials}</span>
            <span className="avatar-chevron"><ChevronIcon /></span>
          </button>

          {dropdownOpen && (
            <div className="avatar-dropdown" role="menu">
              <div className="avatar-dropdown-header">
                <span className="avatar-initials avatar-initials-lg">{initials}</span>
                <div className="avatar-dropdown-info">
                  <span className="avatar-dropdown-email">{user?.email}</span>
                  {user?.role && (
                    <span className="avatar-dropdown-role">{user.role}</span>
                  )}
                </div>
              </div>
              <div className="avatar-dropdown-divider" />
              <Link
                to="/profile"
                className="avatar-dropdown-item"
                onClick={() => setDropdownOpen(false)}
                role="menuitem"
              >
                Profile settings
              </Link>
              <button
                className="avatar-dropdown-item avatar-dropdown-logout"
                onClick={handleLogout}
                role="menuitem"
              >
                Log out
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}

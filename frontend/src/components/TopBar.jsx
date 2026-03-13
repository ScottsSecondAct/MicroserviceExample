import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Search, Bell, ChevronDown, User, LogOut } from 'lucide-react'
import { useAuth } from '../context/AuthContext.jsx'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from './ui/dropdown-menu.jsx'

function getInitials(email) {
  if (!email) return '?'
  const local = email.split('@')[0]
  const parts = local.split(/[._-]/)
  if (parts.length >= 2 && parts[0].length > 0 && parts[1].length > 0) {
    return (parts[0][0] + parts[1][0]).toUpperCase()
  }
  return local.slice(0, 2).toUpperCase()
}

export default function TopBar() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [searchQuery, setSearchQuery] = useState('')

  function handleLogout() {
    logout()
    navigate('/login')
  }

  function handleSearchSubmit(e) {
    e.preventDefault()
    // Global search — wire up to search results page when available
  }

  const initials = getInitials(user?.email)

  return (
    <header className="h-14 sticky top-0 z-20 bg-white border-b border-gray-200 flex items-center justify-between px-5 gap-4 flex-shrink-0">
      {/* Search */}
      <div className="flex-1 max-w-md">
        <form className="relative flex items-center" onSubmit={handleSearchSubmit} role="search">
          <Search className="absolute left-2.5 h-4 w-4 text-gray-400 pointer-events-none" />
          <input
            type="search"
            className="w-full pl-9 pr-3 py-1.5 text-sm border border-gray-200 rounded-lg bg-gray-50 focus:outline-none focus:border-blue-500 focus:bg-white focus:ring-2 focus:ring-blue-500/10 transition-all"
            placeholder="Search contacts, accounts, deals…"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            aria-label="Global search"
          />
        </form>
      </div>

      {/* Right side */}
      <div className="flex items-center gap-2">
        {/* Notifications */}
        <button
          className="relative text-gray-500 hover:text-gray-900 hover:bg-gray-100 p-2 rounded-lg transition-colors"
          title="Notifications"
          aria-label="Notifications"
        >
          <Bell size={18} />
          <span className="absolute top-1 right-1 min-w-[14px] h-3.5 bg-red-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center px-0.5 leading-none">
            3
          </span>
        </button>

        {/* User menu — shadcn DropdownMenu */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              className="flex items-center gap-1.5 px-1.5 py-1 rounded-lg hover:bg-gray-100 transition-colors"
              title={user?.email}
            >
              <span className="w-8 h-8 rounded-full bg-blue-600 text-white text-xs font-bold flex items-center justify-center select-none">
                {initials}
              </span>
              <ChevronDown size={12} className="text-gray-400" />
            </button>
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end" className="w-56">
            {/* User info header */}
            <div className="flex items-center gap-3 px-3 py-2.5 bg-gray-50 rounded-t-[inherit]">
              <span className="w-9 h-9 rounded-full bg-blue-600 text-white text-sm font-bold flex items-center justify-center flex-shrink-0 select-none">
                {initials}
              </span>
              <div className="flex flex-col min-w-0">
                <span className="text-sm font-medium text-gray-900 truncate">{user?.email}</span>
                {user?.role && (
                  <span className="text-xs text-gray-500 capitalize">{user.role}</span>
                )}
              </div>
            </div>
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild>
              <Link
                to="/profile"
                className="flex items-center gap-2 cursor-pointer no-underline text-gray-700 hover:no-underline"
              >
                <User size={14} />
                Profile settings
              </Link>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="flex items-center gap-2 text-red-600 focus:text-red-600 focus:bg-red-50 cursor-pointer"
              onClick={handleLogout}
            >
              <LogOut size={14} />
              Log out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  )
}

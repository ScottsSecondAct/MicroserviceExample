import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Users, Building2, BarChart3, CheckSquare, LayoutDashboard, ChevronLeft, ChevronRight, ShieldCheck } from 'lucide-react'
import TopBar from './TopBar.jsx'
import { useAuth } from '../context/AuthContext.jsx'
import { cn } from '../lib/utils'

const navGroups = [
  {
    label: 'CRM',
    items: [
      { to: '/contacts', label: 'Contacts', Icon: Users },
      { to: '/accounts', label: 'Accounts', Icon: Building2 },
      { to: '/deals', label: 'Pipeline', Icon: BarChart3 },
    ],
  },
  {
    label: 'Productivity',
    items: [
      { to: '/activities/tasks', label: 'Tasks', Icon: CheckSquare },
    ],
  },
  {
    label: 'Insights',
    items: [
      { to: '/dashboard', label: 'Dashboard', Icon: LayoutDashboard },
    ],
  },
]

const adminNavGroup = {
  label: 'Admin',
  items: [
    { to: '/admin/users', label: 'Users', Icon: ShieldCheck },
  ],
}

export default function Layout() {
  const [collapsed, setCollapsed] = useState(false)
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  return (
    <div className="flex flex-col min-h-screen">
      <TopBar />

      <div className="flex flex-1 min-h-0">
        {/* Sidebar */}
        <aside
          className={cn(
            'flex flex-col flex-shrink-0 bg-sidebar text-sidebar-foreground transition-[width] duration-200 ease-in-out sticky top-14 h-[calc(100vh-56px)] z-10 overflow-hidden',
            collapsed ? 'w-16' : 'w-60'
          )}
        >
          {/* Sidebar header */}
          <div
            className={cn(
              'flex items-center h-14 border-b border-sidebar-border flex-shrink-0 px-4',
              collapsed ? 'justify-center' : 'justify-between'
            )}
          >
            {!collapsed && (
              <span className="text-lg font-bold tracking-wide text-white select-none">CRM</span>
            )}
            <button
              className="text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent rounded p-1 flex items-center transition-colors"
              onClick={() => setCollapsed(c => !c)}
              title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            >
              {collapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
            </button>
          </div>

          {/* Nav */}
          <nav className="flex-1 overflow-y-auto py-3">
            {[...navGroups, ...(isAdmin ? [adminNavGroup] : [])].map(group => (
              <div key={group.label} className="mb-1">
                {!collapsed && (
                  <div className="px-4 pt-2 pb-1 text-[11px] font-semibold uppercase tracking-widest text-sidebar-muted select-none">
                    {group.label}
                  </div>
                )}
                {group.items.map(({ to, label, Icon }) => (
                  <NavLink
                    key={to}
                    to={to}
                    title={collapsed ? label : undefined}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-2.5 py-2 text-sm font-medium text-slate-400 hover:bg-slate-700 hover:text-slate-100 transition-colors no-underline',
                        collapsed ? 'justify-center px-0' : 'px-4',
                        isActive && 'bg-blue-600 text-white hover:bg-blue-600/90 hover:text-white'
                      )
                    }
                  >
                    <Icon size={18} className="flex-shrink-0" />
                    {!collapsed && <span>{label}</span>}
                  </NavLink>
                ))}
              </div>
            ))}
          </nav>
        </aside>

        {/* Main content */}
        <main className="flex-1 min-w-0 p-6 bg-gray-50">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

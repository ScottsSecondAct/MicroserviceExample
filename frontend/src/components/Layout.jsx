import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { Users, Building2, BarChart3, CheckSquare, LayoutDashboard, ChevronLeft, ChevronRight, ShieldCheck, X } from 'lucide-react'
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
  const [mobileOpen, setMobileOpen] = useState(false)
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  return (
    <div className="flex flex-col min-h-screen">
      <TopBar onMenuClick={() => setMobileOpen(o => !o)} />

      <div className="flex flex-1 min-h-0">
        {/* Mobile backdrop overlay */}
        {mobileOpen && (
          <div
            className="fixed inset-0 z-30 bg-black/50 md:hidden"
            onClick={() => setMobileOpen(false)}
            aria-hidden="true"
          />
        )}

        {/* Sidebar — fixed drawer on mobile, sticky inline on desktop */}
        <aside
          className={cn(
            'flex flex-col bg-sidebar text-sidebar-foreground overflow-hidden',
            // Mobile: fixed full-height drawer, slides in from left
            'fixed top-0 bottom-0 left-0 z-40 w-72',
            'transition-transform duration-300 ease-in-out',
            mobileOpen ? 'translate-x-0' : '-translate-x-full',
            // Desktop (md+): inline sticky sidebar, reset mobile overrides
            'md:sticky md:top-14 md:bottom-auto md:z-10 md:flex-shrink-0',
            'md:h-[calc(100vh-56px)] md:translate-x-0',
            'md:transition-[width] md:duration-200 md:ease-in-out',
            collapsed ? 'md:w-16' : 'md:w-60',
          )}
        >
          {/* Sidebar header */}
          <div
            className={cn(
              'flex items-center h-14 border-b border-sidebar-border flex-shrink-0 px-4 justify-between',
              collapsed && 'md:justify-center',
            )}
          >
            {/* Logo — always visible on mobile; hidden on desktop when collapsed */}
            <span
              className={cn(
                'text-lg font-bold tracking-wide text-white select-none',
                collapsed && 'md:hidden',
              )}
            >
              CRM
            </span>

            {/* Desktop: collapse/expand toggle */}
            <button
              className="hidden md:flex text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent rounded p-1 items-center transition-colors"
              onClick={() => setCollapsed(c => !c)}
              title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            >
              {collapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
            </button>

            {/* Mobile: close drawer button */}
            <button
              className="flex md:hidden text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent rounded p-1 items-center transition-colors"
              onClick={() => setMobileOpen(false)}
              title="Close menu"
            >
              <X size={16} />
            </button>
          </div>

          {/* Nav */}
          <nav className="flex-1 overflow-y-auto py-3">
            {[...navGroups, ...(isAdmin ? [adminNavGroup] : [])].map(group => (
              <div key={group.label} className="mb-1">
                {/* Group label: always show on mobile; hide on desktop when collapsed */}
                <div
                  className={cn(
                    'px-4 pt-2 pb-1 text-[11px] font-semibold uppercase tracking-widest text-sidebar-muted select-none',
                    collapsed ? 'md:hidden' : '',
                  )}
                >
                  {group.label}
                </div>
                {group.items.map(({ to, label, Icon }) => (
                  <NavLink
                    key={to}
                    to={to}
                    title={collapsed ? label : undefined}
                    onClick={() => setMobileOpen(false)}
                    className={({ isActive }) =>
                      cn(
                        'flex items-center gap-2.5 py-2 text-sm font-medium text-slate-400 hover:bg-slate-700 hover:text-slate-100 transition-colors no-underline',
                        // Mobile: always padded; Desktop: icon-only when collapsed
                        collapsed ? 'px-4 md:justify-center md:px-0' : 'px-4',
                        isActive && 'bg-blue-600 text-white hover:bg-blue-600/90 hover:text-white',
                      )
                    }
                  >
                    <Icon size={18} className="flex-shrink-0" />
                    {/* Label: always show on mobile; hide on desktop when collapsed */}
                    <span className={cn(collapsed && 'md:hidden')}>{label}</span>
                  </NavLink>
                ))}
              </div>
            ))}
          </nav>
        </aside>

        {/* Main content */}
        <main className="flex-1 min-w-0 p-4 md:p-6 bg-gray-50 overflow-x-hidden">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

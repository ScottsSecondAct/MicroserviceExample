import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import TopBar from './TopBar.jsx'

const icons = {
  contacts: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
      <circle cx="9" cy="7" r="4"/>
      <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
      <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
    </svg>
  ),
  accounts: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="7" width="20" height="14" rx="2" ry="2"/>
      <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>
    </svg>
  ),
  pipeline: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="3" width="5" height="18" rx="1"/>
      <rect x="9.5" y="8" width="5" height="13" rx="1"/>
      <rect x="17" y="5" width="5" height="16" rx="1"/>
    </svg>
  ),
  tasks: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 11l3 3L22 4"/>
      <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>
    </svg>
  ),
  dashboard: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" rx="1"/>
      <rect x="14" y="3" width="7" height="7" rx="1"/>
      <rect x="14" y="14" width="7" height="7" rx="1"/>
      <rect x="3" y="14" width="7" height="7" rx="1"/>
    </svg>
  ),
  collapse: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M15 18l-6-6 6-6"/>
    </svg>
  ),
  expand: (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 18l6-6-6-6"/>
    </svg>
  ),
}

const navGroups = [
  {
    label: 'CRM',
    items: [
      { to: '/contacts', label: 'Contacts', icon: 'contacts' },
      { to: '/accounts', label: 'Accounts', icon: 'accounts' },
      { to: '/deals', label: 'Pipeline', icon: 'pipeline' },
    ],
  },
  {
    label: 'Productivity',
    items: [
      { to: '/activities/tasks', label: 'Tasks', icon: 'tasks' },
    ],
  },
  {
    label: 'Insights',
    items: [
      { to: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    ],
  },
]

export default function Layout() {
  const [collapsed, setCollapsed] = useState(false)

  return (
    <div className={`app-shell${collapsed ? ' sidebar-collapsed' : ''}`}>
      <TopBar />

      <div className="app-body">
        <aside className="left-sidebar">
          <div className="sidebar-header">
            {!collapsed && <div className="nav-brand">CRM</div>}
            <button
              className="sidebar-toggle"
              onClick={() => setCollapsed(c => !c)}
              title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
            >
              {collapsed ? icons.expand : icons.collapse}
            </button>
          </div>

          <nav className="sidebar-nav">
            {navGroups.map(group => (
              <div key={group.label} className="nav-group">
                {!collapsed && <div className="nav-group-label">{group.label}</div>}
                {group.items.map(item => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    className="sidebar-link"
                    title={collapsed ? item.label : undefined}
                  >
                    <span className="sidebar-icon">{icons[item.icon]}</span>
                    {!collapsed && <span className="sidebar-label">{item.label}</span>}
                  </NavLink>
                ))}
              </div>
            ))}
          </nav>
        </aside>

        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

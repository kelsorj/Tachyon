import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'

function Layout({ children }) {
  const location = useLocation()
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  const isActive = (path) => {
    return location.pathname === path || location.pathname.startsWith(path + '/')
  }

  const navItems = [
    { path: '/devices', icon: '🤖', label: 'Devices' },
    { path: '/labware', icon: '🧫', label: 'Labware' },
    { path: '/workflows', icon: '⚡', label: 'Workflows' },
    { path: '/tools', icon: '🛠️', label: 'Tools' },
  ]

  return (
    <div style={{ display: 'flex', height: '100vh', background: '#0a0a0a' }}>
      {/* Sidebar Navigation */}
      <div style={{
        width: sidebarCollapsed ? 60 : 250,
        background: '#1a1a2e',
        borderRight: '2px solid #333',
        display: 'flex',
        flexDirection: 'column',
        transition: 'width 0.2s ease',
        overflow: 'hidden',
      }}>
        {/* Header with collapse button */}
        <div style={{ 
          padding: sidebarCollapsed ? '20px 10px' : 20, 
          display: 'flex', 
          alignItems: 'center',
          justifyContent: sidebarCollapsed ? 'center' : 'space-between',
          minHeight: 70,
        }}>
          {!sidebarCollapsed && (
            <h1 style={{ fontSize: '1.5em', margin: 0, color: '#fff', whiteSpace: 'nowrap' }}>Tachyon</h1>
          )}
          <button
            onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
            style={{
              background: 'none',
              border: 'none',
              color: '#888',
              cursor: 'pointer',
              fontSize: '1.2em',
              padding: 5,
              borderRadius: 4,
              transition: 'color 0.2s',
            }}
            onMouseEnter={(e) => e.currentTarget.style.color = '#fff'}
            onMouseLeave={(e) => e.currentTarget.style.color = '#888'}
            title={sidebarCollapsed ? 'Expand menu' : 'Collapse menu'}
          >
            {sidebarCollapsed ? '☰' : '◀'}
          </button>
        </div>

        <nav style={{ display: 'flex', flexDirection: 'column', gap: 8, padding: sidebarCollapsed ? '0 8px' : '0 20px' }}>
          {navItems.map(({ path, icon, label }) => (
            <Link
              key={path}
              to={path}
              style={{
                padding: sidebarCollapsed ? '12px 0' : '12px 16px',
                borderRadius: 6,
                textDecoration: 'none',
                color: isActive(path) ? '#fff' : '#aaa',
                background: isActive(path) ? '#1890ff' : 'transparent',
                fontWeight: isActive(path) ? 'bold' : 'normal',
                transition: 'all 0.2s',
                display: 'flex',
                alignItems: 'center',
                justifyContent: sidebarCollapsed ? 'center' : 'flex-start',
                gap: 10,
              }}
              onMouseEnter={(e) => {
                if (!isActive(path)) {
                  e.currentTarget.style.background = '#222'
                  e.currentTarget.style.color = '#fff'
                }
              }}
              onMouseLeave={(e) => {
                if (!isActive(path)) {
                  e.currentTarget.style.background = 'transparent'
                  e.currentTarget.style.color = '#aaa'
                }
              }}
              title={sidebarCollapsed ? label : undefined}
            >
              <span style={{ fontSize: '1.2em' }}>{icon}</span>
              {!sidebarCollapsed && <span>{label}</span>}
            </Link>
          ))}
        </nav>

        {!sidebarCollapsed && location.pathname.includes('/devices/') && (
          <div style={{ 
            margin: '30px 20px 20px',
            padding: 15, 
            background: '#222', 
            borderRadius: 6,
            border: '1px solid #333'
          }}>
            <div style={{ fontSize: '0.85em', color: '#888', marginBottom: 8 }}>
              Current Device
            </div>
            <div style={{ fontSize: '0.9em', color: '#fff', fontWeight: 'bold' }}>
              {decodeURIComponent(location.pathname.split('/')[2] || 'Device')}
            </div>
            <Link
              to="/devices"
              style={{
                display: 'block',
                marginTop: 10,
                padding: '8px 12px',
                borderRadius: 4,
                background: '#1890ff',
                color: '#fff',
                textDecoration: 'none',
                textAlign: 'center',
                fontSize: '0.85em',
                fontWeight: 'bold'
              }}
            >
              ← Back to Devices
            </Link>
          </div>
        )}
      </div>

      {/* Main Content */}
      <div style={{ flex: 1, overflow: 'auto', background: '#0a0a0a' }}>
        {children}
      </div>
    </div>
  )
}

export default Layout

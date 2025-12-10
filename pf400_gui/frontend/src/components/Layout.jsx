import { Link, useLocation } from 'react-router-dom'

function Layout({ children }) {
  const location = useLocation()

  const isActive = (path) => {
    return location.pathname === path || location.pathname.startsWith(path + '/')
  }

  return (
    <div style={{ display: 'flex', height: '100vh', background: '#0a0a0a' }}>
      {/* Sidebar Navigation */}
      <div style={{
        width: 250,
        background: '#1a1a2e',
        borderRight: '2px solid #333',
        display: 'flex',
        flexDirection: 'column',
        padding: 20
      }}>
        <div style={{ marginBottom: 30 }}>
          <h1 style={{ fontSize: '1.5em', margin: 0, color: '#fff' }}>Tachyon</h1>
          <div style={{ fontSize: '0.85em', color: '#888', marginTop: 5 }}>
            Device Management
          </div>
        </div>

        <nav style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          <Link
            to="/"
            style={{
              padding: '12px 16px',
              borderRadius: 6,
              textDecoration: 'none',
              color: isActive('/') && !location.pathname.includes('/devices/') ? '#fff' : '#aaa',
              background: isActive('/') && !location.pathname.includes('/devices/') ? '#1890ff' : 'transparent',
              fontWeight: isActive('/') && !location.pathname.includes('/devices/') ? 'bold' : 'normal',
              transition: 'all 0.2s',
              display: 'flex',
              alignItems: 'center',
              gap: 10
            }}
            onMouseEnter={(e) => {
              if (!isActive('/') || location.pathname.includes('/devices/')) {
                e.currentTarget.style.background = '#222'
                e.currentTarget.style.color = '#fff'
              }
            }}
            onMouseLeave={(e) => {
              if (!isActive('/') || location.pathname.includes('/devices/')) {
                e.currentTarget.style.background = 'transparent'
                e.currentTarget.style.color = '#aaa'
              }
            }}
          >
            <span style={{ fontSize: '1.2em' }}>🏠</span>
            <span>Dashboard</span>
          </Link>
        </nav>

        {location.pathname.includes('/devices/') && (
          <div style={{ 
            marginTop: 30, 
            padding: 15, 
            background: '#222', 
            borderRadius: 6,
            border: '1px solid #333'
          }}>
            <div style={{ fontSize: '0.85em', color: '#888', marginBottom: 8 }}>
              Current Device
            </div>
            <div style={{ fontSize: '0.9em', color: '#fff', fontWeight: 'bold' }}>
              {location.pathname.split('/')[2] || 'Device'}
            </div>
            <Link
              to="/"
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
              ← Back to Dashboard
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


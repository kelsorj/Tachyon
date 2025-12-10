import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'

const API_URL = "http://localhost:3061"

function DeviceDashboard() {
  const [devices, setDevices] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    fetchDevices()
  }, [])

  const fetchDevices = async () => {
    try {
      setLoading(true)
      const res = await fetch(`${API_URL}/devices`)
      const data = await res.json()
      if (res.ok) {
        setDevices(data.devices || [])
        setError(null)
      } else {
        setError(data.detail || 'Failed to fetch devices')
      }
    } catch (e) {
      setError(`Error: ${e.message}`)
    } finally {
      setLoading(false)
    }
  }

  const getDeviceTypeIcon = (deviceTypeId) => {
    // You can customize icons based on device type
    // For now, using a generic robot icon for PF400
    return '🤖'
  }

  const getStatusColor = (status) => {
    switch (status?.toLowerCase()) {
      case 'active':
        return '#52c41a'
      case 'offline':
        return '#ff4d4f'
      case 'error':
        return '#ff7875'
      default:
        return '#888'
    }
  }

  const getDeviceRoute = (device) => {
    // Determine the route based on device type or name
    // For PF400 devices, route to the PF400 diagnostics
    if (device.name?.startsWith('PF400') || device.device_type_id?.includes('pf400')) {
      return `/devices/${device.name}/diagnostics`
    }
    // For other device types, you can add more routes here
    return `/devices/${device.name}`
  }

  if (loading) {
    return (
      <div style={{ padding: 40, textAlign: 'center' }}>
        <div style={{ fontSize: '1.2em', color: '#888' }}>Loading devices...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div style={{ padding: 40, textAlign: 'center' }}>
        <div style={{ fontSize: '1.2em', color: '#ff4d4f', marginBottom: 20 }}>Error: {error}</div>
        <button 
          onClick={fetchDevices}
          style={{
            padding: '10px 20px',
            borderRadius: 4,
            background: '#1890ff',
            color: '#fff',
            border: 'none',
            cursor: 'pointer',
            fontSize: '1em'
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 30 }}>
        <h1 style={{ fontSize: '2em', marginBottom: 10 }}>Tachyon Device Dashboard</h1>
        <p style={{ color: '#888', fontSize: '1.1em' }}>
          Select a device to access its diagnostic interface
        </p>
      </div>

      {devices.length === 0 ? (
        <div style={{ 
          padding: 60, 
          textAlign: 'center', 
          background: '#1a1a2e', 
          borderRadius: 8,
          border: '1px solid #333'
        }}>
          <div style={{ fontSize: '3em', marginBottom: 20 }}>📭</div>
          <div style={{ fontSize: '1.2em', color: '#888', marginBottom: 10 }}>
            No devices found
          </div>
          <div style={{ color: '#666' }}>
            Devices will appear here once they are registered in the system
          </div>
        </div>
      ) : (
        <div style={{ 
          display: 'grid', 
          gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', 
          gap: 20 
        }}>
          {devices.map(device => (
            <Link
              key={device._id || device.name}
              to={getDeviceRoute(device)}
              style={{
                textDecoration: 'none',
                color: 'inherit',
                display: 'block'
              }}
            >
              <div style={{
                background: '#1a1a2e',
                borderRadius: 8,
                padding: 20,
                border: '2px solid #333',
                cursor: 'pointer',
                transition: 'all 0.2s',
                height: '100%',
                display: 'flex',
                flexDirection: 'column'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.borderColor = '#1890ff'
                e.currentTarget.style.transform = 'translateY(-2px)'
                e.currentTarget.style.boxShadow = '0 4px 12px rgba(24, 144, 255, 0.3)'
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.borderColor = '#333'
                e.currentTarget.style.transform = 'translateY(0)'
                e.currentTarget.style.boxShadow = 'none'
              }}
              >
                <div style={{ display: 'flex', alignItems: 'center', marginBottom: 15 }}>
                  <div style={{ fontSize: '2.5em', marginRight: 15 }}>
                    {getDeviceTypeIcon(device.device_type_id)}
                  </div>
                  <div style={{ flex: 1 }}>
                    <div style={{ fontSize: '1.3em', fontWeight: 'bold', marginBottom: 5 }}>
                      {device.name || 'Unnamed Device'}
                    </div>
                    <div style={{ 
                      display: 'inline-block',
                      padding: '4px 12px',
                      borderRadius: 12,
                      fontSize: '0.85em',
                      fontWeight: 'bold',
                      background: getStatusColor(device.status),
                      color: '#fff'
                    }}>
                      {device.status || 'Unknown'}
                    </div>
                  </div>
                </div>

                {device.serial_number && (
                  <div style={{ color: '#888', fontSize: '0.9em', marginBottom: 8 }}>
                    <strong>Serial:</strong> {device.serial_number}
                  </div>
                )}

                {device.notes && (
                  <div style={{ color: '#aaa', fontSize: '0.9em', marginBottom: 8, flex: 1 }}>
                    {device.notes}
                  </div>
                )}

                {device.connection && (
                  <div style={{ 
                    marginTop: 'auto',
                    paddingTop: 15,
                    borderTop: '1px solid #333',
                    fontSize: '0.85em',
                    color: '#666'
                  }}>
                    <div><strong>IP:</strong> {device.connection.ip || 'N/A'}</div>
                    {device.connection.port && (
                      <div><strong>Port:</strong> {device.connection.port}</div>
                    )}
                  </div>
                )}

                <div style={{ 
                  marginTop: 15,
                  paddingTop: 15,
                  borderTop: '1px solid #333',
                  textAlign: 'right',
                  color: '#1890ff',
                  fontWeight: 'bold'
                }}>
                  View Diagnostics →
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      <div style={{ marginTop: 30, padding: 20, background: '#1a1a2e', borderRadius: 8, border: '1px solid #333' }}>
        <div style={{ fontSize: '0.9em', color: '#888' }}>
          <strong>Total Devices:</strong> {devices.length} | 
          <strong> Active:</strong> {devices.filter(d => d.status === 'active').length} | 
          <strong> Offline:</strong> {devices.filter(d => d.status === 'offline').length}
        </div>
      </div>
    </div>
  )
}

export default DeviceDashboard


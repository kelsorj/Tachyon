import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'

const API_URL = "http://localhost:3061"

function DeviceDashboard() {
  const [devices, setDevices] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [typeFilter, setTypeFilter] = useState('all')
  const [sortBy, setSortBy] = useState('name')
  const [sortOrder, setSortOrder] = useState('asc')

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

  const getDeviceTypeIcon = (device) => {
    // Customize icons based on device type or name
    if (device.name?.toLowerCase().includes('planar') || 
        device.product_name?.toLowerCase().includes('planar')) {
      return '🔄'
    }
    // Default robot icon for PF400 and other robots
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

  // Filter, search, and sort devices
  const filteredAndSortedDevices = devices
    .filter(device => {
      // Search filter - check device name and notes
      const matchesSearch = !searchTerm ||
        device.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        device.notes?.toLowerCase().includes(searchTerm.toLowerCase()) ||
        device.serial_number?.toLowerCase().includes(searchTerm.toLowerCase())

      // Status filter
      const matchesStatus = statusFilter === 'all' || device.status === statusFilter

      // Type filter
      let matchesType = typeFilter === 'all'
      if (!matchesType) {
        switch (typeFilter) {
          case 'robot':
            matchesType = device.name?.startsWith('PF400') ||
                         device.device_type_id?.includes('pf400') ||
                         device.product_name?.toLowerCase().includes('robot')
            break
          case 'planar':
            matchesType = device.name?.toLowerCase().includes('planar') ||
                         device.product_name?.toLowerCase().includes('planar') ||
                         device.vendor?.toLowerCase().includes('planar')
            break
          case 'other':
            matchesType = !device.name?.startsWith('PF400') &&
                         !device.device_type_id?.includes('pf400') &&
                         !device.name?.toLowerCase().includes('planar') &&
                         !device.product_name?.toLowerCase().includes('planar') &&
                         !device.vendor?.toLowerCase().includes('planar') &&
                         !device.product_name?.toLowerCase().includes('robot')
            break
        }
      }

      return matchesSearch && matchesStatus && matchesType
    })
    .sort((a, b) => {
      let aValue, bValue

      switch (sortBy) {
        case 'name':
          aValue = a.name || ''
          bValue = b.name || ''
          break
        case 'status':
          aValue = a.status || ''
          bValue = b.status || ''
          break
        case 'serial':
          aValue = a.serial_number || ''
          bValue = b.serial_number || ''
          break
        case 'type':
          // Sort by device type category
          const getTypeCategory = (device) => {
            if (device.name?.startsWith('PF400') || device.device_type_id?.includes('pf400')) return 'robot'
            if (device.name?.toLowerCase().includes('planar') ||
                device.product_name?.toLowerCase().includes('planar')) return 'planar'
            return 'other'
          }
          aValue = getTypeCategory(a)
          bValue = getTypeCategory(b)
          break
        default:
          aValue = a.name || ''
          bValue = b.name || ''
      }

      // Handle numeric sorting for names with numbers (like PF400-001, PF400-002)
      const aNumMatch = aValue.match(/(\d+)$/)
      const bNumMatch = bValue.match(/(\d+)$/)

      if (aNumMatch && bNumMatch && aValue.replace(/\d+$/, '') === bValue.replace(/\d+$/, '')) {
        // Same base name, sort by number
        const aNum = parseInt(aNumMatch[1])
        const bNum = parseInt(bNumMatch[1])
        return sortOrder === 'asc' ? aNum - bNum : bNum - aNum
      }

      // String comparison
      const comparison = aValue.localeCompare(bValue, undefined, { numeric: true, sensitivity: 'base' })
      return sortOrder === 'asc' ? comparison : -comparison
    })

  const getDeviceRoute = (device) => {
    // Determine the route based on device type or name
    // For PF400 devices, route to the PF400 diagnostics
    if (device.name?.startsWith('PF400') || device.device_type_id?.includes('pf400')) {
      return `/devices/${device.name}/diagnostics`
    }
    // For Planar Motor devices
    if (device.name?.toLowerCase().includes('planar') ||
        device.product_name?.toLowerCase().includes('planar') ||
        device.vendor?.toLowerCase().includes('planar')) {
      return `/devices/${device.name}/diagnostics`
    }
    // For other device types, you can add more routes here
    return `/devices/${device.name}/diagnostics`
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
        <p style={{ color: '#888', fontSize: '1.1em', marginBottom: 20 }}>
          Select a device to access its diagnostic interface
        </p>

        {/* Search, Filter, and Sort Controls */}
        <div style={{
          display: 'flex',
          gap: 15,
          alignItems: 'center',
          flexWrap: 'wrap',
          marginBottom: 20,
          padding: 15,
          background: '#1a1a2e',
          borderRadius: 8,
          border: '1px solid #333'
        }}>
          {/* Search Input */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ color: '#ccc', fontSize: '0.9em', fontWeight: 'bold' }}>🔍</span>
            <input
              type="text"
              placeholder="Search devices..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{
                padding: '8px 12px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#2a2a3e',
                color: '#fff',
                fontSize: '0.9em',
                minWidth: 200,
                outline: 'none'
              }}
            />
          </div>

          {/* Sort By */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ color: '#ccc', fontSize: '0.9em', fontWeight: 'bold' }}>Sort:</span>
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value)}
              style={{
                padding: '8px 12px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#2a2a3e',
                color: '#fff',
                fontSize: '0.9em',
                cursor: 'pointer',
                outline: 'none'
              }}
            >
              <option value="name">Name</option>
              <option value="status">Status</option>
              <option value="serial">Serial</option>
              <option value="type">Type</option>
            </select>
            <button
              onClick={() => setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')}
              style={{
                padding: '8px 12px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#2a2a3e',
                color: '#fff',
                fontSize: '0.9em',
                cursor: 'pointer',
                outline: 'none',
                minWidth: '40px'
              }}
              title={`Sort ${sortOrder === 'asc' ? 'descending' : 'ascending'}`}
            >
              {sortOrder === 'asc' ? '↑' : '↓'}
            </button>
          </div>

          {/* Status Filter */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ color: '#ccc', fontSize: '0.9em', fontWeight: 'bold' }}>Status:</span>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              style={{
                padding: '8px 12px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#2a2a3e',
                color: '#fff',
                fontSize: '0.9em',
                cursor: 'pointer',
                outline: 'none'
              }}
            >
              <option value="all">All Statuses</option>
              <option value="active">Active</option>
              <option value="offline">Offline</option>
              <option value="error">Error</option>
            </select>
          </div>

          {/* Type Filter */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{ color: '#ccc', fontSize: '0.9em', fontWeight: 'bold' }}>Type:</span>
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              style={{
                padding: '8px 12px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#2a2a3e',
                color: '#fff',
                fontSize: '0.9em',
                cursor: 'pointer',
                outline: 'none'
              }}
            >
              <option value="all">All Types</option>
              <option value="robot">Robots</option>
              <option value="planar">Planar Motors</option>
              <option value="other">Other</option>
            </select>
          </div>

          {/* Clear Filters */}
          {(searchTerm || statusFilter !== 'all' || typeFilter !== 'all') && (
            <button
              onClick={() => {
                setSearchTerm('')
                setStatusFilter('all')
                setTypeFilter('all')
                setSortBy('name')
                setSortOrder('asc')
              }}
              style={{
                padding: '8px 16px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#ff4d4f',
                color: '#fff',
                fontSize: '0.9em',
                cursor: 'pointer',
                outline: 'none'
              }}
            >
              Clear All
            </button>
          )}
        </div>
      </div>

      {filteredAndSortedDevices.length === 0 ? (
        <div style={{
          padding: 60,
          textAlign: 'center',
          background: '#1a1a2e',
          borderRadius: 8,
          border: '1px solid #333'
        }}>
          <div style={{ fontSize: '3em', marginBottom: 20 }}>🔍</div>
          <div style={{ fontSize: '1.2em', color: '#888', marginBottom: 10 }}>
            {devices.length === 0 ? 'No devices found' : 'No devices match your search'}
          </div>
          <div style={{ color: '#666' }}>
            {devices.length === 0
              ? 'Devices will appear here once they are registered in the system'
              : 'Try adjusting your search or filter criteria'
            }
          </div>
        </div>
      ) : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
          gap: 20
        }}>
          {filteredAndSortedDevices.map(device => (
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
                    {getDeviceTypeIcon(device)}
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
        <div style={{ fontSize: '0.9em', color: '#888', marginBottom: 8 }}>
          <strong>Total Devices:</strong> {devices.length} |
          <strong> Active:</strong> {devices.filter(d => d.status === 'active').length} |
          <strong> Offline:</strong> {devices.filter(d => d.status === 'offline').length}
        </div>
        {(searchTerm || statusFilter !== 'all' || typeFilter !== 'all' || sortBy !== 'name' || sortOrder !== 'asc') && (
          <div style={{ fontSize: '0.9em', color: '#ccc', borderTop: '1px solid #333', paddingTop: 8 }}>
            <strong>Filtered & Sorted Results:</strong> {filteredAndSortedDevices.length} |
            <strong> Active:</strong> {filteredAndSortedDevices.filter(d => d.status === 'active').length} |
            <strong> Offline:</strong> {filteredAndSortedDevices.filter(d => d.status === 'offline').length}
          </div>
        )}
      </div>
    </div>
  )
}

export default DeviceDashboard


import { useState, useEffect } from 'react'
import { Link, useSearchParams } from 'react-router-dom'

const DEFAULT_API_URL = `${window.location.protocol}//${window.location.hostname}:8091`
const ENV_API_URL = import.meta.env.VITE_API_URL
// If VITE_API_URL is set to localhost but we're not browsing from localhost, ignore it.
const API_URL = (ENV_API_URL && !(ENV_API_URL.includes('localhost') && window.location.hostname !== 'localhost'))
  ? ENV_API_URL
  : DEFAULT_API_URL

function DeviceDashboard() {
  const [searchParams, setSearchParams] = useSearchParams()
  
  const [devices, setDevices] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Initialize filter state from URL params
  const [searchTerm, setSearchTerm] = useState(searchParams.get('search') || '')
  const [statusFilter, setStatusFilter] = useState(searchParams.get('status') || 'all')
  const [typeFilter, setTypeFilter] = useState(searchParams.get('type') || '')
  const [typeDropdownOpen, setTypeDropdownOpen] = useState(false)
  const [sortBy, setSortBy] = useState(searchParams.get('sortBy') || 'name')
  const [sortOrder, setSortOrder] = useState(searchParams.get('sortOrder') || 'asc')

  // Add Device Modal state
  const [showAddModal, setShowAddModal] = useState(false)
  const [newDevice, setNewDevice] = useState({
    name: '',
    ui_type: 'Plate Pad',
    description: '',
    status: 'active'
  })
  const [addingDevice, setAddingDevice] = useState(false)
  const [addError, setAddError] = useState(null)

  // Update URL params when filters change
  useEffect(() => {
    const params = new URLSearchParams()
    if (searchTerm) params.set('search', searchTerm)
    if (statusFilter && statusFilter !== 'all') params.set('status', statusFilter)
    if (typeFilter) params.set('type', typeFilter)
    if (sortBy && sortBy !== 'name') params.set('sortBy', sortBy)
    if (sortOrder && sortOrder !== 'asc') params.set('sortOrder', sortOrder)
    
    setSearchParams(params, { replace: true })
  }, [searchTerm, statusFilter, typeFilter, sortBy, sortOrder, setSearchParams])

  useEffect(() => {
    fetchDevices()
  }, [])

  // Extract unique device types from devices
  // Priority: ui_type > device_category mapping > name pattern inference
  const getDeviceType = (device) => {
    // Check explicit ui_type first (user-set device UI type)
    if (device.ui_type) {
      return device.ui_type
    }
    
    // Check device_category and map to user-friendly names
    if (device.device_category) {
      const categoryMap = {
        'static_position': 'Plate Pad',
        'plate_pad': 'Plate Pad',
        'robot': 'PF400 Robot',
        'pf400': 'PF400 Robot',
        'planar_motor': 'Planar Motor',
        'planar': 'Planar Motor'
      }
      if (categoryMap[device.device_category.toLowerCase()]) {
        return categoryMap[device.device_category.toLowerCase()]
      }
    }
    
    // Infer from name patterns
    const name = device.name?.toLowerCase() || ''
    const nameNormalized = name.replace(/[\s\-_]/g, '')
    
    // Check Plate Pad first (various patterns)
    if (nameNormalized.includes('platepad')) return 'Plate Pad'
    if (name.includes('plate_pad') || name.includes('plate pad')) return 'Plate Pad'
    
    // Other device types
    if (name.includes('pf400')) return 'PF400 Robot'
    if (name.includes('planar')) return 'Planar Motor'
    if (name.includes('plateloc')) return 'Plateloc'
    if (name.includes('hotel')) return 'Plate Hotel'
    if (name.includes('echo')) return 'Echo'
    if (name.includes('cytomat')) return 'Cytomat'
    if (name.includes('xpeel')) return 'XPeel'
    if (name.includes('el406')) return 'EL406'
    if (name.includes('carousel')) return 'Carousel'
    if (name.includes('mms')) return 'MMS'
    
    // Fall back to product_name if available
    if (device.product_name) {
      return device.product_name
    }
    
    return 'Other'
  }

  // Get unique types sorted alphabetically
  const uniqueTypes = [...new Set(devices.map(d => getDeviceType(d)))].sort()

  // Filter type suggestions based on input
  const filteredTypeSuggestions = typeFilter
    ? uniqueTypes.filter(t => t.toLowerCase().includes(typeFilter.toLowerCase()))
    : uniqueTypes

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

  const handleAddDevice = async () => {
    if (!newDevice.name.trim()) {
      setAddError('Device name is required')
      return
    }
    
    setAddingDevice(true)
    setAddError(null)
    
    try {
      const res = await fetch(`${API_URL}/devices`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newDevice)
      })
      
      if (res.ok) {
        setShowAddModal(false)
        setNewDevice({ name: '', ui_type: 'Plate Pad', description: '', status: 'active' })
        fetchDevices()
      } else {
        const data = await res.json()
        setAddError(data.detail || 'Failed to create device')
      }
    } catch (e) {
      setAddError(`Error: ${e.message}`)
    } finally {
      setAddingDevice(false)
    }
  }

  const getDeviceTypeIcon = (device) => {
    // Customize icons based on device type or name
    if (device.name?.toLowerCase().includes('planar') || 
        device.product_name?.toLowerCase().includes('planar')) {
      return '🔄'
    }
    // Plate Pad devices
    const nameLower = device.name?.toLowerCase().replace(/[\s-_]/g, '') || ''
    if (nameLower.includes('platepad') || device.device_category === 'static_position') {
      return '📍'
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

      // Type filter - match against device type (case-insensitive partial match)
      const deviceType = getDeviceType(device)
      const matchesType = !typeFilter || 
        deviceType.toLowerCase().includes(typeFilter.toLowerCase())

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
          // Sort by device type
          aValue = getDeviceType(a)
          bValue = getDeviceType(b)
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
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
          <h1 style={{ fontSize: '2em', margin: 0 }}>Tachyon Device Dashboard</h1>
          <button
            onClick={() => setShowAddModal(true)}
            style={{
              padding: '10px 20px',
              background: '#10b981',
              color: '#fff',
              border: 'none',
              borderRadius: 6,
              cursor: 'pointer',
              fontWeight: 'bold',
              fontSize: '1em',
              display: 'flex',
              alignItems: 'center',
              gap: 8
            }}
          >
            ➕ Add Device
          </button>
        </div>
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

          {/* Type Filter - Combo Box with Autocomplete */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 8, position: 'relative' }}>
            <span style={{ color: '#ccc', fontSize: '0.9em', fontWeight: 'bold' }}>Type:</span>
            <div style={{ position: 'relative' }}>
              <input
                type="text"
                value={typeFilter}
                onChange={(e) => setTypeFilter(e.target.value)}
                onFocus={() => setTypeDropdownOpen(true)}
                onBlur={() => setTimeout(() => setTypeDropdownOpen(false), 150)}
                placeholder="All types..."
                style={{
                  padding: '8px 12px',
                  paddingRight: 28,
                  borderRadius: 4,
                  border: '1px solid #555',
                  background: '#2a2a3e',
                  color: '#fff',
                  fontSize: '0.9em',
                  outline: 'none',
                  minWidth: 150
                }}
              />
              <span
                onClick={() => setTypeDropdownOpen(!typeDropdownOpen)}
                style={{
                  position: 'absolute',
                  right: 8,
                  top: '50%',
                  transform: 'translateY(-50%)',
                  cursor: 'pointer',
                  color: '#888',
                  fontSize: '0.7em'
                }}
              >
                ▼
              </span>
              {typeDropdownOpen && filteredTypeSuggestions.length > 0 && (
                <div style={{
                  position: 'absolute',
                  top: '100%',
                  left: 0,
                  right: 0,
                  marginTop: 4,
                  background: '#2a2a3e',
                  border: '1px solid #555',
                  borderRadius: 4,
                  maxHeight: 200,
                  overflowY: 'auto',
                  zIndex: 1000,
                  boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
                }}>
                  <div
                    onClick={() => { setTypeFilter(''); setTypeDropdownOpen(false) }}
                    style={{
                      padding: '8px 12px',
                      cursor: 'pointer',
                      color: '#888',
                      fontStyle: 'italic',
                      borderBottom: '1px solid #444'
                    }}
                    onMouseEnter={(e) => e.currentTarget.style.background = '#3a3a4e'}
                    onMouseLeave={(e) => e.currentTarget.style.background = 'transparent'}
                  >
                    All types
                  </div>
                  {filteredTypeSuggestions.map(type => (
                    <div
                      key={type}
                      onClick={() => { setTypeFilter(type); setTypeDropdownOpen(false) }}
                      style={{
                        padding: '8px 12px',
                        cursor: 'pointer',
                        color: '#fff'
                      }}
                      onMouseEnter={(e) => e.currentTarget.style.background = '#3a3a4e'}
                      onMouseLeave={(e) => e.currentTarget.style.background = 'transparent'}
                    >
                      {type}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Clear Filters */}
          {(searchTerm || statusFilter !== 'all' || typeFilter) && (
            <button
              onClick={() => {
                setSearchTerm('')
                setStatusFilter('all')
                setTypeFilter('')
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
        {(searchTerm || statusFilter !== 'all' || typeFilter || sortBy !== 'name' || sortOrder !== 'asc') && (
          <div style={{ fontSize: '0.9em', color: '#ccc', borderTop: '1px solid #333', paddingTop: 8 }}>
            <strong>Filtered & Sorted Results:</strong> {filteredAndSortedDevices.length} |
            <strong> Active:</strong> {filteredAndSortedDevices.filter(d => d.status === 'active').length} |
            <strong> Offline:</strong> {filteredAndSortedDevices.filter(d => d.status === 'offline').length}
          </div>
        )}
      </div>

      {/* Add Device Modal */}
      {showAddModal && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: 'rgba(0, 0, 0, 0.7)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            background: '#1a1a2e',
            borderRadius: 12,
            padding: 30,
            minWidth: 450,
            maxWidth: 550,
            border: '1px solid #333',
            boxShadow: '0 20px 60px rgba(0, 0, 0, 0.5)'
          }}>
            <h2 style={{ color: '#fff', marginTop: 0, marginBottom: 25 }}>Add New Device</h2>
            
            {addError && (
              <div style={{ 
                background: '#ef4444', 
                color: '#fff', 
                padding: 12, 
                borderRadius: 6, 
                marginBottom: 20 
              }}>
                {addError}
              </div>
            )}
            
            <div style={{ marginBottom: 20 }}>
              <label style={{ display: 'block', color: '#888', marginBottom: 6, fontWeight: 'bold' }}>
                Device Name *
              </label>
              <input
                type="text"
                value={newDevice.name}
                onChange={(e) => setNewDevice({ ...newDevice, name: e.target.value })}
                placeholder="e.g., PlatePad-003"
                style={{
                  width: '100%',
                  padding: '10px 14px',
                  borderRadius: 6,
                  border: '1px solid #444',
                  background: '#2a2a3e',
                  color: '#fff',
                  fontSize: '1em',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
            </div>
            
            <div style={{ marginBottom: 20 }}>
              <label style={{ display: 'block', color: '#888', marginBottom: 6, fontWeight: 'bold' }}>
                Device Type
              </label>
              <select
                value={newDevice.ui_type}
                onChange={(e) => setNewDevice({ ...newDevice, ui_type: e.target.value })}
                style={{
                  width: '100%',
                  padding: '10px 14px',
                  borderRadius: 6,
                  border: '1px solid #444',
                  background: '#2a2a3e',
                  color: '#fff',
                  fontSize: '1em',
                  cursor: 'pointer',
                  outline: 'none'
                }}
              >
                <option value="Plate Pad">Plate Pad</option>
                <option value="PF400 Robot">PF400 Robot</option>
                <option value="Planar Motor">Planar Motor</option>
                <option value="Plateloc">Plateloc</option>
                <option value="Plate Hotel">Plate Hotel</option>
                <option value="Echo">Echo</option>
                <option value="Cytomat">Cytomat</option>
                <option value="XPeel">XPeel</option>
                <option value="EL406">EL406</option>
                <option value="Carousel">Carousel</option>
                <option value="Other">Other</option>
              </select>
            </div>
            
            <div style={{ marginBottom: 20 }}>
              <label style={{ display: 'block', color: '#888', marginBottom: 6, fontWeight: 'bold' }}>
                Description
              </label>
              <textarea
                value={newDevice.description}
                onChange={(e) => setNewDevice({ ...newDevice, description: e.target.value })}
                placeholder="Optional description..."
                rows={3}
                style={{
                  width: '100%',
                  padding: '10px 14px',
                  borderRadius: 6,
                  border: '1px solid #444',
                  background: '#2a2a3e',
                  color: '#fff',
                  fontSize: '1em',
                  resize: 'vertical',
                  outline: 'none',
                  boxSizing: 'border-box'
                }}
              />
            </div>
            
            <div style={{ marginBottom: 25 }}>
              <label style={{ display: 'block', color: '#888', marginBottom: 6, fontWeight: 'bold' }}>
                Status
              </label>
              <select
                value={newDevice.status}
                onChange={(e) => setNewDevice({ ...newDevice, status: e.target.value })}
                style={{
                  width: '100%',
                  padding: '10px 14px',
                  borderRadius: 6,
                  border: '1px solid #444',
                  background: '#2a2a3e',
                  color: '#fff',
                  fontSize: '1em',
                  cursor: 'pointer',
                  outline: 'none'
                }}
              >
                <option value="active">Active</option>
                <option value="offline">Offline</option>
              </select>
            </div>
            
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12 }}>
              <button
                onClick={() => {
                  setShowAddModal(false)
                  setAddError(null)
                  setNewDevice({ name: '', ui_type: 'Plate Pad', description: '', status: 'active' })
                }}
                style={{
                  padding: '10px 20px',
                  background: '#555',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 6,
                  cursor: 'pointer',
                  fontSize: '1em'
                }}
              >
                Cancel
              </button>
              <button
                onClick={handleAddDevice}
                disabled={addingDevice}
                style={{
                  padding: '10px 20px',
                  background: addingDevice ? '#666' : '#10b981',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 6,
                  cursor: addingDevice ? 'not-allowed' : 'pointer',
                  fontWeight: 'bold',
                  fontSize: '1em'
                }}
              >
                {addingDevice ? 'Creating...' : '✓ Create Device'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default DeviceDashboard


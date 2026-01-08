import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'

const DEFAULT_API_URL = `${window.location.protocol}//${window.location.hostname}:8091`
const ENV_API_URL = import.meta.env.VITE_API_URL
const API_URL = (ENV_API_URL && !(ENV_API_URL.includes('localhost') && window.location.hostname !== 'localhost'))
  ? ENV_API_URL
  : DEFAULT_API_URL

function PlatePadDiagnostics() {
  const { deviceName } = useParams()
  const [device, setDevice] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [saving, setSaving] = useState(false)
  const [editMode, setEditMode] = useState(false)
  
  // Editable fields
  const [deviceDisplayName, setDeviceDisplayName] = useState('')
  const [uiType, setUiType] = useState('Plate Pad')
  const [description, setDescription] = useState('')
  const [config, setConfig] = useState({
    plate_capacity: 1,
    current_plate_count: 0,
    default_orientation: 'landscape'
  })
  const [robotAccess, setRobotAccess] = useState([])
  
  // Device UI types that have dedicated interfaces
  const UI_TYPES = ['Plate Pad', 'PF400 Robot', 'Planar Motor']

  // Available robots and teachpoints for linking
  const [availableRobots, setAvailableRobots] = useState([])
  const [selectedRobot, setSelectedRobot] = useState('')
  const [availableTeachpoints, setAvailableTeachpoints] = useState([])
  const [selectedTeachpoint, setSelectedTeachpoint] = useState('')

  useEffect(() => {
    fetchDevice()
    fetchAvailableRobots()
  }, [deviceName])

  const fetchDevice = async () => {
    try {
      setLoading(true)
      const res = await fetch(`${API_URL}/devices/${encodeURIComponent(deviceName)}`)
      if (!res.ok) {
        throw new Error(`Failed to fetch device: ${res.status}`)
      }
      const data = await res.json()
      setDevice(data)
      setDeviceDisplayName(data.name || deviceName)
      setUiType(data.ui_type || 'Plate Pad')
      setDescription(data.description || '')
      setConfig(data.config || { plate_capacity: 1, current_plate_count: 0, default_orientation: 'landscape' })
      setRobotAccess(data.robot_access || [])
      setError(null)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  const fetchAvailableRobots = async () => {
    try {
      const res = await fetch(`${API_URL}/devices`)
      if (res.ok) {
        const data = await res.json()
        // Filter for PF400 robots
        const robots = (data.devices || data || []).filter(d => 
          d.name?.startsWith('PF400') || d.device_type_id?.includes('pf400')
        )
        setAvailableRobots(robots)
      }
    } catch (e) {
      console.error('Failed to fetch robots:', e)
    }
  }

  const fetchTeachpointsForRobot = async (robotName) => {
    try {
      const res = await fetch(`${API_URL}/devices/${encodeURIComponent(robotName)}/teachpoints`)
      if (res.ok) {
        const data = await res.json()
        setAvailableTeachpoints(data.teachpoints || data || [])
      }
    } catch (e) {
      console.error('Failed to fetch teachpoints:', e)
      setAvailableTeachpoints([])
    }
  }

  const handleRobotSelect = (robotName) => {
    setSelectedRobot(robotName)
    setSelectedTeachpoint('')
    if (robotName) {
      fetchTeachpointsForRobot(robotName)
    } else {
      setAvailableTeachpoints([])
    }
  }

  const handleSave = async () => {
    try {
      setSaving(true)
      const payload = {
        description,
        config,
        robot_access: robotAccess,
        ui_type: uiType
      }
      // Include name if it was changed
      if (deviceDisplayName && deviceDisplayName !== deviceName) {
        payload.name = deviceDisplayName
      }
      
      const res = await fetch(`${API_URL}/devices/${encodeURIComponent(deviceName)}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      if (!res.ok) {
        const errData = await res.json().catch(() => ({}))
        throw new Error(errData.detail || `Failed to save: ${res.status}`)
      }
      const updated = await res.json()
      
      // If renamed, navigate to the new URL
      if (updated.name && updated.name !== deviceName) {
        window.location.href = `/devices/${encodeURIComponent(updated.name)}/diagnostics`
        return
      }
      
      await fetchDevice()
      setEditMode(false)
    } catch (e) {
      setError(e.message)
    } finally {
      setSaving(false)
    }
  }

  const handleAddRobotLink = () => {
    if (!selectedRobot || !selectedTeachpoint) return
    
    // Check if this link already exists
    const exists = robotAccess.some(
      ra => ra.robot_name === selectedRobot && ra.teachpoint_id === selectedTeachpoint
    )
    if (exists) {
      alert('This robot-teachpoint link already exists')
      return
    }

    setRobotAccess([
      ...robotAccess,
      {
        robot_name: selectedRobot,
        teachpoint_id: selectedTeachpoint,
        access_type: 'pick_place',
        linked_at: new Date().toISOString()
      }
    ])
    setSelectedRobot('')
    setSelectedTeachpoint('')
    setAvailableTeachpoints([])
  }

  const handleRemoveRobotLink = (index) => {
    setRobotAccess(robotAccess.filter((_, i) => i !== index))
  }

  if (loading) {
    return (
      <div style={{ padding: 40, textAlign: 'center', color: '#888' }}>
        Loading device...
      </div>
    )
  }

  if (error) {
    return (
      <div style={{ padding: 40 }}>
        <div style={{ color: '#ff4d4f', marginBottom: 20 }}>Error: {error}</div>
        <button onClick={fetchDevice} style={buttonStyle}>Retry</button>
      </div>
    )
  }

  return (
    <div style={{ padding: 20, maxWidth: 900, margin: '0 auto' }}>
      {/* Header */}
      <div style={{ marginBottom: 30 }}>
        <Link to="/devices" style={{ color: '#1890ff', textDecoration: 'none', marginBottom: 10, display: 'inline-block' }}>
          ← Back to Devices
        </Link>
        <div style={{ display: 'flex', alignItems: 'center', gap: 15, marginTop: 10 }}>
          <span style={{ fontSize: '2.5em' }}>📍</span>
          <div style={{ flex: 1 }}>
            {editMode ? (
              <input
                type="text"
                value={deviceDisplayName}
                onChange={(e) => setDeviceDisplayName(e.target.value)}
                style={{
                  ...inputStyle,
                  fontSize: '1.5em',
                  fontWeight: 'bold',
                  padding: '8px 12px',
                  maxWidth: 400
                }}
                placeholder="Device name"
              />
            ) : (
              <h1 style={{ margin: 0, fontSize: '1.8em' }}>{deviceDisplayName || deviceName}</h1>
            )}
            <div style={{ color: '#888', marginTop: 5 }}>
              Plate Pad • Static Position Device
              {device?._id && <span style={{ color: '#555', marginLeft: 10, fontSize: '0.85em' }}>ID: {device._id}</span>}
            </div>
          </div>
          <div style={{ marginLeft: 'auto' }}>
            {!editMode ? (
              <button onClick={() => setEditMode(true)} style={buttonStyle}>
                ✏️ Edit
              </button>
            ) : (
              <div style={{ display: 'flex', gap: 10 }}>
                <button onClick={handleSave} disabled={saving} style={{ ...buttonStyle, background: '#52c41a' }}>
                  {saving ? 'Saving...' : '💾 Save'}
                </button>
                <button onClick={() => { setEditMode(false); fetchDevice() }} style={{ ...buttonStyle, background: '#666' }}>
                  Cancel
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Status Badge and Device Type */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 20, marginBottom: 25, flexWrap: 'wrap' }}>
        <div style={{
          display: 'inline-block',
          padding: '6px 16px',
          borderRadius: 20,
          background: device?.status === 'active' ? '#52c41a' : '#ff4d4f',
          color: '#fff',
          fontWeight: 'bold'
        }}>
          {device?.status || 'Unknown'}
        </div>
        
        {/* Device UI Type Selector */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ color: '#888', fontSize: '0.9em' }}>Device Type:</span>
          {editMode ? (
            <select
              value={uiType}
              onChange={(e) => setUiType(e.target.value)}
              style={{
                padding: '6px 12px',
                borderRadius: 4,
                border: '1px solid #555',
                background: '#2a2a3e',
                color: '#fff',
                fontSize: '0.9em',
                cursor: 'pointer'
              }}
            >
              {UI_TYPES.map(type => (
                <option key={type} value={type}>{type}</option>
              ))}
            </select>
          ) : (
            <span style={{
              padding: '6px 12px',
              borderRadius: 4,
              background: '#2a2a3e',
              color: '#1890ff',
              fontWeight: 'bold',
              fontSize: '0.9em'
            }}>
              {uiType}
            </span>
          )}
        </div>
      </div>

      {/* Description */}
      <Section title="Description">
        {editMode ? (
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            style={{ ...inputStyle, minHeight: 80, resize: 'vertical' }}
            placeholder="Description of this plate pad location..."
          />
        ) : (
          <div style={{ color: '#ccc' }}>{description || 'No description'}</div>
        )}
      </Section>

      {/* Configuration */}
      <Section title="Configuration">
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 15 }}>
          <div>
            <label style={labelStyle}>Plate Capacity</label>
            {editMode ? (
              <input
                type="number"
                min="1"
                value={config.plate_capacity || 1}
                onChange={(e) => setConfig({ ...config, plate_capacity: parseInt(e.target.value) || 1 })}
                style={inputStyle}
              />
            ) : (
              <div style={{ color: '#fff', fontSize: '1.1em' }}>{config.plate_capacity || 1}</div>
            )}
          </div>
          <div>
            <label style={labelStyle}>Current Plate Count</label>
            {editMode ? (
              <input
                type="number"
                min="0"
                value={config.current_plate_count || 0}
                onChange={(e) => setConfig({ ...config, current_plate_count: parseInt(e.target.value) || 0 })}
                style={inputStyle}
              />
            ) : (
              <div style={{ color: '#fff', fontSize: '1.1em' }}>{config.current_plate_count || 0}</div>
            )}
          </div>
          <div>
            <label style={labelStyle}>Default Orientation</label>
            {editMode ? (
              <select
                value={config.default_orientation || 'landscape'}
                onChange={(e) => setConfig({ ...config, default_orientation: e.target.value })}
                style={inputStyle}
              >
                <option value="landscape">Landscape</option>
                <option value="portrait">Portrait</option>
              </select>
            ) : (
              <div style={{ color: '#fff', fontSize: '1.1em', textTransform: 'capitalize' }}>
                {config.default_orientation || 'landscape'}
              </div>
            )}
          </div>
        </div>
      </Section>

      {/* Robot Access Links */}
      <Section title="Robot Teachpoint Links">
        <p style={{ color: '#888', marginBottom: 15, fontSize: '0.9em' }}>
          Link this plate pad to robot teachpoints for automated pick/place operations.
        </p>

        {/* Existing links */}
        {robotAccess.length > 0 ? (
          <div style={{ marginBottom: 20 }}>
            {robotAccess.map((link, idx) => (
              <div key={idx} style={{
                display: 'flex',
                alignItems: 'center',
                gap: 15,
                padding: '12px 16px',
                background: '#2a2a3e',
                borderRadius: 6,
                marginBottom: 8,
                border: '1px solid #444'
              }}>
                <span style={{ fontSize: '1.3em' }}>🤖</span>
                <div style={{ flex: 1 }}>
                  <div style={{ color: '#fff', fontWeight: 'bold' }}>{link.robot_name}</div>
                  <div style={{ color: '#888', fontSize: '0.9em' }}>
                    Teachpoint: <span style={{ color: '#1890ff' }}>{link.teachpoint_id}</span>
                    {' • '}{link.access_type || 'pick_place'}
                  </div>
                </div>
                {editMode && (
                  <button
                    onClick={() => handleRemoveRobotLink(idx)}
                    style={{ ...buttonStyle, background: '#ff4d4f', padding: '6px 12px' }}
                  >
                    Remove
                  </button>
                )}
              </div>
            ))}
          </div>
        ) : (
          <div style={{ color: '#666', marginBottom: 20, padding: 20, background: '#1a1a2e', borderRadius: 6, textAlign: 'center' }}>
            No robot links configured
          </div>
        )}

        {/* Add new link */}
        {editMode && (
          <div style={{
            padding: 15,
            background: '#1a1a2e',
            borderRadius: 6,
            border: '1px dashed #444'
          }}>
            <div style={{ fontWeight: 'bold', marginBottom: 12, color: '#ccc' }}>Add Robot Link</div>
            <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', flexWrap: 'wrap' }}>
              <div>
                <label style={labelStyle}>Robot</label>
                <select
                  value={selectedRobot}
                  onChange={(e) => handleRobotSelect(e.target.value)}
                  style={{ ...inputStyle, minWidth: 180 }}
                >
                  <option value="">Select robot...</option>
                  {availableRobots.map(r => (
                    <option key={r.name} value={r.name}>{r.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label style={labelStyle}>Teachpoint</label>
                <select
                  value={selectedTeachpoint}
                  onChange={(e) => setSelectedTeachpoint(e.target.value)}
                  style={{ ...inputStyle, minWidth: 180 }}
                  disabled={!selectedRobot}
                >
                  <option value="">Select teachpoint...</option>
                  {availableTeachpoints.map(tp => (
                    <option key={tp.id || tp.name} value={tp.id || tp.name}>
                      {tp.name || tp.id}
                    </option>
                  ))}
                </select>
              </div>
              <button
                onClick={handleAddRobotLink}
                disabled={!selectedRobot || !selectedTeachpoint}
                style={{
                  ...buttonStyle,
                  background: (!selectedRobot || !selectedTeachpoint) ? '#444' : '#1890ff',
                  cursor: (!selectedRobot || !selectedTeachpoint) ? 'not-allowed' : 'pointer'
                }}
              >
                + Add Link
              </button>
            </div>
          </div>
        )}
      </Section>

      {/* Raw Device Data (collapsed by default) */}
      <details style={{ marginTop: 30 }}>
        <summary style={{ cursor: 'pointer', color: '#888', padding: '10px 0' }}>
          Raw Device Data (Debug)
        </summary>
        <pre style={{
          background: '#1a1a2e',
          padding: 15,
          borderRadius: 6,
          overflow: 'auto',
          fontSize: '0.85em',
          color: '#aaa'
        }}>
          {JSON.stringify(device, null, 2)}
        </pre>
      </details>
    </div>
  )
}

// Helper Components
function Section({ title, children }) {
  return (
    <div style={{
      background: '#1e1e2e',
      borderRadius: 8,
      padding: 20,
      marginBottom: 20,
      border: '1px solid #333'
    }}>
      <h3 style={{ margin: '0 0 15px 0', color: '#fff', fontSize: '1.1em' }}>{title}</h3>
      {children}
    </div>
  )
}

// Styles
const buttonStyle = {
  padding: '10px 20px',
  borderRadius: 6,
  border: 'none',
  background: '#1890ff',
  color: '#fff',
  cursor: 'pointer',
  fontWeight: 'bold',
  fontSize: '0.95em'
}

const inputStyle = {
  width: '100%',
  padding: '10px 12px',
  borderRadius: 4,
  border: '1px solid #444',
  background: '#2a2a3e',
  color: '#fff',
  fontSize: '1em',
  boxSizing: 'border-box'
}

const labelStyle = {
  display: 'block',
  color: '#888',
  fontSize: '0.85em',
  marginBottom: 5,
  textTransform: 'uppercase'
}

export default PlatePadDiagnostics

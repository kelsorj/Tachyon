import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import PlanarMotorViewer from './PlanarMotorViewer'

const PF400_API_URL = "http://localhost:3061" // For fetching device info from MongoDB

function PlanarMotorDiagnostics() {
  const { deviceName } = useParams()
  const [xbots, setXbots] = useState({})
  const [pmcStatus, setPmcStatus] = useState(null)
  const [connected, setConnected] = useState(false)
  const [logs, setLogs] = useState([])
  const [selectedXbot, setSelectedXbot] = useState(1)
  const [jogStep, setJogStep] = useState(0.010) // 10mm default
  const [maxSpeed, setMaxSpeed] = useState(0.5)
  const [maxAcceleration, setMaxAcceleration] = useState(5.0)
  const [pmcIp, setPmcIp] = useState('192.168.10.100') // Will be updated from status
  const [apiUrl, setApiUrl] = useState(null) // Will be set from device connection info
  const [loadingDevice, setLoadingDevice] = useState(true)
  
  // Teachpoints state
  const [teachpoints, setTeachpoints] = useState([])
  const [newTeachpointName, setNewTeachpointName] = useState('')
  const [showTeachpointForm, setShowTeachpointForm] = useState(false)

  const log = (msg) => setLogs(prev => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev.slice(0, 14)])

  // Fetch device info from MongoDB to get connection details
  useEffect(() => {
    const fetchDeviceInfo = async () => {
      if (!deviceName) {
        setLoadingDevice(false)
        return
      }

      try {
        setLoadingDevice(true)
        // Get all devices and find the one matching deviceName
        const res = await fetch(`${PF400_API_URL}/devices`)
        if (res.ok) {
          const data = await res.json()
          const device = data.devices?.find(d => d.name === deviceName)
          
          if (device) {
            // Get API URL from device connection config
            const apiPort = device.connection?.api_port || 3062
            const backendHost = device.connection?.backend_host || 'localhost'
            
            const constructedApiUrl = `http://${backendHost}:${apiPort}`
            setApiUrl(constructedApiUrl)
            
            // Also set PMC IP if available
            if (device.connection?.pmc_ip) {
              setPmcIp(device.connection.pmc_ip)
            }
            
            log(`✓ Loaded device: ${device.name}`)
            log(`  Backend: ${constructedApiUrl}`)
            if (device.connection?.pmc_ip) {
              log(`  PMC IP: ${device.connection.pmc_ip}`)
            }
          } else {
            log(`⚠ Device '${deviceName}' not found in database, using defaults`)
            setApiUrl('http://localhost:3062')
          }
        } else {
          log(`⚠ Failed to fetch devices, using defaults`)
          setApiUrl('http://localhost:3062')
        }
      } catch (e) {
        log(`⚠ Error fetching device info: ${e.message}, using defaults`)
        setApiUrl('http://localhost:3062')
      } finally {
        setLoadingDevice(false)
      }
    }
    
    fetchDeviceInfo()
  }, [deviceName])

  // Fetch initial status to get PMC IP (after API URL is set)
  useEffect(() => {
    if (!apiUrl || loadingDevice) return
    
    const fetchInitialStatus = async () => {
      try {
        const res = await fetch(`${apiUrl}/status`)
        if (res.ok) {
          const data = await res.json()
          if (data.pmc_ip) {
            setPmcIp(data.pmc_ip)
          }
        }
      } catch (e) {
        // Silently ignore - backend might not be running yet
      }
    }
    fetchInitialStatus()
  }, [apiUrl, loadingDevice])

  // Fetch XBOT statuses periodically
  useEffect(() => {
    if (!apiUrl || loadingDevice) return
    
    let isMounted = true
    let timeoutId = null
    
    const fetchStatus = async () => {
      if (!isMounted || !connected || !apiUrl) return
      
      try {
        const controller = new AbortController()
        const timeoutAbort = setTimeout(() => controller.abort(), 2000)
        
        const [statusRes, xbotsRes] = await Promise.all([
          fetch(`${apiUrl}/xbots/status`, { signal: controller.signal }),
          fetch(`${apiUrl}/pmc/status`, { signal: controller.signal })
        ])
        clearTimeout(timeoutAbort)
        
        if (isMounted) {
          if (statusRes.ok) {
            const statusData = await statusRes.json()
            setXbots(statusData.xbots || {})
          }
          if (xbotsRes.ok) {
            const pmcData = await xbotsRes.json()
            setPmcStatus(pmcData.status)
          }
        }
      } catch (e) {
        // Silently ignore errors
      }
      
      if (isMounted && connected) {
        timeoutId = setTimeout(fetchStatus, 500)
      }
    }
    
    if (connected) {
      fetchStatus()
    }
    
    return () => {
      isMounted = false
      if (timeoutId) clearTimeout(timeoutId)
    }
  }, [connected, apiUrl, loadingDevice])

  // Connect to PMC
  const handleConnect = async () => {
    if (!apiUrl) {
      log('✗ Backend URL not configured. Please wait for device info to load.')
      return
    }
    
    try {
      const res = await fetch(`${apiUrl}/connect`, { method: 'POST' })
      const data = await res.json()
      if (res.ok) {
        log(`✓ Connected to PMC. Found ${data.xbot_count || 0} XBOT(s)`)
        setConnected(true)
      } else {
        const errorMsg = data.detail || data.message || 'Unknown error'
        log(`✗ Failed to connect: ${errorMsg}`)
        // Show more detailed error for network issues
        if (errorMsg.includes('network') || errorMsg.includes('connect') || errorMsg.includes('reachable')) {
          // Extract IP from error message if available, otherwise use current pmcIp state
          const ipMatch = errorMsg.match(/192\.168\.\d+\.\d+/)
          const displayIp = ipMatch ? ipMatch[0] : pmcIp
          log(`  → Check that PMC is powered on and reachable at ${displayIp}`)
        }
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Disconnect from PMC
  const handleDisconnect = async () => {
    if (!apiUrl) return
    
    try {
      const res = await fetch(`${apiUrl}/disconnect`, { method: 'POST' })
      if (res.ok) {
        log(`✓ Disconnected from PMC`)
        setConnected(false)
        setXbots({})
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Activate XBOTs
  const handleActivate = async () => {
    if (!apiUrl) return
    
    log(`→ Activating XBOTs...`)
    try {
      const res = await fetch(`${apiUrl}/xbots/activate`, { method: 'POST' })
      const data = await res.json()
      if (res.ok) {
        log(`✓ XBOTs activated`)
      } else {
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Levitate XBOT
  const handleLevitate = async (xbotId) => {
    if (!apiUrl) return
    
    log(`→ Levitating XBOT ${xbotId}...`)
    try {
      const res = await fetch(`${apiUrl}/xbots/${xbotId}/levitate`, { method: 'POST' })
      const data = await res.json()
      if (res.ok) {
        log(`✓ XBOT ${xbotId} levitating`)
      } else {
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Land XBOT
  const handleLand = async (xbotId) => {
    if (!apiUrl) return
    
    log(`→ Landing XBOT ${xbotId}...`)
    try {
      const res = await fetch(`${apiUrl}/xbots/${xbotId}/land`, { method: 'POST' })
      const data = await res.json()
      if (res.ok) {
        log(`✓ XBOT ${xbotId} landing`)
      } else {
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Stop XBOT
  const handleStop = async (xbotId) => {
    if (!apiUrl) return
    
    log(`→ Stopping XBOT ${xbotId}...`)
    try {
      const res = await fetch(`${apiUrl}/xbots/${xbotId}/stop-motion`, { method: 'POST' })
      const data = await res.json()
      if (res.ok) {
        log(`✓ XBOT ${xbotId} stopped`)
      } else {
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Jog XBOT
  const handleJog = async (axis, direction) => {
    if (!apiUrl) return
    
    const distance = direction * jogStep
    log(`→ Jogging XBOT ${selectedXbot} ${axis.toUpperCase()}: ${(distance * 1000).toFixed(1)}mm`)
    
    try {
      const res = await fetch(`${apiUrl}/xbots/jog`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          xbot_id: selectedXbot,
          axis: axis,
          distance: distance,
          max_speed: maxSpeed,
          max_acceleration: maxAcceleration
        })
      })
      const data = await res.json()
      if (res.ok) {
        log(`✓ Jog complete`)
      } else {
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // ============== Teachpoints ==============
  
  // Fetch teachpoints from MongoDB (via Mac backend)
  const fetchTeachpoints = async () => {
    if (!deviceName) return
    try {
      const res = await fetch(`${PF400_API_URL}/devices/${deviceName}/teachpoints`)
      if (res.ok) {
        const data = await res.json()
        setTeachpoints(data.teachpoints || [])
      }
    } catch (e) {
      console.error('Error fetching teachpoints:', e)
    }
  }

  // Fetch teachpoints on mount and when device changes
  useEffect(() => {
    if (deviceName && !loadingDevice) {
      fetchTeachpoints()
    }
  }, [deviceName, loadingDevice])

  // Save current position as teachpoint
  const handleSaveTeachpoint = async () => {
    if (!newTeachpointName.trim()) {
      log('✗ Please enter a teachpoint name')
      return
    }
    if (!connected || !xbots[selectedXbot]) {
      log('✗ Connect and select an XBOT first')
      return
    }

    const xbot = xbots[selectedXbot]
    const position = xbot.position || {}
    const tpId = newTeachpointName.toLowerCase().replace(/\s+/g, '_').replace(/[^a-z0-9_]/g, '')
    
    log(`→ Saving teachpoint "${newTeachpointName}"...`)
    
    try {
      const res = await fetch(`${PF400_API_URL}/devices/${deviceName}/teachpoints`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          device_name: deviceName,
          id: tpId,
          name: newTeachpointName,
          description: `XBOT ${selectedXbot} position`,
          position: {
            x: position.x || 0,
            y: position.y || 0,
            z: position.z || 0,
            rx: position.rx || 0,
            ry: position.ry || 0,
            rz: position.rz || 0
          },
          xbot_id: selectedXbot
        })
      })
      
      if (res.ok) {
        log(`✓ Saved teachpoint "${newTeachpointName}"`)
        setNewTeachpointName('')
        setShowTeachpointForm(false)
        fetchTeachpoints()
      } else {
        const data = await res.json()
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Delete teachpoint
  const handleDeleteTeachpoint = async (tpId, tpName) => {
    if (!confirm(`Delete teachpoint "${tpName}"?`)) return
    
    log(`→ Deleting teachpoint "${tpName}"...`)
    
    try {
      const res = await fetch(`${PF400_API_URL}/devices/${deviceName}/teachpoints/${tpId}`, {
        method: 'DELETE'
      })
      
      if (res.ok) {
        log(`✓ Deleted teachpoint "${tpName}"`)
        fetchTeachpoints()
      } else {
        const data = await res.json()
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Go to teachpoint (move XBOT to saved position)
  const handleGotoTeachpoint = async (tp) => {
    if (!connected || !apiUrl) {
      log('✗ Not connected')
      return
    }
    
    const pos = tp.position || {}
    log(`→ Moving to "${tp.name}" (X: ${(pos.x * 1000).toFixed(1)}mm, Y: ${(pos.y * 1000).toFixed(1)}mm)...`)
    
    try {
      const res = await fetch(`${apiUrl}/xbots/linear-motion`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          xbot_id: tp.xbot_id || selectedXbot,
          x: pos.x || 0,
          y: pos.y || 0,
          final_speed: 0,
          max_speed: maxSpeed,
          max_acceleration: maxAcceleration
        })
      })
      
      if (res.ok) {
        log(`✓ Moving to "${tp.name}"`)
      } else {
        const data = await res.json()
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  const xbotIds = Object.keys(xbots).map(id => parseInt(id)).sort()

  if (loadingDevice) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', padding: 10, boxSizing: 'border-box', alignItems: 'center', justifyContent: 'center' }}>
        <div style={{ fontSize: '1.2em', color: '#888' }}>Loading device configuration...</div>
      </div>
    )
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', padding: 10, boxSizing: 'border-box' }}>
      <h1 style={{ margin: '0 0 10px 0', fontSize: '1.5em' }}>
        Planar Motor Control
        {deviceName && <span style={{ fontSize: '0.7em', color: '#888', marginLeft: 10 }}>({deviceName})</span>}
      </h1>
      {apiUrl && (
        <div style={{ fontSize: '0.85em', color: '#666', marginBottom: 10 }}>
          Backend: {apiUrl}
        </div>
      )}

      <div style={{ display: 'flex', flex: 1, gap: 15, minHeight: 0 }}>
        
        {/* 3D VIEWER - Takes 70% of width */}
        <div style={{ flex: 7, minWidth: 0, border: '2px solid #444', borderRadius: 8, overflow: 'hidden' }}>
          <PlanarMotorViewer xbots={xbots} />
        </div>

        {/* CONTROLS - Takes 30% of width */}
        <div style={{ flex: 3, minWidth: 280, maxWidth: 400, display: 'flex', flexDirection: 'column', gap: 10 }}>
          
          {/* Connection Status */}
          <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10 }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>Connection</div>
            <div style={{ marginBottom: 8 }}>
              <div>Status: <span style={{ color: connected ? '#52c41a' : '#ff4d4f' }}>{connected ? 'Connected' : 'Disconnected'}</span></div>
              {pmcStatus && <div>PMC: {pmcStatus}</div>}
            </div>
            <div style={{ display: 'flex', gap: 5 }}>
              {!connected ? (
                <button 
                  onClick={handleConnect}
                  style={{ flex: 1, padding: '8px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                >
                  Connect
                </button>
              ) : (
                <button 
                  onClick={handleDisconnect}
                  style={{ flex: 1, padding: '8px', borderRadius: 4, background: '#ff4d4f', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                >
                  Disconnect
                </button>
              )}
            </div>
          </div>

          {/* System Controls */}
          <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10 }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>System</div>
            <button 
              onClick={handleActivate}
              style={{ width: '100%', padding: '8px', borderRadius: 4, background: '#1890ff', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold', marginBottom: 5 }}
            >
              Activate XBOTs
            </button>
          </div>

          {/* XBOT Selection */}
          {xbotIds.length > 0 && (
            <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10 }}>
              <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>XBOT Selection</div>
              <select 
                value={selectedXbot} 
                onChange={e => setSelectedXbot(parseInt(e.target.value))}
                style={{ width: '100%', padding: '6px', borderRadius: 4, background: '#222', color: '#fff', border: '1px solid #444' }}
              >
                {xbotIds.map(id => (
                  <option key={id} value={id}>XBOT {id}</option>
                ))}
              </select>
            </div>
          )}

          {/* XBOT Controls */}
          {xbotIds.length > 0 && (
            <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10 }}>
              <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>XBOT Controls</div>
              <div style={{ display: 'flex', gap: 5, marginBottom: 5 }}>
                <button 
                  onClick={() => handleLevitate(selectedXbot)}
                  style={{ flex: 1, padding: '6px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.9em' }}
                >
                  Levitate
                </button>
                <button 
                  onClick={() => handleLand(selectedXbot)}
                  style={{ flex: 1, padding: '6px', borderRadius: 4, background: '#faad14', color: '#000', border: 'none', cursor: 'pointer', fontSize: '0.9em' }}
                >
                  Land
                </button>
                <button 
                  onClick={() => handleStop(selectedXbot)}
                  style={{ flex: 1, padding: '6px', borderRadius: 4, background: '#ff4d4f', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.9em' }}
                >
                  Stop
                </button>
              </div>
            </div>
          )}

          {/* Jog Controls */}
          {xbotIds.length > 0 && (
            <>
              <div style={{ background: '#222', borderRadius: 8, padding: 10 }}>
                <div style={{ fontWeight: 'bold', marginBottom: 8 }}>Jog Settings</div>
                <div style={{ marginBottom: 8 }}>
                  <div style={{ fontSize: '0.85em', marginBottom: 4 }}>Step Size (mm)</div>
                  <select 
                    value={jogStep} 
                    onChange={e => setJogStep(parseFloat(e.target.value))}
                    style={{ width: '100%', padding: '4px', borderRadius: 4, background: '#333', color: '#fff', border: '1px solid #555' }}
                  >
                    <option value={0.001}>1</option>
                    <option value={0.005}>5</option>
                    <option value={0.010}>10</option>
                    <option value={0.025}>25</option>
                    <option value={0.050}>50</option>
                  </select>
                </div>
                <div style={{ marginBottom: 8 }}>
                  <div style={{ fontSize: '0.85em', marginBottom: 4 }}>Max Speed (m/s)</div>
                  <input 
                    type="number"
                    value={maxSpeed}
                    onChange={e => setMaxSpeed(parseFloat(e.target.value) || 0.5)}
                    step="0.1"
                    min="0.1"
                    max="2.0"
                    style={{ width: '100%', padding: '4px', borderRadius: 4, background: '#333', color: '#fff', border: '1px solid #555' }}
                  />
                </div>
                <div>
                  <div style={{ fontSize: '0.85em', marginBottom: 4 }}>Max Acceleration (m/s²)</div>
                  <input 
                    type="number"
                    value={maxAcceleration}
                    onChange={e => setMaxAcceleration(parseFloat(e.target.value) || 5.0)}
                    step="0.5"
                    min="1.0"
                    max="20.0"
                    style={{ width: '100%', padding: '4px', borderRadius: 4, background: '#333', color: '#fff', border: '1px solid #555' }}
                  />
                </div>
              </div>

              <div style={{ background: '#222', borderRadius: 8, padding: 10 }}>
                <div style={{ fontWeight: 'bold', marginBottom: 8 }}>Jog Controls</div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 5 }}>
                  <div></div>
                  <button 
                    onClick={() => handleJog('y', 1)}
                    style={{ padding: '10px', borderRadius: 4, background: '#95de64', color: '#000', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                  >
                    ▲ Y+
                  </button>
                  <div></div>
                  
                  <button 
                    onClick={() => handleJog('x', -1)}
                    style={{ padding: '10px', borderRadius: 4, background: '#ff7875', color: '#000', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                  >
                    ◄ X-
                  </button>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '0.8em' }}>Stop</div>
                  <button 
                    onClick={() => handleJog('x', 1)}
                    style={{ padding: '10px', borderRadius: 4, background: '#ff7875', color: '#000', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                  >
                    X+ ►
                  </button>
                  
                  <div></div>
                  <button 
                    onClick={() => handleJog('y', -1)}
                    style={{ padding: '10px', borderRadius: 4, background: '#a8071a', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                  >
                    ▼ Y-
                  </button>
                  <div></div>
                </div>
              </div>
            </>
          )}

          {/* Teachpoints */}
          <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
              <div style={{ fontWeight: 'bold', color: '#69c0ff' }}>Teachpoints</div>
              {connected && (
                <button
                  onClick={() => setShowTeachpointForm(!showTeachpointForm)}
                  style={{ padding: '4px 8px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.8em' }}
                >
                  {showTeachpointForm ? 'Cancel' : '+ Save Current'}
                </button>
              )}
            </div>
            
            {/* Save teachpoint form */}
            {showTeachpointForm && (
              <div style={{ marginBottom: 10, padding: 8, background: '#222', borderRadius: 4 }}>
                <input
                  type="text"
                  placeholder="Teachpoint name"
                  value={newTeachpointName}
                  onChange={e => setNewTeachpointName(e.target.value)}
                  onKeyPress={e => e.key === 'Enter' && handleSaveTeachpoint()}
                  style={{ width: '100%', padding: '6px', marginBottom: 8, borderRadius: 4, background: '#333', color: '#fff', border: '1px solid #555', boxSizing: 'border-box' }}
                />
                <button
                  onClick={handleSaveTeachpoint}
                  style={{ width: '100%', padding: '6px', borderRadius: 4, background: '#1890ff', color: '#fff', border: 'none', cursor: 'pointer' }}
                >
                  Save Position
                </button>
              </div>
            )}
            
            {/* Teachpoints list */}
            <div style={{ maxHeight: 150, overflowY: 'auto' }}>
              {teachpoints.length === 0 ? (
                <div style={{ color: '#666', fontSize: '0.85em', fontStyle: 'italic' }}>
                  No teachpoints saved
                </div>
              ) : (
                teachpoints.map(tp => (
                  <div 
                    key={tp.id} 
                    style={{ 
                      display: 'flex', 
                      alignItems: 'center', 
                      justifyContent: 'space-between',
                      padding: '6px 8px',
                      marginBottom: 4,
                      background: '#222',
                      borderRadius: 4,
                      fontSize: '0.85em'
                    }}
                  >
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontWeight: 'bold', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {tp.name}
                      </div>
                      {tp.position && (
                        <div style={{ fontSize: '0.8em', color: '#888' }}>
                          X: {((tp.position.x || 0) * 1000).toFixed(1)} Y: {((tp.position.y || 0) * 1000).toFixed(1)}
                        </div>
                      )}
                    </div>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button
                        onClick={() => handleGotoTeachpoint(tp)}
                        disabled={!connected}
                        style={{ 
                          padding: '4px 8px', 
                          borderRadius: 4, 
                          background: connected ? '#1890ff' : '#444', 
                          color: '#fff', 
                          border: 'none', 
                          cursor: connected ? 'pointer' : 'not-allowed',
                          fontSize: '0.8em'
                        }}
                      >
                        Go
                      </button>
                      <button
                        onClick={() => handleDeleteTeachpoint(tp.id, tp.name)}
                        style={{ padding: '4px 8px', borderRadius: 4, background: '#ff4d4f', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.8em' }}
                      >
                        ✕
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Logs */}
          <div style={{ background: '#111', borderRadius: 4, padding: 8, fontSize: '0.75em', maxHeight: 120, overflowY: 'auto', flex: 1, minHeight: 0 }}>
            {logs.length === 0 ? (
              <div style={{ color: '#666', fontStyle: 'italic' }}>No logs yet</div>
            ) : (
              logs.map((l, i) => <div key={i}>{l}</div>)
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

export default PlanarMotorDiagnostics


import { useState, useEffect } from 'react'
import RobotViewer from './RobotViewer'

function PF400Diagnostics() {
  const [logs, setLogs] = useState([])
  const [joints, setJoints] = useState({})
  const [cartesian, setCartesian] = useState({})
  const [speedProfile, setSpeedProfile] = useState(2)

  // Step sizes (in meters for linear, radians for angular)
  const [stepZ, setStepZ] = useState(0.010)
  const [stepOut, setStepOut] = useState(0.010)
  const [stepRot, setStepRot] = useState(0.1745)
  const [stepGrip, setStepGrip] = useState(0.010)
  const [stepShoulder, setStepShoulder] = useState(0.0175)
  const [stepElbow, setStepElbow] = useState(0.0175)
  const [stepRail, setStepRail] = useState(0.010) // Rail step in meters (10mm default)

  // Teachpoints
  const [teachpoints, setTeachpoints] = useState([])
  const [newTpName, setNewTpName] = useState('')

  const API_URL = "http://localhost:3061"

  // Fetch joints periodically - uses recursive setTimeout to prevent request pileup
  useEffect(() => {
    let isMounted = true
    let timeoutId = null
    
    const fetchJoints = async () => {
      if (!isMounted) return
      
      try {
        const controller = new AbortController()
        const timeoutAbort = setTimeout(() => controller.abort(), 2000) // 2 second timeout
        
        const res = await fetch(`${API_URL}/joints`, { signal: controller.signal })
        clearTimeout(timeoutAbort)
        
        if (isMounted) {
          const data = await res.json()
          setJoints(data.joints || {})
          setCartesian(data.cartesian || {})
        }
      } catch (e) {
        // Silently ignore errors (connection issues, timeouts)
      }
      
      // Schedule next fetch only after current one completes
      if (isMounted) {
        timeoutId = setTimeout(fetchJoints, 500) // 500ms between successful fetches
      }
    }
    
    fetchJoints() // Start polling
    
    return () => {
      isMounted = false
      if (timeoutId) clearTimeout(timeoutId)
    }
  }, [])

  // Fetch teachpoints on mount and after changes
  const fetchTeachpoints = async () => {
    try {
      const res = await fetch(`${API_URL}/teachpoints`)
      const data = await res.json()
      setTeachpoints(data.teachpoints || [])
    } catch (e) {
      console.error('Failed to fetch teachpoints:', e)
    }
  }

  useEffect(() => {
    fetchTeachpoints()
  }, [])

  // Save current position as teachpoint
  const saveCurrentPosition = async () => {
    if (!newTpName.trim()) {
      log('✗ Enter a name for the teachpoint')
      return
    }
    try {
      const res = await fetch(`${API_URL}/teachpoints/save-current?name=${encodeURIComponent(newTpName)}&description=`, {
        method: 'POST'
      })
      const data = await res.json()
      if (res.ok) {
        log(`✓ Saved teachpoint: ${newTpName}`)
        setNewTpName('')
        fetchTeachpoints()
      } else {
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Move to teachpoint
  const moveToTeachpoint = async (tp) => {
    log(`→ Moving to ${tp.name}...`)
    try {
      const res = await fetch(`${API_URL}/teachpoints/move/${tp.id}?speed_profile=${speedProfile}`, {
        method: 'POST'
      })
      const data = await res.json()
      if (res.ok) {
        log(`✓ Arrived at ${tp.name}`)
      } else {
        log(`✗ Move failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Delete teachpoint
  const deleteTeachpoint = async (tp) => {
    if (!confirm(`Delete teachpoint "${tp.name}"?`)) return
    try {
      const res = await fetch(`${API_URL}/teachpoints/${tp.id}`, { method: 'DELETE' })
      if (res.ok) {
        log(`✓ Deleted: ${tp.name}`)
        fetchTeachpoints()
      } else {
        const data = await res.json()
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Update teachpoint with current position
  const updateTeachpoint = async (tp) => {
    if (!confirm(`Update "${tp.name}" with current position?`)) return
    try {
      // Pass the existing ID to update in place instead of creating a new one
      const params = new URLSearchParams({
        name: tp.name,
        description: tp.description || '',
        id: tp.id  // Critical: pass existing ID to replace, not create new
      })
      const res = await fetch(`${API_URL}/teachpoints/save-current?${params}`, {
        method: 'POST'
      })
      if (res.ok) {
        log(`✓ Updated: ${tp.name}`)
        fetchTeachpoints()
      } else {
        const data = await res.json()
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  // Rename teachpoint
  const renameTeachpoint = async (tp) => {
    const newName = prompt(`Rename "${tp.name}" to:`, tp.name)
    if (!newName || newName === tp.name) return
    try {
      const params = new URLSearchParams({ name: newName })
      const res = await fetch(`${API_URL}/teachpoints/${tp.id}/rename?${params}`, {
        method: 'PATCH'
      })
      if (res.ok) {
        log(`✓ Renamed: ${tp.name} → ${newName}`)
        fetchTeachpoints()
      } else {
        const data = await res.json()
        log(`✗ Failed: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  const sendJog = async (type, direction) => {
    let step = 0
    let payload = { speed_profile: speedProfile }

    if (type === 'z') { step = stepZ; payload.axis = 'z' }
    else if (type === 'out' || type === 'in') { step = stepOut; payload.axis = 'r' }
    else if (type === 'left' || type === 'right') { step = stepOut; payload.axis = 't' }
    else if (type === 'rot') { step = stepRot; payload.axis = 'yaw' }
    else if (type === 'grip') { step = stepGrip; payload.axis = 'gripper' }
    else if (type === 'shoulder') { step = stepShoulder; payload.joint = 2 }
    else if (type === 'elbow') { step = stepElbow; payload.joint = 3 }
    else if (type === 'rail') { step = stepRail; payload.joint = 6 }

    payload.distance = direction * step
    const distMm = (payload.distance * 1000).toFixed(1)
    log(`→ Jog ${type}: ${distMm}mm sending...`)
    
    try {
      const startTime = Date.now()
      const res = await fetch(`${API_URL}/jog`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      const elapsed = Date.now() - startTime
      const data = await res.json()
      
      if (!res.ok) {
        log(`✗ Jog FAILED: ${data.detail || res.status} (${elapsed}ms)`)
      } else {
        log(`✓ Jog ${type} complete (${elapsed}ms)`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  const log = (msg) => setLogs(prev => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev.slice(0, 14)])

  // Options for dropdowns
  const linearOpts = [{v: 0.0001, l: '0.1'}, {v: 0.001, l: '1'}, {v: 0.010, l: '10'}, {v: 0.050, l: '50'}]
  const angularOpts = [{v: 0.0017, l: '0.1'}, {v: 0.0175, l: '1'}, {v: 0.1745, l: '10'}, {v: 0.7854, l: '45'}]

  // Colors
  const colors = {
    zUp: '#69c0ff', zDown: '#0050b3',
    out: '#95de64', inC: '#237804',
    right: '#ff7875', left: '#a8071a',
    cw: '#b37feb', ccw: '#391085',
    gray: '#e8e8e8'
  }

  // Button style helper
  const btn = (bg, fg = '#000', size = 55) => ({
    width: size, height: size, borderRadius: '50%', margin: 4,
    backgroundColor: bg, color: fg, fontWeight: 'bold', fontSize: size > 45 ? '1.4em' : '1em',
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    boxShadow: '0 3px 6px rgba(0,0,0,0.4)', border: 'none', cursor: 'pointer'
  })

  const selectStyle = {
    padding: '6px', fontSize: '1em', fontWeight: 'bold', borderRadius: 4,
    backgroundColor: '#fff', color: '#000', border: '1px solid #888', width: 70
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', padding: 10, boxSizing: 'border-box' }}>
      <h1 style={{ margin: '0 0 10px 0', fontSize: '1.5em' }}>PF400 Robot Control</h1>

      <div style={{ display: 'flex', flex: 1, gap: 15, minHeight: 0 }}>
        
        {/* 3D VIEWER - Takes 70% of width */}
        <div style={{ flex: 7, minWidth: 0, border: '2px solid #444', borderRadius: 8, overflow: 'hidden' }}>
          <RobotViewer joints={joints} cartesian={cartesian} />
        </div>

        {/* CONTROLS - Takes 30% of width */}
        <div style={{ flex: 3, minWidth: 280, maxWidth: 400, display: 'flex', flexDirection: 'column', gap: 10 }}>
          
          {/* Speed */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, justifyContent: 'center' }}>
            <span style={{ fontWeight: 'bold' }}>Speed:</span>
            <select value={speedProfile} onChange={e => setSpeedProfile(+e.target.value)} style={selectStyle}>
              <option value={1}>Slow</option>
              <option value={2}>Medium</option>
              <option value={3}>Fast</option>
            </select>
          </div>

          {/* Main Jog Grid */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 8, padding: 10, background: '#222', borderRadius: 8 }}>
            
            {/* Col 1: Z */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepZ} onChange={e => setStepZ(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {linearOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={{ fontSize: '0.7em', marginBottom: 3 }}>Z (mm)</span>
              <button style={btn(colors.zUp)} onClick={() => sendJog('z', 1)}>▲</button>
              <div style={{ height: 50 }} />
              <button style={btn(colors.zDown, '#fff')} onClick={() => sendJog('z', -1)}>▼</button>
            </div>

            {/* Col 2: Out/In + L/R */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepOut} onChange={e => setStepOut(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {linearOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={{ fontSize: '0.7em', marginBottom: 3 }}>Out/In</span>
              <button style={btn(colors.out)} onClick={() => sendJog('out', 1)}>▲</button>
              <div style={{ display: 'flex', alignItems: 'center', gap: 3, margin: '5px 0' }}>
                <button style={btn(colors.left, '#fff', 40)} onClick={() => sendJog('left', 1)}>◄</button>
                <span style={{ fontSize: '0.6em' }}>L/R</span>
                <button style={btn(colors.right, '#000', 40)} onClick={() => sendJog('right', -1)}>►</button>
              </div>
              <button style={btn(colors.inC, '#fff')} onClick={() => sendJog('in', -1)}>▼</button>
            </div>

            {/* Col 3: CW/CCW */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepRot} onChange={e => setStepRot(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {angularOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={{ fontSize: '0.7em', marginBottom: 3 }}>Rot (°)</span>
              <button style={btn(colors.cw)} onClick={() => sendJog('rot', -1)}>↻</button>
              <div style={{ height: 50 }} />
              <button style={btn(colors.ccw, '#fff')} onClick={() => sendJog('rot', 1)}>↺</button>
            </div>

            {/* Col 4: Gripper */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepGrip} onChange={e => setStepGrip(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {linearOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={{ fontSize: '0.7em', marginBottom: 3 }}>Grip</span>
              <button style={btn(colors.gray, '#333')} onClick={() => sendJog('grip', -1)}>►◄</button>
              <span style={{ fontSize: '0.6em' }}>Close</span>
              <div style={{ height: 20 }} />
              <button style={btn(colors.gray, '#333')} onClick={() => sendJog('grip', 1)}>◄►</button>
              <span style={{ fontSize: '0.6em' }}>Open</span>
            </div>
          </div>

          {/* Joint Jogs */}
          <div style={{ background: '#222', borderRadius: 8, padding: 10 }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8 }}>Joint Jogs</div>
            
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, marginBottom: 8 }}>
              <span style={{ width: 60 }}>Shoulder</span>
              <select value={stepShoulder} onChange={e => setStepShoulder(+e.target.value)} style={{...selectStyle, width: 55}}>
                {angularOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <button style={btn(colors.left, '#fff', 35)} onClick={() => sendJog('shoulder', 1)}>-</button>
              <button style={btn(colors.right, '#000', 35)} onClick={() => sendJog('shoulder', -1)}>+</button>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, marginBottom: 8 }}>
              <span style={{ width: 60 }}>Elbow</span>
              <select value={stepElbow} onChange={e => setStepElbow(+e.target.value)} style={{...selectStyle, width: 55}}>
                {angularOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <button style={btn(colors.left, '#fff', 35)} onClick={() => sendJog('elbow', 1)}>-</button>
              <button style={btn(colors.right, '#000', 35)} onClick={() => sendJog('elbow', -1)}>+</button>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8 }}>
              <span style={{ width: 60 }}>Rail (J6)</span>
              <select value={stepRail} onChange={e => setStepRail(+e.target.value)} style={{...selectStyle, width: 55}}>
                {linearOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <button style={btn('#0066ff', '#fff', 35)} onClick={() => sendJog('rail', 1)}>◄</button>
              <button style={btn(colors.right, '#000', 35)} onClick={() => sendJog('rail', -1)}>►</button>
            </div>
          </div>

          {/* Teachpoints Panel */}
          <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10, flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column' }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>📍 Teachpoints</div>
            
            {/* Save current position */}
            <div style={{ display: 'flex', gap: 5, marginBottom: 10 }}>
              <input 
                type="text" 
                placeholder="New teachpoint name"
                value={newTpName}
                onChange={e => setNewTpName(e.target.value)}
                onKeyPress={e => e.key === 'Enter' && saveCurrentPosition()}
                style={{ flex: 1, padding: '6px 8px', borderRadius: 4, border: '1px solid #444', background: '#222', color: '#fff' }}
              />
              <button 
                onClick={saveCurrentPosition}
                style={{ padding: '6px 12px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
              >
                Save
              </button>
            </div>

            {/* Teachpoints list */}
            <div style={{ flex: 1, overflowY: 'auto', minHeight: 0 }}>
              {teachpoints.length === 0 ? (
                <div style={{ color: '#666', fontStyle: 'italic', textAlign: 'center', padding: 20 }}>
                  No teachpoints saved yet
                </div>
              ) : (
                teachpoints.map(tp => (
                  <div key={tp.id} style={{ 
                    background: '#222', 
                    borderRadius: 6, 
                    padding: 8, 
                    marginBottom: 6,
                    border: '1px solid #333'
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 }}>
                      <span style={{ fontWeight: 'bold', color: '#fff' }}>{tp.name}</span>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button 
                          onClick={() => moveToTeachpoint(tp)}
                          title="Move to this position"
                          style={{ padding: '3px 8px', borderRadius: 4, background: '#1890ff', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.85em' }}
                        >
                          Go
                        </button>
                        <button 
                          onClick={() => updateTeachpoint(tp)}
                          title="Update with current position"
                          style={{ padding: '3px 8px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.85em' }}
                        >
                          📍
                        </button>
                        <button 
                          onClick={() => renameTeachpoint(tp)}
                          title="Rename teachpoint"
                          style={{ padding: '3px 8px', borderRadius: 4, background: '#faad14', color: '#000', border: 'none', cursor: 'pointer', fontSize: '0.85em' }}
                        >
                          ✏️
                        </button>
                        <button 
                          onClick={() => deleteTeachpoint(tp)}
                          title="Delete teachpoint"
                          style={{ padding: '3px 8px', borderRadius: 4, background: '#ff4d4f', color: '#fff', border: 'none', cursor: 'pointer', fontSize: '0.85em' }}
                        >
                          🗑️
                        </button>
                      </div>
                    </div>
                    {/* Coordinates display */}
                    <div style={{ fontSize: '0.7em', color: '#888', fontFamily: 'monospace' }}>
                      {tp.cartesian && (
                        <div>XYZ: {tp.cartesian.x?.toFixed(1)}, {tp.cartesian.y?.toFixed(1)}, {tp.cartesian.z?.toFixed(1)} mm</div>
                      )}
                      {tp.joints && (
                        <div>J: [{tp.joints.slice(0, 4).map(j => j?.toFixed(1)).join(', ')}]</div>
                      )}
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Logs */}
          <div style={{ background: '#111', borderRadius: 4, padding: 8, fontSize: '0.75em', maxHeight: 120, overflowY: 'auto' }}>
            {logs.map((l, i) => <div key={i}>{l}</div>)}
          </div>
        </div>
      </div>
    </div>
  )
}

export default PF400Diagnostics


import { useState, useEffect, useLayoutEffect, useRef } from 'react'
import { useParams } from 'react-router-dom'
import RobotViewer from './RobotViewer'
import tangentApproachImg from '../../help/tangent-approach.png'

function PF400Diagnostics() {
  const { deviceName } = useParams()
  const DEVICE_NAME = deviceName || 'PF400-021'

  const [logs, setLogs] = useState([])
  const [joints, setJoints] = useState({})
  const [cartesian, setCartesian] = useState({})
  const [speedProfile, setSpeedProfile] = useState(2)

  // Per-device visualization config (from MongoDB)
  const [pf400VerticalScale, setPf400VerticalScale] = useState(1.85)
  const [pf400VertJogLimitM, setPf400VertJogLimitM] = useState(1.25)

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
  // Selected teachpoint for controls (dropdown)
  const [selectedTeachpointId, setSelectedTeachpointId] = useState('')
  const [tpFeatures, setTpFeatures] = useState({
    regrip_station: false,
    grip_orientation: 'landscape', // landscape|portrait
    tangent_approach_mm: '',
    z_above_mm: '',
    z_grasp_offset_range_mm: '', // "min-max" (string)
  })

  // Labware + Pick/Place
  const [labwareTypes, setLabwareTypes] = useState([])
  const [selectedLabwareId, setSelectedLabwareId] = useState('')
  const [pickTeachpointId, setPickTeachpointId] = useState('')
  const [placeTeachpointId, setPlaceTeachpointId] = useState('')
  const [pickPlaceOrientation, setPickPlaceOrientation] = useState('landscape') // landscape|portrait
  const [speedNoPlate, setSpeedNoPlate] = useState(1)
  const [speedHoldingPlate, setSpeedHoldingPlate] = useState(1)

  // Reachable devices for linking
  const [reachableDevices, setReachableDevices] = useState([])
  const [deviceTeachpoints, setDeviceTeachpoints] = useState({}) // teachpoints from other devices
  const [linkingTeachpoint, setLinkingTeachpoint] = useState(null)

  // IMPORTANT:
  // - When the UI is opened from another computer, "localhost" refers to the *user's* machine,
  //   not the robot host running the backend.
  // - Default to the current page hostname + backend port instead.
  const DEFAULT_API_URL = `${window.location.protocol}//${window.location.hostname}:8091`
  const ENV_API_URL = import.meta.env.VITE_API_URL
  // If VITE_API_URL is set to localhost but we're not browsing from localhost, ignore it.
  // This prevents "GUI button not triggering backend" when the UI is opened remotely.
  const API_URL = (ENV_API_URL && !(ENV_API_URL.includes('localhost') && window.location.hostname !== 'localhost'))
    ? ENV_API_URL
    : DEFAULT_API_URL

  // Load per-device config from MongoDB via backend `/devices`
  useEffect(() => {
    const loadDeviceConfig = async () => {
      try {
        const res = await fetch(`${API_URL}/devices`)
        if (!res.ok) return
        const data = await res.json()
        const device = data.devices?.find(d => d.name === DEVICE_NAME)
        if (!device) return

        // Scale: expects something like 1.85
        const scaleRaw = device.pf400_vertical_scale
        const scale = typeof scaleRaw === 'string' ? parseFloat(scaleRaw) : scaleRaw
        if (Number.isFinite(scale) && scale > 0.2 && scale < 10) {
          setPf400VerticalScale(scale)
        }

        // Jog limit: allow meters or mm (if value is large, assume mm)
        const limitRaw = device.pf400_vert_jog_limit
        let limit = typeof limitRaw === 'string' ? parseFloat(limitRaw) : limitRaw
        if (Number.isFinite(limit)) {
          if (limit > 10) limit = limit / 1000.0 // mm -> m
          if (limit > 0.2 && limit < 10) {
            setPf400VertJogLimitM(limit)
          }
        }
      } catch {
        // ignore
      }
    }
    loadDeviceConfig()
  }, [DEVICE_NAME])

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
      const tps = data.teachpoints || []
      setTeachpoints(tps)
      // Default selection for dropdown
      if (!selectedTeachpointId && tps.length) {
        setSelectedTeachpointId(tps[0].id)
      }
    } catch (e) {
      console.error('Failed to fetch teachpoints:', e)
    }
  }

  const parseRangeMm = (raw) => {
    const s = String(raw || '').trim()
    if (!s) return { min: null, max: null }
    // allow "3-10", "3 - 10", "3–10", "3 to 10"
    const cleaned = s.replace(/\s+/g, ' ').replace(/to/gi, '-').replace(/[–—]/g, '-')
    const parts = cleaned.split('-').map(p => p.trim()).filter(Boolean)
    if (parts.length === 1) {
      const v = Number(parts[0])
      if (!Number.isFinite(v)) return { min: null, max: null }
      return { min: v, max: v }
    }
    if (parts.length >= 2) {
      const a = Number(parts[0])
      const b = Number(parts[1])
      if (!Number.isFinite(a) || !Number.isFinite(b)) return { min: null, max: null }
      return { min: a, max: b }
    }
    return { min: null, max: null }
  }

  const formatRangeMm = (min, max) => {
    const a = (min === null || min === undefined || min === '') ? null : Number(min)
    const b = (max === null || max === undefined || max === '') ? null : Number(max)
    if (!Number.isFinite(a) && !Number.isFinite(b)) return ''
    if (Number.isFinite(a) && Number.isFinite(b)) return `${a}-${b}`
    if (Number.isFinite(a)) return `${a}-${a}`
    if (Number.isFinite(b)) return `${b}-${b}`
    return ''
  }

  const loadTeachpointFeaturesIntoForm = (tp) => {
    const f = tp?.features || {}
    setTpFeatures({
      regrip_station: !!f.regrip_station,
      grip_orientation: (f.grip_orientation || 'landscape'),
      tangent_approach_mm: (f.tangent_approach_mm ?? ''),
      z_above_mm: (f.z_above_mm ?? ''),
      z_grasp_offset_range_mm: formatRangeMm(f.z_grasp_offset_min_mm, f.z_grasp_offset_max_mm),
    })
  }

  const saveTeachpointFeatures = async () => {
    if (!selectedTeachpointId) return
    try {
      const zRange = parseRangeMm(tpFeatures.z_grasp_offset_range_mm)
      const payload = {
        regrip_station: !!tpFeatures.regrip_station,
        grip_orientation: tpFeatures.grip_orientation,
        tangent_approach_mm: tpFeatures.tangent_approach_mm === '' ? null : Number(tpFeatures.tangent_approach_mm),
        z_above_mm: tpFeatures.z_above_mm === '' ? null : Number(tpFeatures.z_above_mm),
        z_grasp_offset_min_mm: zRange.min,
        z_grasp_offset_max_mm: zRange.max,
      }

      const res = await fetch(`${API_URL}/teachpoints/${encodeURIComponent(selectedTeachpointId)}/features`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        throw new Error(data.detail || `HTTP ${res.status}`)
      }
      log(`✓ Saved features for teachpoint`)
      fetchTeachpoints()
    } catch (e) {
      log(`✗ Failed to save features: ${e.message}`)
    }
  }

  const selectedTeachpoint = teachpoints.find(tp => tp.id === selectedTeachpointId) || null

  // Keep form in sync when switching the teachpoint dropdown
  useEffect(() => {
    if (selectedTeachpoint) loadTeachpointFeaturesIntoForm(selectedTeachpoint)
  }, [selectedTeachpointId])

  const startLinkingSelectedTeachpoint = () => {
    if (!selectedTeachpoint) return
    startLinking(selectedTeachpoint)
  }

  // Fetch reachable devices and their teachpoints
  const fetchReachableDevices = async () => {
    try {
      // First get reachable devices
      const reachableRes = await fetch(`${API_URL}/devices/${encodeURIComponent(DEVICE_NAME)}/reachable`)
      if (reachableRes.ok) {
        const reachableData = await reachableRes.json()
        setReachableDevices(reachableData.reachable_devices || [])

        // Then fetch teachpoints for each reachable device
        const deviceTps = {}
        for (const device of reachableData.reachable_devices || []) {
          try {
            const tpRes = await fetch(`${API_URL}/devices/${device.device_name}/teachpoints`)
            if (tpRes.ok) {
              const tpData = await tpRes.json()
              // Add device_name to each teachpoint for linking
              const teachpointsWithDevice = (tpData.teachpoints || []).map(tp => ({
                ...tp,
                device_name: device.device_name
              }))
              deviceTps[device.device_name] = teachpointsWithDevice
            }
          } catch (e) {
            console.error(`Failed to fetch teachpoints for ${device.device_name}:`, e)
          }
        }
        setDeviceTeachpoints(deviceTps)
      }
    } catch (e) {
      console.error('Failed to fetch reachable devices:', e)
    }
  }

  useEffect(() => {
    fetchTeachpoints()
    fetchReachableDevices()
  }, [])

  // Fetch labware types
  const fetchLabwareTypes = async () => {
    try {
      const res = await fetch(`${API_URL}/labware/types`)
      const data = await res.json().catch(() => ({}))
      if (!res.ok) return
      const types = (data.labware_types || []).slice()
      types.sort((a, b) => String(a?.name || '').localeCompare(String(b?.name || '')))
      setLabwareTypes(types)
      if (!selectedLabwareId && types.length) setSelectedLabwareId(types[0].labware_type_id)
    } catch {
      // ignore
    }
  }

  useEffect(() => {
    fetchLabwareTypes()
  }, [])

  // Default pick/place teachpoints (first two) for convenience
  useEffect(() => {
    if (!teachpoints.length) return
    if (!pickTeachpointId) setPickTeachpointId(teachpoints[0]?.id || '')
    if (!placeTeachpointId) setPlaceTeachpointId(teachpoints[1]?.id || teachpoints[0]?.id || '')
  }, [teachpoints])

  const selectedLabware = labwareTypes.find(l => l.labware_type_id === selectedLabwareId) || null

  const pf400Widths = () => {
    const pf = selectedLabware?.pf400 || {}
    const orient = String(pickPlaceOrientation || 'landscape').toLowerCase()
    const open = orient === 'portrait' ? pf.portrait_open_width_mm : pf.landscape_open_width_mm
    const closed = orient === 'portrait' ? pf.portrait_closed_width_mm : pf.landscape_closed_width_mm
    return { open, closed }
  }

  const setGripperAbsolute = async (mm) => {
    if (!Number.isFinite(Number(mm))) {
      log('✗ Labware PF400 open/closed width not set')
      return
    }
    try {
      const res = await fetch(`${API_URL}/gripper/set`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ gripper_mm: Number(mm), speed_profile: speedProfile }),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        log(`✗ Gripper set failed: ${data.detail || res.status}`)
      } else {
        log(`✓ Gripper set: ${Number(mm).toFixed(2)}mm`)
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  const runPickPlace = async () => {
    if (!selectedLabwareId) return log('✗ Select a labware first')
    if (!pickTeachpointId || !placeTeachpointId) return log('✗ Select pick and place teachpoints')
    const { open, closed } = pf400Widths()
    if (!Number.isFinite(Number(open)) || !Number.isFinite(Number(closed))) {
      return log('✗ Labware PF400 open/closed widths are not set for this orientation')
    }
    log(`→ Pick&Place (${pickPlaceOrientation}) starting...`)
    try {
      const reqBody = {
        labware_type_id: selectedLabwareId,
        pick_teachpoint_id: pickTeachpointId,
        place_teachpoint_id: placeTeachpointId,
        orientation: pickPlaceOrientation,
        speed_no_plate: speedNoPlate,
        speed_holding_plate: speedHoldingPlate,
        pause_seconds: 0.35,
      }
      log(`  API: ${API_URL}/pf400/pick-place`)
      log(`  Req: pick=${pickTeachpointId}, place=${placeTeachpointId}, labware=${selectedLabwareId}, speeds=${speedNoPlate}/${speedHoldingPlate}`)
      const res = await fetch(`${API_URL}/pf400/pick-place`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(reqBody),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        log(`✗ Pick&Place failed: ${data.detail || res.status}`)
      } else {
        const steps = data.steps || []
        if (steps.length) {
          const fmt = (v) => (v === null || v === undefined) ? '—' : Number(v).toFixed(1)
          for (const s of steps) {
            if (s.step?.includes('open') || s.step?.includes('close')) {
              log(`  ${s.step}: ${fmt(s.gripper_mm_before)} → ${fmt(s.gripper_mm_after)} mm (target ${fmt(s.target_gripper_mm)} mm)`)
            }
          }
        }
        log('✓ Pick&Place complete')
      }
    } catch (e) {
      log(`✗ Error: ${e.message}`)
    }
  }

  const swapPickPlaceTeachpoints = () => {
    if (!pickTeachpointId && !placeTeachpointId) return
    setPickTeachpointId(placeTeachpointId)
    setPlaceTeachpointId(pickTeachpointId)
    log('↔ Swapped Pick/Place teachpoints')
  }

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

  const startLinking = (localTp) => {
    setLinkingTeachpoint(localTp)
    log(`🔗 Select a teachpoint from reachable devices to link with ${localTp.name}`)
  }

  const completeLinking = async (targetTp) => {
    if (!linkingTeachpoint) return

    // Link the selected local teachpoint with the target teachpoint
    try {
      const res = await fetch(`${API_URL}/devices/${encodeURIComponent(DEVICE_NAME)}/teachpoints/link`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          source_teachpoint_id: linkingTeachpoint.id,
          target_device: targetTp.device_name || 'unknown',
          target_teachpoint_id: targetTp.id,
          transfer_type: 'handoff'
        })
      })

      if (res.ok) {
        log(`🔗 Linked: ${linkingTeachpoint.name} ↔ ${targetTp.name}`)
        setLinkingTeachpoint(null)
        fetchTeachpoints() // Refresh to show linked status
        fetchReachableDevices() // Refresh device teachpoints
      } else {
        const data = await res.json()
        log(`✗ Failed to link: ${data.detail}`)
      }
    } catch (e) {
      log(`✗ Error linking teachpoints: ${e.message}`)
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

  const moveToSafe = async () => {
    log(`→ Safe: sending...`)
    try {
      const startTime = Date.now()
      const res = await fetch(`${API_URL}/pf400/safe`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ speed_profile: speedProfile }),
      })
      const elapsed = Date.now() - startTime
      const data = await res.json().catch(() => ({}))
      if (!res.ok) {
        log(`✗ Safe FAILED: ${data.detail || data.message || res.status} (${elapsed}ms)`)
      } else {
        log(`✓ Safe complete (${elapsed}ms)`)
      }
    } catch (e) {
      log(`✗ Safe error: ${e.message}`)
    }
  }

  const log = (msg) => setLogs(prev => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev.slice(0, 14)])

  const HoverPopover = ({ children, content, width = 520 }) => {
    const anchorRef = useRef(null)
    const popoverRef = useRef(null)
    const closeTimerRef = useRef(null)
    const [open, setOpen] = useState(false)
    // Start off-screen so we never "flash" at (0,0) while measuring/placing.
    const [pos, setPos] = useState({ top: -9999, left: -9999 })
    const [popoverHeight, setPopoverHeight] = useState(320)

    const place = () => {
      const el = anchorRef.current
      if (!el) return
      const r = el.getBoundingClientRect()
      const desiredTop = r.top + r.height / 2

      // Prefer placing to the right of the label. If there's not enough room,
      // place to the left so the popover doesn't "jump under the cursor" and blink.
      const placeRight = (r.right + 12 + width) <= window.innerWidth
      const desiredLeft = placeRight ? (r.right + 12) : (r.left - width - 12)
      const left = Math.min(Math.max(desiredLeft, 12), window.innerWidth - width - 12)

      // We're using translateY(-50%), so clamp based on half the popover height.
      const halfH = Math.max(80, (popoverHeight || 320) / 2)
      const top = Math.min(Math.max(desiredTop, 12 + halfH), window.innerHeight - 12 - halfH)
      setPos({ top, left })
    }

    const cancelClose = () => {
      if (closeTimerRef.current) {
        clearTimeout(closeTimerRef.current)
        closeTimerRef.current = null
      }
    }

    const scheduleClose = () => {
      cancelClose()
      closeTimerRef.current = setTimeout(() => setOpen(false), 120)
    }

    const handleEnter = () => {
      cancelClose()
      setOpen(true)
    }

    const handleLeave = (e) => {
      const rt = e?.relatedTarget
      // If we're moving between the anchor and the popover, don't close.
      if (rt) {
        if (anchorRef.current && anchorRef.current.contains(rt)) return
        if (popoverRef.current && popoverRef.current.contains(rt)) return
      }
      scheduleClose()
    }

    useLayoutEffect(() => {
      if (!open) return
      // Layout effect runs before paint, so we can measure + position without a visible flash.
      const el = popoverRef.current
      if (el) {
        const h = el.getBoundingClientRect().height
        if (h && Math.abs(h - popoverHeight) > 2) setPopoverHeight(h)
      }
      place()
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [open, width, popoverHeight])

    useEffect(() => {
      return () => cancelClose()
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [])

    return (
      <div
        ref={anchorRef}
        onMouseEnter={handleEnter}
        onMouseLeave={handleLeave}
        style={{ display: 'inline-block' }}
      >
        {children}
        {open && (
          <div
            ref={popoverRef}
            onMouseEnter={handleEnter}
            onMouseLeave={handleLeave}
            style={{
              position: 'fixed',
              top: pos.top,
              left: pos.left,
              transform: 'translateY(-50%)',
              zIndex: 9999,
              width,
              background: '#0e0e14',
              border: '1px solid #333',
              borderRadius: 10,
              padding: 10,
              boxShadow: '0 10px 30px rgba(0,0,0,0.65)',
              // Make the popover hoverable so it doesn't flicker when it overlaps the cursor.
              pointerEvents: 'auto',
            }}
          >
            {content}
          </div>
        )}
      </div>
    )
  }

  const helpStyles = {
    svgStyle: { width: '100%', height: 'auto', display: 'block' },
    label: { fontSize: 12, fill: '#ddd' },
    muted: { fontSize: 11, fill: '#999' },
    stroke: { stroke: '#69c0ff', strokeWidth: 2, fill: 'none' },
    plate: { stroke: '#ddd', strokeWidth: 2, fill: '#222' },
    finger: { stroke: '#faad14', strokeWidth: 2, fill: '#111' },
    highlight: { stroke: '#52c41a', strokeWidth: 3, fill: 'none' },
  }

  const HelpGripOrientation = () => (
    <div>
      <div style={{ color: '#ddd', fontWeight: 'bold', marginBottom: 6 }}>Grip Orientation: Landscape vs Portrait</div>
      <svg viewBox="0 0 520 140" style={helpStyles.svgStyle}>
        <text x="10" y="18" style={helpStyles.muted}>Landscape: fingers grip the long side</text>
        <rect x="140" y="30" width="240" height="40" rx="4" style={helpStyles.plate} />
        <rect x="95" y="35" width="35" height="30" rx="14" style={helpStyles.finger} />
        <rect x="390" y="35" width="35" height="30" rx="14" style={helpStyles.finger} />
        <rect x="92" y="32" width="330" height="36" rx="6" style={helpStyles.highlight} />

        <text x="10" y="98" style={helpStyles.muted}>Portrait: fingers grip the short side</text>
        <rect x="240" y="105" width="40" height="24" rx="4" style={helpStyles.plate} />
        <rect x="220" y="100" width="22" height="34" rx="10" style={helpStyles.finger} />
        <rect x="278" y="100" width="22" height="34" rx="10" style={helpStyles.finger} />
        <rect x="218" y="98" width="84" height="38" rx="6" style={helpStyles.highlight} />
      </svg>
      <div style={{ color: '#999', fontSize: 12, marginTop: 6 }}>
        Pick the direction the fingers squeeze: long-side grip = landscape, short-side grip = portrait.
      </div>
    </div>
  )

  // Access (Vertical/Horizontal) removed. Use Z Above: set to 0 for direct moves.

  const HelpTangentApproach = () => (
    <div>
      <div style={{ color: '#ddd', fontWeight: 'bold', marginBottom: 6 }}>Tangent Approach (mm)</div>
      <div style={{ color: '#999', fontSize: 12, marginBottom: 10 }}>
        The robot approaches from a global tangent direction and “locks” the wrist orientation for the last Tangent Approach distance.
      </div>
      <img
        src={tangentApproachImg}
        alt="Tangent Approach diagram"
        style={{ width: '100%', height: 'auto', display: 'block', borderRadius: 8, border: '1px solid #333' }}
      />
      <div style={{ color: '#999', fontSize: 12, marginTop: 8 }}>
        Tip: set Tangent Approach to 0 to disable.
      </div>
    </div>
  )

  const HelpZAboveAndOffset = () => (
    <div>
      <div style={{ color: '#ddd', fontWeight: 'bold', marginBottom: 6 }}>Z Above + Z Grasp Offset</div>
      <div style={{ color: '#999', fontSize: 12, marginBottom: 6 }}>
        Z Above is the “safe height above” the teachpoint. Set Z Above = 0 for a direct move (no approach/retract).
        Z Grasp Offset is an allowed Z window around the teachpoint during pick/place (stored now; full math later).
      </div>
      <svg viewBox="0 0 520 190" style={helpStyles.svgStyle}>
        <rect x="40" y="150" width="440" height="18" fill="#1a2e1a" stroke="#2f6f2f" />
        <text x="50" y="145" style={helpStyles.muted}>Surface / deck</text>

        <circle cx="260" cy="140" r="5" fill="#52c41a" />
        <text x="270" y="144" style={helpStyles.label}>Teachpoint Z</text>

        <path d="M260 140 L260 60" style={helpStyles.stroke} />
        <path d="M250 60 L270 60" style={helpStyles.stroke} />
        <text x="280" y="70" style={helpStyles.label}>Z Above</text>

        <rect x="235" y="110" width="50" height="60" fill="none" stroke="#faad14" strokeWidth="2" strokeDasharray="6 4" />
        <text x="292" y="125" style={helpStyles.label}>Z Grasp Offset</text>
        <text x="292" y="140" style={helpStyles.muted}>(min..max)</text>
      </svg>
    </div>
  )

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

  // Fixed row heights so the big circular buttons line up across columns
  const buttonRowStyle = { height: 70, display: 'flex', alignItems: 'center', justifyContent: 'center' }
  const midRowStyle = { height: 70, display: 'flex', alignItems: 'center', justifyContent: 'center' }
  const labelStyle = { fontSize: '0.7em', marginBottom: 3, height: 16, display: 'flex', alignItems: 'center' }

  const selectStyle = {
    padding: '6px', fontSize: '1em', fontWeight: 'bold', borderRadius: 4,
    backgroundColor: '#fff', color: '#000', border: '1px solid #888', width: 70
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', padding: 10, boxSizing: 'border-box' }}>
      <h1 style={{ margin: '0 0 10px 0', fontSize: '1.5em' }}>PF400 Robot Control</h1>

      {/* Layout: shrink 3D view column so side panels have room */}
      <div
        style={{
          display: 'grid',
          flex: 1,
          gap: 15,
          minHeight: 0,
          // Left + Center + Right
          // Center is intentionally clamped to be much smaller (≈60% shrink vs "fill remaining").
          gridTemplateColumns: 'minmax(300px, 380px) minmax(320px, 38vw) minmax(380px, 560px)',
        }}
      >
        {/* LEFT SIDEBAR: device linking + logs */}
        <div style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: 10, minHeight: 0 }}>
          {/* Device Linking */}
          <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10, overflow: 'hidden' }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>Device Linking</div>

            {/* Reachable Devices */}
            <div style={{ marginBottom: 12 }}>
              <div style={{ fontSize: '0.9em', color: '#ccc', marginBottom: 4 }}>Reachable Devices:</div>
              {reachableDevices.length === 0 ? (
                <div style={{ fontSize: '0.8em', color: '#666', fontStyle: 'italic' }}>
                  No reachable devices configured
                </div>
              ) : (
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                  {reachableDevices.map(device => (
                    <div key={device.device_name} style={{
                      background: '#2a2a3e',
                      borderRadius: 4,
                      padding: '4px 8px',
                      fontSize: '0.8em',
                      color: '#69c0ff'
                    }}>
                      {device.device_name} ({device.access_type})
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Teachpoint Linking */}
            {reachableDevices.length > 0 && (
              <div>
                <div style={{ fontSize: '0.9em', color: '#ccc', marginBottom: 6 }}>
                  {linkingTeachpoint
                    ? `Select target teachpoint to link with "${linkingTeachpoint.name}":`
                    : 'Link Teachpoints: choose a local teachpoint and click "Start linking"'
                  }
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '110px 1fr', gap: 8, alignItems: 'center', marginBottom: 8 }}>
                  <div style={{ color: '#bbb', textAlign: 'right', fontSize: '0.85em' }}>Local TP</div>
                  <div style={{ display: 'flex', gap: 6 }}>
                    <select
                      value={selectedTeachpointId}
                      onChange={(e) => setSelectedTeachpointId(e.target.value)}
                      style={{ ...selectStyle, width: '100%' }}
                    >
                      {teachpoints.map(tp => (
                        <option key={tp.id} value={tp.id}>{tp.name}</option>
                      ))}
                    </select>
                    <button
                      onClick={startLinkingSelectedTeachpoint}
                      disabled={!teachpoints.length}
                      style={{
                        padding: '6px 10px',
                        borderRadius: 4,
                        background: teachpoints.length ? (linkingTeachpoint ? '#faad14' : '#722ed1') : '#444',
                        color: '#fff',
                        border: 'none',
                        cursor: teachpoints.length ? 'pointer' : 'not-allowed',
                        fontWeight: 'bold',
                      }}
                      title="Start linking using the selected local teachpoint"
                    >
                      {linkingTeachpoint ? 'Change' : 'Start linking'}
                    </button>
                  </div>
                </div>
                <div style={{ maxHeight: 320, overflowY: 'auto' }}>
                  {Object.entries(deviceTeachpoints).map(([deviceName, deviceTps]) => (
                    <div key={deviceName} style={{ marginBottom: 8 }}>
                      <div style={{ fontSize: '0.8em', color: '#faad14', marginBottom: 4 }}>
                        {deviceName} teachpoints:
                      </div>
                      {deviceTps.map(tp => (
                        <div key={tp.id} style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          background: '#2a2a3e',
                          borderRadius: 4,
                          padding: '4px 8px',
                          marginBottom: 2,
                          fontSize: '0.8em'
                        }}>
                          <span>{tp.name}</span>
                          <button
                            onClick={() => completeLinking(tp)}
                            disabled={!linkingTeachpoint}
                            style={{
                              padding: '2px 6px',
                              borderRadius: 3,
                              background: linkingTeachpoint ? '#52c41a' : '#666',
                              color: '#fff',
                              border: 'none',
                              cursor: linkingTeachpoint ? 'pointer' : 'not-allowed',
                              fontSize: '0.7em'
                            }}
                            title={linkingTeachpoint ? `Link "${linkingTeachpoint.name}" with "${tp.name}"` : "Select a local teachpoint first"}
                          >
                            Link
                          </button>
                        </div>
                      ))}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Logs */}
          <div style={{ background: '#111', borderRadius: 8, padding: 10, fontSize: '0.75em', overflowY: 'auto', flex: 1, minHeight: 0, textAlign: 'left', fontFamily: 'monospace', whiteSpace: 'pre-wrap' }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>Logs</div>
            {logs.length === 0 ? (
              <div style={{ color: '#666', fontStyle: 'italic' }}>No logs yet</div>
            ) : (
              logs.map((l, i) => <div key={i}>{l}</div>)
            )}
          </div>
        </div>

        {/* CENTER: 3D viewer */}
        <div style={{ width: '100%', minWidth: 0, display: 'flex', flexDirection: 'column', gap: 10, minHeight: 0 }}>
          {/* Top: 3D viewer (about half height) */}
          <div style={{ flex: 1, minHeight: 0, border: '2px solid #444', borderRadius: 8, overflow: 'hidden' }}>
            <RobotViewer
              joints={joints}
              cartesian={cartesian}
              verticalScale={pf400VerticalScale}
              vertJogLimitM={pf400VertJogLimitM}
            />
          </div>

          {/* Bottom: Teachpoints (about half height) */}
          <div style={{ flex: 1, minHeight: 0, background: '#1a1a2e', borderRadius: 8, padding: 10, display: 'flex', flexDirection: 'column' }}>
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

            {/* Teachpoints dropdown + selected teachpoint details */}
            <div style={{ background: '#111', borderRadius: 8, padding: 10, border: '1px solid #333' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '110px 1fr', gap: 8, alignItems: 'center', marginBottom: 8 }}>
                <div style={{ color: '#bbb', textAlign: 'right' }}>Teachpoint</div>
                <select
                  value={selectedTeachpointId}
                  onChange={(e) => {
                    const id = e.target.value
                    setSelectedTeachpointId(id)
                  }}
                  style={{ ...selectStyle, width: '100%' }}
                >
                  {teachpoints.length === 0 ? (
                    <option value="">No teachpoints</option>
                  ) : (
                    teachpoints.map(tp => (
                      <option key={tp.id} value={tp.id}>{tp.name}</option>
                    ))
                  )}
                </select>
              </div>

              {!selectedTeachpoint ? (
                <div style={{ color: '#666', fontStyle: 'italic', textAlign: 'center', padding: 10 }}>
                  No teachpoint selected
                </div>
              ) : (
                <>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                    <div style={{ fontWeight: 'bold', color: '#fff' }}>
                      {selectedTeachpoint.name}
                      {(selectedTeachpoint.linked_to || selectedTeachpoint.linked_from) && <span style={{ marginLeft: 6, color: '#52c41a' }}>🔗 linked</span>}
                    </div>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button
                        onClick={() => moveToTeachpoint(selectedTeachpoint)}
                        title="Move to this position"
                        style={{ padding: '4px 10px', borderRadius: 4, background: '#1890ff', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                      >
                        Go
                      </button>
                      <button
                        onClick={() => updateTeachpoint(selectedTeachpoint)}
                        title="Update with current position"
                        style={{ padding: '4px 10px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                      >
                        Update
                      </button>
                      <button
                        onClick={startLinkingSelectedTeachpoint}
                        disabled={!reachableDevices.length}
                        title={reachableDevices.length ? "Start linking this teachpoint to another device" : "No reachable devices available"}
                        style={{
                          padding: '4px 10px',
                          borderRadius: 4,
                          background: reachableDevices.length ? (linkingTeachpoint?.id === selectedTeachpoint.id ? '#faad14' : '#722ed1') : '#444',
                          color: '#fff',
                          border: 'none',
                          cursor: reachableDevices.length ? 'pointer' : 'not-allowed',
                          fontWeight: 'bold'
                        }}
                      >
                        {linkingTeachpoint?.id === selectedTeachpoint.id ? 'Linking…' : 'Link'}
                      </button>
                      <button
                        onClick={() => renameTeachpoint(selectedTeachpoint)}
                        title="Rename teachpoint"
                        style={{ padding: '4px 10px', borderRadius: 4, background: '#faad14', color: '#000', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                      >
                        Rename
                      </button>
                      <button
                        onClick={() => deleteTeachpoint(selectedTeachpoint)}
                        title="Delete teachpoint"
                        style={{ padding: '4px 10px', borderRadius: 4, background: '#ff4d4f', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                      >
                        Delete
                      </button>
                    </div>
                  </div>

                  <div style={{ fontSize: '0.8em', color: '#bbb', fontFamily: 'monospace' }}>
                    {selectedTeachpoint.cartesian && (
                      <div>XYZ: {selectedTeachpoint.cartesian.x?.toFixed(1)}, {selectedTeachpoint.cartesian.y?.toFixed(1)}, {selectedTeachpoint.cartesian.z?.toFixed(1)} mm</div>
                    )}
                    {selectedTeachpoint.joints && (
                      <div>J: [{selectedTeachpoint.joints.slice(0, 6).map(j => j?.toFixed(1)).join(', ')}]</div>
                    )}
                  </div>

                  {/* Inline features editor (always visible) */}
                  <div style={{ marginTop: 10, background: '#0e0e14', borderRadius: 8, padding: 10, border: '1px solid #333' }}>
                    <div style={{ fontWeight: 'bold', color: '#ddd', marginBottom: 8 }}>Orientation Features</div>

                    <div style={{ display: 'flex', gap: 10, alignItems: 'center', flexWrap: 'wrap' }}>
                      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                        <div style={{ width: 120, color: '#bbb', textAlign: 'right' }}>Regrip Station</div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#ddd' }}>
                          <input
                            type="checkbox"
                            checked={!!tpFeatures.regrip_station}
                            onChange={(e) => setTpFeatures(p => ({ ...p, regrip_station: e.target.checked }))}
                          />
                          <span>Enabled</span>
                        </div>
                      </div>

                      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                        <HoverPopover content={<HelpGripOrientation />} width={520}>
                          <div style={{ width: 120, color: '#bbb', textAlign: 'right', cursor: 'help', textDecoration: 'underline dotted', textUnderlineOffset: 3 }}>
                            Grip Orientation
                          </div>
                        </HoverPopover>
                        <select
                          value={tpFeatures.grip_orientation}
                          onChange={(e) => setTpFeatures(p => ({ ...p, grip_orientation: e.target.value }))}
                          style={{ ...selectStyle, width: 140, minWidth: 140 }}
                        >
                          <option value="landscape">Landscape</option>
                          <option value="portrait">Portrait</option>
                        </select>
                      </div>

                      {/* Access removed: use Z Above (0 = direct) */}
                    </div>

                    <div style={{ fontWeight: 'bold', margin: '12px 0 8px', color: '#ddd' }}>Approach Values (mm)</div>
                    <div style={{ display: 'grid', gridTemplateColumns: '160px 1fr 160px 1fr', gap: 8, alignItems: 'center' }}>
                      <HoverPopover content={<HelpTangentApproach />} width={520}>
                        <div style={{ color: '#bbb', textAlign: 'right', cursor: 'help', textDecoration: 'underline dotted', textUnderlineOffset: 3 }}>
                          Tangent Approach
                        </div>
                      </HoverPopover>
                      <input
                        type="number"
                        step="0.001"
                        value={tpFeatures.tangent_approach_mm}
                        onChange={(e) => setTpFeatures(p => ({ ...p, tangent_approach_mm: e.target.value }))}
                        style={{ padding: '6px 8px', borderRadius: 4, border: '1px solid #444', background: '#222', color: '#fff' }}
                        placeholder="e.g. 160.0"
                      />

                      <HoverPopover content={<HelpZAboveAndOffset />} width={520}>
                        <div style={{ color: '#bbb', textAlign: 'right', cursor: 'help', textDecoration: 'underline dotted', textUnderlineOffset: 3 }}>
                          Z Above
                        </div>
                      </HoverPopover>
                      <input
                        type="number"
                        step="0.001"
                        value={tpFeatures.z_above_mm}
                        onChange={(e) => setTpFeatures(p => ({ ...p, z_above_mm: e.target.value }))}
                        style={{ padding: '6px 8px', borderRadius: 4, border: '1px solid #444', background: '#222', color: '#fff' }}
                        placeholder="0 = direct"
                      />

                      <HoverPopover content={<HelpZAboveAndOffset />} width={520}>
                        <div style={{ color: '#bbb', textAlign: 'right', cursor: 'help', textDecoration: 'underline dotted', textUnderlineOffset: 3 }}>
                          Z Grasp Offset Range
                        </div>
                      </HoverPopover>
                      <input
                        type="text"
                        value={tpFeatures.z_grasp_offset_range_mm}
                        onChange={(e) => setTpFeatures(p => ({ ...p, z_grasp_offset_range_mm: e.target.value }))}
                        style={{ padding: '6px 8px', borderRadius: 4, border: '1px solid #444', background: '#222', color: '#fff' }}
                        placeholder="e.g. 3-10"
                      />
                    </div>

                    <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 10 }}>
                      <button
                        onClick={saveTeachpointFeatures}
                        style={{ padding: '6px 12px', borderRadius: 4, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                      >
                        Save Features
                      </button>
                    </div>
                  </div>

                </>
              )}
            </div>
          </div>
        </div>

        {/* RIGHT SIDEBAR: speed + jogs + teachpoints */}
        <div style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: 10, minHeight: 0 }}>
          {/* Labware + Motion actions */}
          <div style={{ background: '#1a1a2e', borderRadius: 8, padding: 10 }}>
            <div style={{ fontWeight: 'bold', marginBottom: 8, color: '#69c0ff' }}>Labware + Motion</div>

            <div style={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: 8, alignItems: 'center' }}>
              <div style={{ color: '#bbb', textAlign: 'right' }}>Labware</div>
              <select
                value={selectedLabwareId}
                onChange={(e) => setSelectedLabwareId(e.target.value)}
                style={{ ...selectStyle, width: '100%' }}
              >
                {labwareTypes.map(l => (
                  <option key={l.labware_type_id} value={l.labware_type_id}>{l.name}</option>
                ))}
              </select>

              <div style={{ color: '#bbb', textAlign: 'right' }}>Orientation</div>
              <select
                value={pickPlaceOrientation}
                onChange={(e) => setPickPlaceOrientation(e.target.value)}
                style={{ ...selectStyle, width: '100%' }}
              >
                <option value="landscape">Landscape</option>
                <option value="portrait">Portrait</option>
              </select>

              <div style={{ color: '#bbb', textAlign: 'right' }}>Pick TP</div>
              <select
                value={pickTeachpointId}
                onChange={(e) => setPickTeachpointId(e.target.value)}
                style={{ ...selectStyle, width: '100%' }}
              >
                {teachpoints.map(tp => (
                  <option key={tp.id} value={tp.id}>{tp.name}</option>
                ))}
              </select>

              <div style={{ color: '#bbb', textAlign: 'right' }}>Place TP</div>
              <select
                value={placeTeachpointId}
                onChange={(e) => setPlaceTeachpointId(e.target.value)}
                style={{ ...selectStyle, width: '100%' }}
              >
                {teachpoints.map(tp => (
                  <option key={tp.id} value={tp.id}>{tp.name}</option>
                ))}
              </select>

              <div style={{ gridColumn: '1 / -1', display: 'flex', justifyContent: 'flex-end', marginTop: 2 }}>
                <button
                  style={{ padding: '6px 10px', borderRadius: 6, background: '#2f54eb', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold' }}
                  onClick={swapPickPlaceTeachpoints}
                  title="Swap the selected Pick and Place teachpoints"
                >
                  Swap Pick/Place
                </button>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginTop: 10 }}>
              <div style={{ background: '#222', borderRadius: 6, padding: 8 }}>
                <div style={{ fontWeight: 'bold', marginBottom: 6, color: '#ddd' }}>Speed</div>
                <div style={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: 6, alignItems: 'center' }}>
                  <div style={{ color: '#bbb', textAlign: 'right', fontSize: '0.9em' }}>No plate</div>
                  <select value={speedNoPlate} onChange={(e) => setSpeedNoPlate(+e.target.value)} style={{ ...selectStyle, width: '100%' }}>
                    <option value={1}>Slow</option>
                    <option value={2}>Medium</option>
                    <option value={3}>Fast</option>
                  </select>
                  <div style={{ color: '#bbb', textAlign: 'right', fontSize: '0.9em' }}>Holding plate</div>
                  <select value={speedHoldingPlate} onChange={(e) => setSpeedHoldingPlate(+e.target.value)} style={{ ...selectStyle, width: '100%' }}>
                    <option value={1}>Slow</option>
                    <option value={2}>Medium</option>
                    <option value={3}>Fast</option>
                  </select>
                </div>
              </div>

              <div style={{ background: '#222', borderRadius: 6, padding: 8 }}>
                <div style={{ fontWeight: 'bold', marginBottom: 6, color: '#ddd' }}>Gripper</div>
                <div style={{ fontSize: '0.85em', color: '#888', marginBottom: 6 }}>
                  {(() => {
                    const { open, closed } = pf400Widths()
                    if (!selectedLabware) return 'Select labware'
                    return `Open: ${open ?? '—'} mm · Closed: ${closed ?? '—'} mm`
                  })()}
                </div>
                <div style={{ display: 'flex', gap: 8, justifyContent: 'space-between' }}>
                  <button
                    style={{ padding: '8px 10px', borderRadius: 6, background: '#1890ff', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold', flex: 1 }}
                    onClick={() => setGripperAbsolute(pf400Widths().open)}
                  >
                    Open
                  </button>
                  <button
                    style={{ padding: '8px 10px', borderRadius: 6, background: '#faad14', color: '#000', border: 'none', cursor: 'pointer', fontWeight: 'bold', flex: 1 }}
                    onClick={() => setGripperAbsolute(pf400Widths().closed)}
                  >
                    Close
                  </button>
                </div>
              </div>
            </div>

            <div style={{ display: 'flex', gap: 8, marginTop: 10 }}>
              <button
                style={{ padding: '10px 12px', borderRadius: 6, background: '#52c41a', color: '#fff', border: 'none', cursor: 'pointer', fontWeight: 'bold', flex: 1 }}
                onClick={runPickPlace}
              >
                Pick and Place
              </button>
            </div>
          </div>

          {/* Speed */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, justifyContent: 'center' }}>
            <span style={{ fontWeight: 'bold' }}>Speed:</span>
            <select
              value={speedProfile}
              onChange={e => setSpeedProfile(+e.target.value)}
              style={{ ...selectStyle, width: 120 }}
            >
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
              <span style={labelStyle}>Z (mm)</span>
              <div style={buttonRowStyle}>
                <button style={btn(colors.zUp)} onClick={() => sendJog('z', 1)}>▲</button>
              </div>
              <div style={midRowStyle} />
              <div style={buttonRowStyle}>
                <button style={btn(colors.zDown, '#fff')} onClick={() => sendJog('z', -1)}>▼</button>
              </div>
            </div>

            {/* Col 2: Out/In + L/R */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepOut} onChange={e => setStepOut(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {linearOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={labelStyle}>Out/In</span>
              <div style={buttonRowStyle}>
                <button style={btn(colors.out)} onClick={() => sendJog('out', 1)}>▲</button>
              </div>
              <div style={midRowStyle}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <button style={btn(colors.left, '#fff', 40)} onClick={() => sendJog('left', 1)}>◄</button>
                  <span style={{ fontSize: '0.6em', minWidth: 24, textAlign: 'center' }}>L/R</span>
                  <button style={btn(colors.right, '#000', 40)} onClick={() => sendJog('right', -1)}>►</button>
                </div>
              </div>
              <div style={buttonRowStyle}>
                <button style={btn(colors.inC, '#fff')} onClick={() => sendJog('in', -1)}>▼</button>
              </div>
            </div>

            {/* Col 3: CW/CCW */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepRot} onChange={e => setStepRot(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {angularOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={labelStyle}>Rot (°)</span>
              <div style={buttonRowStyle}>
                <button style={btn(colors.cw)} onClick={() => sendJog('rot', -1)}>↻</button>
              </div>
              <div style={midRowStyle} />
              <div style={buttonRowStyle}>
                <button style={btn(colors.ccw, '#fff')} onClick={() => sendJog('rot', 1)}>↺</button>
              </div>
            </div>

            {/* Col 4: Gripper */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
              <select value={stepGrip} onChange={e => setStepGrip(+e.target.value)} style={{...selectStyle, marginBottom: 5}}>
                {linearOpts.map(o => <option key={o.v} value={o.v}>{o.l}</option>)}
              </select>
              <span style={labelStyle}>Grip</span>
              <div style={buttonRowStyle}>
                <button style={btn(colors.gray, '#333')} onClick={() => sendJog('grip', -1)}>►◄</button>
              </div>
              <div style={midRowStyle}>
                <div style={{ display: 'flex', gap: 14, fontSize: '0.6em', color: '#bbb' }}>
                  <span>Close</span>
                  <span>Open</span>
                </div>
              </div>
              <div style={buttonRowStyle}>
                <button style={btn(colors.gray, '#333')} onClick={() => sendJog('grip', 1)}>◄►</button>
              </div>
            </div>
          </div>

          {/* Joint Jogs */}
          <div style={{ background: '#222', borderRadius: 8, padding: 10 }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
              <div style={{ fontWeight: 'bold' }}>Joint Jogs</div>
              <button style={btn('#111827', '#fff', 70)} onClick={moveToSafe}>Safe</button>
            </div>
            
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

        </div>
      </div>
    </div>
  )
}

export default PF400Diagnostics


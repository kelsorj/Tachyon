import { useEffect, useMemo, useState } from 'react'

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:8091"

function sortByName(a, b) {
  return String(a?.name || '').localeCompare(String(b?.name || ''))
}

function uniq(arr) {
  return Array.from(new Set(arr))
}

function TabButton({ active, onClick, children }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '10px 14px',
        borderRadius: 8,
        border: '1px solid #333',
        background: active ? '#1677ff' : '#1a1a2e',
        color: '#fff',
        fontWeight: 'bold',
        cursor: 'pointer'
      }}
    >
      {children}
    </button>
  )
}

function SmallButton({ disabled, onClick, children, variant = 'default' }) {
  const bg = variant === 'danger' ? '#ff4d4f' : variant === 'primary' ? '#52c41a' : '#444'
  return (
    <button
      disabled={disabled}
      onClick={onClick}
      style={{
        padding: '9px 12px',
        borderRadius: 8,
        border: 'none',
        background: bg,
        color: '#fff',
        fontWeight: 'bold',
        cursor: disabled ? 'not-allowed' : 'pointer',
        opacity: disabled ? 0.6 : 1,
        whiteSpace: 'nowrap'
      }}
    >
      {children}
    </button>
  )
}

function LabwareDashboard() {
  const [mode, setMode] = useState('entries') // entries | classes
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  const [labwareTypes, setLabwareTypes] = useState([])
  const [labwareClasses, setLabwareClasses] = useState([])
  const [loading, setLoading] = useState(true)

  const [selectedTypeId, setSelectedTypeId] = useState('')
  const [selectedClassId, setSelectedClassId] = useState('')
  const [entryTab, setEntryTab] = useState('plate') // plate | pipette | classes | image

  // New entry form (minimal)
  const [newEntryName, setNewEntryName] = useState('')
  const [newEntryWells, setNewEntryWells] = useState(96)
  const wellsOptions = useMemo(() => [6, 24, 48, 96, 384, 1536], [])

  // New class form (minimal)
  const [newClassName, setNewClassName] = useState('')

  const card = useMemo(() => ({
    background: '#1a1a2e',
    borderRadius: 12,
    border: '1px solid #333',
    padding: 16,
  }), [])

  const input = useMemo(() => ({
    padding: '8px 10px',
    borderRadius: 8,
    border: '1px solid #555',
    background: '#2a2a3e',
    color: '#fff',
    outline: 'none',
    width: '100%',
    boxSizing: 'border-box'
  }), [])

  const label = useMemo(() => ({ fontSize: '0.85em', color: '#bbb', marginBottom: 6, fontWeight: 'bold' }), [])

  const fetchAll = async () => {
    try {
      setLoading(true)
      const [typesRes, classesRes] = await Promise.all([
        fetch(`${API_URL}/labware/types`),
        fetch(`${API_URL}/labware/classes`),
      ])
      const typesJson = await typesRes.json().catch(() => ({}))
      const classesJson = await classesRes.json().catch(() => ({}))
      if (!typesRes.ok) throw new Error(typesJson.detail || 'Failed to fetch labware types')
      if (!classesRes.ok) throw new Error(classesJson.detail || 'Failed to fetch labware classes')
      const types = (typesJson.labware_types || []).slice().sort(sortByName)
      const classes = (classesJson.labware_classes || []).slice().sort(sortByName)
      setLabwareTypes(types)
      setLabwareClasses(classes)
      setError('')

      // choose defaults
      if (!selectedTypeId && types.length) setSelectedTypeId(types[0].labware_type_id)
      if (!selectedClassId && classes.length) setSelectedClassId(classes[0].labware_class_id)
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchAll()
  }, [])

  const selectedType = useMemo(
    () => labwareTypes.find(t => t.labware_type_id === selectedTypeId) || null,
    [labwareTypes, selectedTypeId]
  )

  const selectedClass = useMemo(
    () => labwareClasses.find(c => c.labware_class_id === selectedClassId) || null,
    [labwareClasses, selectedClassId]
  )

  const createEntry = async () => {
    setBusy(true)
    try {
      const payload = {
        kind: 'sbs_plate',
        name: newEntryName.trim(),
        wells: Number(newEntryWells),
        plate_dimensions_mm: { length_mm: 127.76, width_mm: 85.48, height_mm: 0 },
        well_dimensions_mm: {},
      }
      const res = await fetch(`${API_URL}/labware/types`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      })
      const data = await res.json()
      if (!res.ok) throw new Error(data.detail || 'Failed to create labware entry')
      setNewEntryName('')
      await fetchAll()
      const createdId = data?.labware_type?.labware_type_id
      if (createdId) setSelectedTypeId(createdId)
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  const deleteEntry = async () => {
    if (!selectedType) return
    if (!confirm(`Delete labware entry "${selectedType.name}"?`)) return
    setBusy(true)
    try {
      const res = await fetch(`${API_URL}/labware/types/${encodeURIComponent(selectedType.labware_type_id)}`, { method: 'DELETE' })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.detail || 'Failed to delete labware entry')
      setSelectedTypeId('')
      await fetchAll()
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  // -------- Plate Properties tab state (editable) --------
  const [ppDescription, setPpDescription] = useState('')
  const [ppManufacturerPart, setPpManufacturerPart] = useState('')
  const [ppWells, setPpWells] = useState(96)
  const [ppBaseClass, setPpBaseClass] = useState('microplate')

  const [ppRobotGripperOffset, setPpRobotGripperOffset] = useState('')
  const [ppEmptyCheckOffset, setPpEmptyCheckOffset] = useState('')
  const [ppThickness, setPpThickness] = useState('')
  const [ppStackingThickness, setPpStackingThickness] = useState('')
  const [ppShimThickness, setPpShimThickness] = useState('')

  const [ppCanBeSealed, setPpCanBeSealed] = useState(false)
  const [ppSealedThickness, setPpSealedThickness] = useState('')
  const [ppSealedStackingThickness, setPpSealedStackingThickness] = useState('')

  const [ppCanHaveLid, setPpCanHaveLid] = useState(false)
  const [ppLiddedThickness, setPpLiddedThickness] = useState('')
  const [ppLiddedStackingThickness, setPpLiddedStackingThickness] = useState('')
  const [ppLidRestingHeight, setPpLidRestingHeight] = useState('')
  const [ppLidDepartureHeight, setPpLidDepartureHeight] = useState('')

  const [ppLowerAtLabeler, setPpLowerAtLabeler] = useState(false)
  const [ppCanMount, setPpCanMount] = useState(false)
  const [ppCanBeMounted, setPpCanBeMounted] = useState(false)
  const [ppHandlingSpeed, setPpHandlingSpeed] = useState('fast') // slow|medium|fast

  const [ppFilterTipPinToolLength, setPpFilterTipPinToolLength] = useState('')
  const [ppFilterChannelRestingDepth, setPpFilterChannelRestingDepth] = useState('')
  const [ppRequiresInsert, setPpRequiresInsert] = useState('None')

  useEffect(() => {
    // Load editor state when the selected entry changes
    if (!selectedType) return
    setEntryTab('plate')
    setPpDescription(selectedType.description || '')
    setPpManufacturerPart(selectedType.catalog_number || '')
    setPpWells(Number(selectedType.wells || 96))
    setPpBaseClass(selectedType.base_class || 'microplate')

    const pp = selectedType.plate_properties || {}
    const f = (v) => (v === null || v === undefined) ? '' : String(v)
    setPpRobotGripperOffset(f(pp.robot_gripper_offset_mm))
    setPpEmptyCheckOffset(f(pp.empty_check_offset_mm))
    setPpThickness(f(pp.thickness_mm ?? selectedType?.plate_dimensions_mm?.height_mm))
    setPpStackingThickness(f(pp.stacking_thickness_mm))
    setPpShimThickness(f(pp.shim_thickness_mm))

    setPpCanBeSealed(Boolean(pp.can_be_sealed))
    setPpSealedThickness(f(pp.sealed_thickness_mm))
    setPpSealedStackingThickness(f(pp.sealed_stacking_thickness_mm))

    setPpCanHaveLid(Boolean(pp.can_have_lid))
    setPpLiddedThickness(f(pp.lidded_thickness_mm))
    setPpLiddedStackingThickness(f(pp.lidded_stacking_thickness_mm))
    setPpLidRestingHeight(f(pp.lid_resting_height_mm))
    setPpLidDepartureHeight(f(pp.lid_departure_height_mm))

    setPpLowerAtLabeler(Boolean(pp.lower_plate_at_labeler))
    setPpCanMount(Boolean(pp.can_mount))
    setPpCanBeMounted(Boolean(pp.can_be_mounted))
    setPpHandlingSpeed(pp.max_robot_handling_speed || 'fast')

    setPpFilterTipPinToolLength(f(pp.filter_tip_pin_tool_length_mm))
    setPpFilterChannelRestingDepth(f(pp.filter_channel_resting_depth_mm))
    setPpRequiresInsert(pp.requires_insert || 'None')
  }, [selectedTypeId])

  const savePlateProperties = async () => {
    if (!selectedType) return
    setBusy(true)
    try {
      const n = (s) => {
        const t = String(s || '').trim()
        if (t === '') return null
        const v = Number(t)
        return Number.isFinite(v) ? v : null
      }

      const payload = {
        description: ppDescription,
        catalog_number: ppManufacturerPart,
        base_class: ppBaseClass,
        wells: Number(ppWells),
        plate_properties: {
          robot_gripper_offset_mm: n(ppRobotGripperOffset),
          empty_check_offset_mm: n(ppEmptyCheckOffset),
          thickness_mm: n(ppThickness),
          stacking_thickness_mm: n(ppStackingThickness),
          shim_thickness_mm: n(ppShimThickness),
          can_be_sealed: Boolean(ppCanBeSealed),
          sealed_thickness_mm: n(ppSealedThickness),
          sealed_stacking_thickness_mm: n(ppSealedStackingThickness),
          can_have_lid: Boolean(ppCanHaveLid),
          lidded_thickness_mm: n(ppLiddedThickness),
          lidded_stacking_thickness_mm: n(ppLiddedStackingThickness),
          lid_resting_height_mm: n(ppLidRestingHeight),
          lid_departure_height_mm: n(ppLidDepartureHeight),
          lower_plate_at_labeler: Boolean(ppLowerAtLabeler),
          can_mount: Boolean(ppCanMount),
          can_be_mounted: Boolean(ppCanBeMounted),
          max_robot_handling_speed: ppHandlingSpeed,
          filter_tip_pin_tool_length_mm: n(ppFilterTipPinToolLength),
          filter_channel_resting_depth_mm: n(ppFilterChannelRestingDepth),
          requires_insert: String(ppRequiresInsert || '').trim() || null,
        }
      }

      const res = await fetch(`${API_URL}/labware/types/${encodeURIComponent(selectedType.labware_type_id)}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.detail || 'Failed to save plate properties')
      await fetchAll()
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  const createClass = async () => {
    setBusy(true)
    try {
      const res = await fetch(`${API_URL}/labware/classes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: newClassName.trim(), description: '' })
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.detail || 'Failed to create labware class')
      setNewClassName('')
      await fetchAll()
      const createdId = data?.labware_class?.labware_class_id
      if (createdId) setSelectedClassId(createdId)
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  const renameClass = async () => {
    if (!selectedClass) return
    const nextName = prompt('Rename labware class:', selectedClass.name)
    if (!nextName || !nextName.trim()) return
    setBusy(true)
    try {
      const res = await fetch(`${API_URL}/labware/classes/${encodeURIComponent(selectedClass.labware_class_id)}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: nextName.trim() })
      })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.detail || 'Failed to rename labware class')
      await fetchAll()
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  const deleteClass = async () => {
    if (!selectedClass) return
    if (!confirm(`Delete labware class "${selectedClass.name}"? This will remove it from all labware entries.`)) return
    setBusy(true)
    try {
      const res = await fetch(`${API_URL}/labware/classes/${encodeURIComponent(selectedClass.labware_class_id)}`, { method: 'DELETE' })
      const data = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(data.detail || 'Failed to delete labware class')
      setSelectedClassId('')
      await fetchAll()
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  // Membership editing on class screen (matches screenshot)
  const classMembers = useMemo(() => {
    if (!selectedClass) return []
    const cid = selectedClass.labware_class_id
    return labwareTypes.filter(t => (t.labware_class_ids || []).includes(cid)).sort(sortByName)
  }, [labwareTypes, selectedClass])

  const classNonMembers = useMemo(() => {
    if (!selectedClass) return labwareTypes.slice().sort(sortByName)
    const cid = selectedClass.labware_class_id
    return labwareTypes.filter(t => !(t.labware_class_ids || []).includes(cid)).sort(sortByName)
  }, [labwareTypes, selectedClass])

  const [pickedNonMembers, setPickedNonMembers] = useState([])
  const [pickedMembers, setPickedMembers] = useState([])

  useEffect(() => {
    // reset list selections when switching class
    setPickedNonMembers([])
    setPickedMembers([])
  }, [selectedClassId])

  const patchTypeClasses = async (typeId, newClassIds) => {
    const res = await fetch(`${API_URL}/labware/types/${encodeURIComponent(typeId)}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ labware_class_ids: newClassIds })
    })
    const data = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(data.detail || 'Failed to update membership')
    return data.labware_type
  }

  const addSelectedToClass = async () => {
    if (!selectedClass) return
    const cid = selectedClass.labware_class_id
    setBusy(true)
    try {
      for (const tid of pickedNonMembers) {
        const t = labwareTypes.find(x => x.labware_type_id === tid)
        const cur = t?.labware_class_ids || []
        const next = uniq([...(cur || []), cid])
        await patchTypeClasses(tid, next)
      }
      await fetchAll()
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  const removeSelectedFromClass = async () => {
    if (!selectedClass) return
    const cid = selectedClass.labware_class_id
    setBusy(true)
    try {
      for (const tid of pickedMembers) {
        const t = labwareTypes.find(x => x.labware_type_id === tid)
        const cur = t?.labware_class_ids || []
        const next = (cur || []).filter(x => x !== cid)
        await patchTypeClasses(tid, next)
      }
      await fetchAll()
    } catch (e) {
      setError(e.message || String(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={{ padding: 20, maxWidth: 1500, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 14 }}>
        <div>
          <div style={{ fontSize: '2em', fontWeight: 'bold', color: '#fff' }}>Labware</div>
          <div style={{ color: '#888', marginTop: 6 }}>
            Replicating the legacy Labware GUI with modern controls: entries, classes, and membership editing.
          </div>
        </div>
        <div style={{ display: 'flex', gap: 10, alignItems: 'center' }}>
          <TabButton active={mode === 'entries'} onClick={() => setMode('entries')}>Labware Entries</TabButton>
          <TabButton active={mode === 'classes'} onClick={() => setMode('classes')}>Labware Classes</TabButton>
          <SmallButton disabled={busy} onClick={fetchAll}>Refresh</SmallButton>
        </div>
      </div>

      {error && (
        <div style={{ ...card, borderColor: '#ff4d4f', marginBottom: 12 }}>
          <div style={{ color: '#ff7875', fontWeight: 'bold' }}>Error</div>
          <div style={{ color: '#ccc', marginTop: 6 }}>{error}</div>
        </div>
      )}

      {loading ? (
        <div style={{ color: '#888' }}>Loading…</div>
      ) : mode === 'entries' ? (
        <div style={{ display: 'grid', gridTemplateColumns: '340px 1fr', gap: 14, alignItems: 'start' }}>
          {/* Left: entries list + create */}
          <div style={card}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
              <div style={{ color: '#fff', fontWeight: 'bold' }}>Labware Entries</div>
              <div style={{ color: '#888', fontSize: '0.9em' }}>{labwareTypes.length} total</div>
            </div>

            <div style={{ display: 'grid', gap: 10 }}>
              <div style={{ display: 'grid', gap: 6 }}>
                <div style={label}>Create new (SBS plate)</div>
                <input value={newEntryName} onChange={(e) => setNewEntryName(e.target.value)} style={input} placeholder="Name" disabled={busy} />
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 8 }}>
                  <select value={newEntryWells} onChange={(e) => setNewEntryWells(Number(e.target.value))} style={input} disabled={busy}>
                    {wellsOptions.map(w => <option key={w} value={w}>{w} wells</option>)}
                  </select>
                  <SmallButton variant="primary" disabled={busy || !newEntryName.trim()} onClick={createEntry}>New</SmallButton>
                </div>
              </div>

              <div style={{ height: 520, overflow: 'auto', border: '1px solid #333', borderRadius: 10, padding: 8, background: '#111' }}>
                {labwareTypes.map(t => (
                  <div
                    key={t.labware_type_id}
                    onClick={() => setSelectedTypeId(t.labware_type_id)}
                    style={{
                      padding: '8px 10px',
                      borderRadius: 8,
                      cursor: 'pointer',
                      background: t.labware_type_id === selectedTypeId ? '#1677ff33' : 'transparent',
                      border: t.labware_type_id === selectedTypeId ? '1px solid #1677ff' : '1px solid transparent',
                      color: '#fff'
                    }}
                  >
                    <div style={{ fontWeight: 'bold' }}>{t.name}</div>
                    <div style={{ color: '#888', fontSize: '0.85em', marginTop: 2 }}>
                      {t.wells ? `${t.wells} wells` : t.kind}
                      {t.vendor ? ` · ${t.vendor}` : ''}
                      {t.catalog_number ? ` · ${t.catalog_number}` : ''}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Right: entry details (first pass) */}
          <div style={card}>
            {!selectedType ? (
              <div style={{ color: '#888' }}>Select a labware entry to view/edit.</div>
            ) : (
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12 }}>
                  <div>
                    <div style={{ color: '#fff', fontWeight: 'bold', fontSize: '1.2em' }}>{selectedType.name}</div>
                    <div style={{ color: '#888', marginTop: 4 }}>
                      id: <span style={{ fontFamily: 'monospace' }}>{selectedType.labware_type_id}</span>
                    </div>
                  </div>
                  <SmallButton variant="danger" disabled={busy} onClick={deleteEntry}>Delete</SmallButton>
                </div>

                {/* Entry tabs (starting with Plate Properties to match plate-props.PNG) */}
                <div style={{ display: 'flex', gap: 10, marginTop: 14, flexWrap: 'wrap' }}>
                  <TabButton active={entryTab === 'plate'} onClick={() => setEntryTab('plate')}>Plate Properties</TabButton>
                  <TabButton active={entryTab === 'pipette'} onClick={() => setEntryTab('pipette')}>Pipette/Well Definition</TabButton>
                  <TabButton active={entryTab === 'classes'} onClick={() => setEntryTab('classes')}>Labware Classes</TabButton>
                  <TabButton active={entryTab === 'image'} onClick={() => setEntryTab('image')}>Image</TabButton>
                </div>

                {entryTab === 'plate' ? (
                  <div style={{ marginTop: 14 }}>
                    {/* General */}
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: 14 }}>
                      <div style={{ border: '1px solid #333', borderRadius: 12, padding: 14, background: '#111' }}>
                        <div style={{ color: '#fff', fontWeight: 'bold', marginBottom: 10 }}>Labware-Entry General Properties</div>

                        <div style={{ display: 'grid', gap: 10 }}>
                          <div>
                            <div style={label}>Description</div>
                            <textarea
                              value={ppDescription}
                              onChange={(e) => setPpDescription(e.target.value)}
                              style={{ ...input, height: 90, resize: 'vertical' }}
                              disabled={busy}
                            />
                          </div>

                          <div style={{ display: 'grid', gridTemplateColumns: '1fr 180px', gap: 10 }}>
                            <div>
                              <div style={label}>Manufacturer part number</div>
                              <input value={ppManufacturerPart} onChange={(e) => setPpManufacturerPart(e.target.value)} style={input} disabled={busy} />
                            </div>
                            <div>
                              <div style={label}>Number of wells</div>
                              <select value={ppWells} onChange={(e) => setPpWells(Number(e.target.value))} style={input} disabled={busy}>
                                {wellsOptions.map(w => <option key={w} value={w}>{w}</option>)}
                              </select>
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Base class */}
                      <div style={{ border: '1px solid #333', borderRadius: 12, padding: 14, background: '#111' }}>
                        <div style={{ color: '#fff', fontWeight: 'bold', marginBottom: 10 }}>Base Class</div>
                        {[
                          ['microplate', 'Microplate'],
                          ['filter_plate', 'Filter plate'],
                          ['reservoir', 'Reservoir'],
                          ['tip_wash_station', 'Tip Wash Station'],
                          ['pin_tool', 'Pin tool'],
                          ['tip_box', 'Tip box'],
                          ['lid', 'Lid'],
                          ['tip_trash_bin', 'Tip trash bin'],
                          ['assaymap_cartridge_rack', 'AssayMAP cartridge rack'],
                        ].map(([val, text]) => (
                          <label key={val} style={{ display: 'flex', gap: 10, alignItems: 'center', color: '#ddd', marginBottom: 8 }}>
                            <input
                              type="radio"
                              name="base_class"
                              value={val}
                              checked={ppBaseClass === val}
                              onChange={() => setPpBaseClass(val)}
                              disabled={busy}
                            />
                            {text}
                          </label>
                        ))}
                      </div>
                    </div>

                    {/* Plate properties grid like screenshot */}
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14, marginTop: 14 }}>
                      {/* Left group: Plate thickness/stacking/lid/seal */}
                      <div style={{ border: '1px solid #333', borderRadius: 12, padding: 14, background: '#111' }}>
                        <div style={{ color: '#fff', fontWeight: 'bold', marginBottom: 10 }}>Plate Properties</div>
                        <div style={{ display: 'grid', gridTemplateColumns: '1fr 140px', gap: 10, alignItems: 'center' }}>
                          <div style={{ color: '#bbb' }}>Robot gripper offset (mm)</div>
                          <input value={ppRobotGripperOffset} onChange={(e) => setPpRobotGripperOffset(e.target.value)} style={input} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Empty check offset (mm)</div>
                          <input value={ppEmptyCheckOffset} onChange={(e) => setPpEmptyCheckOffset(e.target.value)} style={input} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Thickness (mm)</div>
                          <input value={ppThickness} onChange={(e) => setPpThickness(e.target.value)} style={input} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Stacking thickness (mm)</div>
                          <input value={ppStackingThickness} onChange={(e) => setPpStackingThickness(e.target.value)} style={input} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Shim/nesting thickness (mm)</div>
                          <input value={ppShimThickness} onChange={(e) => setPpShimThickness(e.target.value)} style={input} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Can be sealed?</div>
                          <input type="checkbox" checked={ppCanBeSealed} onChange={(e) => setPpCanBeSealed(e.target.checked)} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Sealed thickness (mm)</div>
                          <input value={ppSealedThickness} onChange={(e) => setPpSealedThickness(e.target.value)} style={input} disabled={busy || !ppCanBeSealed} />

                          <div style={{ color: '#bbb' }}>Sealed stacking thickness (mm)</div>
                          <input value={ppSealedStackingThickness} onChange={(e) => setPpSealedStackingThickness(e.target.value)} style={input} disabled={busy || !ppCanBeSealed} />

                          <div style={{ color: '#bbb' }}>Can have lid?</div>
                          <input type="checkbox" checked={ppCanHaveLid} onChange={(e) => setPpCanHaveLid(e.target.checked)} disabled={busy} />

                          <div style={{ color: '#bbb' }}>Lidded thickness (mm)</div>
                          <input value={ppLiddedThickness} onChange={(e) => setPpLiddedThickness(e.target.value)} style={input} disabled={busy || !ppCanHaveLid} />

                          <div style={{ color: '#bbb' }}>Lidded stacking thickness (mm)</div>
                          <input value={ppLiddedStackingThickness} onChange={(e) => setPpLiddedStackingThickness(e.target.value)} style={input} disabled={busy || !ppCanHaveLid} />

                          <div style={{ color: '#bbb' }}>Lid resting height (mm)</div>
                          <input value={ppLidRestingHeight} onChange={(e) => setPpLidRestingHeight(e.target.value)} style={input} disabled={busy || !ppCanHaveLid} />

                          <div style={{ color: '#bbb' }}>Lid departure height (mm)</div>
                          <input value={ppLidDepartureHeight} onChange={(e) => setPpLidDepartureHeight(e.target.value)} style={input} disabled={busy || !ppCanHaveLid} />
                        </div>
                      </div>

                      {/* Right group: handling / speed / misc */}
                      <div style={{ display: 'grid', gap: 14 }}>
                        <div style={{ border: '1px solid #333', borderRadius: 12, padding: 14, background: '#111' }}>
                          <div style={{ color: '#fff', fontWeight: 'bold', marginBottom: 10 }}>Plate Handling</div>
                          <label style={{ display: 'flex', gap: 10, alignItems: 'center', color: '#ddd', marginBottom: 8 }}>
                            <input type="checkbox" checked={ppLowerAtLabeler} onChange={(e) => setPpLowerAtLabeler(e.target.checked)} disabled={busy} />
                            Lower plate at Microplate Labeler
                          </label>
                          <label style={{ display: 'flex', gap: 10, alignItems: 'center', color: '#ddd', marginBottom: 8 }}>
                            <input type="checkbox" checked={ppCanMount} onChange={(e) => setPpCanMount(e.target.checked)} disabled={busy} />
                            Can mount
                          </label>
                          <label style={{ display: 'flex', gap: 10, alignItems: 'center', color: '#ddd' }}>
                            <input type="checkbox" checked={ppCanBeMounted} onChange={(e) => setPpCanBeMounted(e.target.checked)} disabled={busy} />
                            Can be mounted
                          </label>
                        </div>

                        <div style={{ border: '1px solid #333', borderRadius: 12, padding: 14, background: '#111' }}>
                          <div style={{ color: '#fff', fontWeight: 'bold', marginBottom: 10 }}>Maximum Robot Handling Speed</div>
                          {[
                            ['slow', 'Slow'],
                            ['medium', 'Medium'],
                            ['fast', 'Fast'],
                          ].map(([val, text]) => (
                            <label key={val} style={{ display: 'flex', gap: 10, alignItems: 'center', color: '#ddd', marginBottom: 8 }}>
                              <input
                                type="radio"
                                name="handling_speed"
                                value={val}
                                checked={ppHandlingSpeed === val}
                                onChange={() => setPpHandlingSpeed(val)}
                                disabled={busy}
                              />
                              {text}
                            </label>
                          ))}
                        </div>

                        <div style={{ border: '1px solid #333', borderRadius: 12, padding: 14, background: '#111' }}>
                          <div style={{ color: '#fff', fontWeight: 'bold', marginBottom: 10 }}>Miscellaneous</div>
                          <div style={{ display: 'grid', gridTemplateColumns: '1fr 140px', gap: 10, alignItems: 'center' }}>
                            <div style={{ color: '#bbb' }}>Length of filter tip/pin tool (mm)</div>
                            <input value={ppFilterTipPinToolLength} onChange={(e) => setPpFilterTipPinToolLength(e.target.value)} style={input} disabled={busy} />
                            <div style={{ color: '#bbb' }}>Filter channel resting depth (mm)</div>
                            <input value={ppFilterChannelRestingDepth} onChange={(e) => setPpFilterChannelRestingDepth(e.target.value)} style={input} disabled={busy} />
                          </div>

                          <div style={{ marginTop: 10 }}>
                            <div style={label}>Requires insert</div>
                            <select value={ppRequiresInsert} onChange={(e) => setPpRequiresInsert(e.target.value)} style={input} disabled={busy}>
                              <option value="None">None</option>
                              <option value="Required">Required</option>
                            </select>
                          </div>
                        </div>

                        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
                          <SmallButton variant="primary" disabled={busy} onClick={savePlateProperties}>Save changes</SmallButton>
                        </div>
                      </div>
                    </div>
                  </div>
                ) : entryTab === 'pipette' ? (
                  <div style={{ marginTop: 14, color: '#888' }}>Next: replicate “Pipette/Well Definition”.</div>
                ) : entryTab === 'classes' ? (
                  <div style={{ marginTop: 14, color: '#888' }}>Next: replicate per-entry “Labware Classes” membership editor.</div>
                ) : (
                  <div style={{ marginTop: 14, color: '#888' }}>Next: replicate “Image” tab.</div>
                )}
              </div>
            )}
          </div>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: '280px 1fr', gap: 14, alignItems: 'start' }}>
          {/* Left: classes list */}
          <div style={card}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
              <div style={{ color: '#fff', fontWeight: 'bold' }}>Labware Classes</div>
              <div style={{ color: '#888', fontSize: '0.9em' }}>{labwareClasses.length} total</div>
            </div>

            <div style={{ display: 'grid', gap: 10 }}>
              <div style={{ display: 'grid', gap: 6 }}>
                <div style={label}>New class</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: 8 }}>
                  <input value={newClassName} onChange={(e) => setNewClassName(e.target.value)} style={input} placeholder="e.g. ShortLid" disabled={busy} />
                  <SmallButton variant="primary" disabled={busy || !newClassName.trim()} onClick={createClass}>New</SmallButton>
                </div>
              </div>

              <div style={{ height: 520, overflow: 'auto', border: '1px solid #333', borderRadius: 10, padding: 8, background: '#111' }}>
                {labwareClasses.length === 0 ? (
                  <div style={{ color: '#888' }}>No classes yet.</div>
                ) : labwareClasses.map(c => (
                  <div
                    key={c.labware_class_id}
                    onClick={() => setSelectedClassId(c.labware_class_id)}
                    style={{
                      padding: '8px 10px',
                      borderRadius: 8,
                      cursor: 'pointer',
                      background: c.labware_class_id === selectedClassId ? '#1677ff33' : 'transparent',
                      border: c.labware_class_id === selectedClassId ? '1px solid #1677ff' : '1px solid transparent',
                      color: '#fff'
                    }}
                  >
                    <div style={{ fontWeight: 'bold' }}>{c.name}</div>
                    <div style={{ color: '#666', fontSize: '0.8em', fontFamily: 'monospace', marginTop: 2 }}>
                      {c.labware_class_id}
                    </div>
                  </div>
                ))}
              </div>

              <div style={{ display: 'flex', gap: 10 }}>
                <SmallButton disabled={busy || !selectedClass} onClick={renameClass}>Rename</SmallButton>
                <SmallButton variant="danger" disabled={busy || !selectedClass} onClick={deleteClass}>Delete</SmallButton>
              </div>
            </div>
          </div>

          {/* Right: membership editor (matches screenshot layout) */}
          <div style={card}>
            {!selectedClass ? (
              <div style={{ color: '#888' }}>Select a labware class to edit membership.</div>
            ) : (
              <>
                <div style={{ color: '#fff', fontWeight: 'bold', fontSize: '1.1em' }}>
                  Labware-Entry Membership — <span style={{ color: '#69c0ff' }}>{selectedClass.name}</span>
                </div>
                <div style={{ color: '#888', marginTop: 6 }}>
                  Move labware entries between the lists (this controls device-fit constraints later).
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 80px 1fr', gap: 12, marginTop: 14 }}>
                  <div>
                    <div style={label}>Not a member of this class</div>
                    <select
                      multiple
                      value={pickedNonMembers}
                      onChange={(e) => setPickedNonMembers(Array.from(e.target.selectedOptions).map(o => o.value))}
                      style={{ ...input, height: 430 }}
                      disabled={busy}
                    >
                      {classNonMembers.map(t => (
                        <option key={t.labware_type_id} value={t.labware_type_id}>{t.name}</option>
                      ))}
                    </select>
                  </div>

                  <div style={{ display: 'flex', flexDirection: 'column', gap: 10, justifyContent: 'center' }}>
                    <SmallButton disabled={busy || pickedNonMembers.length === 0} onClick={addSelectedToClass}>&gt;</SmallButton>
                    <SmallButton disabled={busy || pickedMembers.length === 0} onClick={removeSelectedFromClass}>&lt;</SmallButton>
                  </div>

                  <div>
                    <div style={label}>Member of this class</div>
                    <select
                      multiple
                      value={pickedMembers}
                      onChange={(e) => setPickedMembers(Array.from(e.target.selectedOptions).map(o => o.value))}
                      style={{ ...input, height: 430 }}
                      disabled={busy}
                    >
                      {classMembers.map(t => (
                        <option key={t.labware_type_id} value={t.labware_type_id}>{t.name}</option>
                      ))}
                    </select>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

export default LabwareDashboard





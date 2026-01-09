import { useState, useEffect, useCallback } from 'react'
import {
  ReactFlow,
  Controls,
  Background,
  applyNodeChanges,
  applyEdgeChanges,
  addEdge,
  MiniMap,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'

// API URL handling for remote access
const DEFAULT_API_URL = `${window.location.protocol}//${window.location.hostname}:8091`
const ENV_API_URL = import.meta.env.VITE_API_URL
const API_URL = (ENV_API_URL && !(ENV_API_URL.includes('localhost') && window.location.hostname !== 'localhost'))
  ? ENV_API_URL
  : DEFAULT_API_URL

// Custom node styles
const nodeStyles = {
  trigger: { background: '#10b981', border: '2px solid #059669', color: '#fff' },
  end: { background: '#ef4444', border: '2px solid #dc2626', color: '#fff' },
  device_action: { background: '#3b82f6', border: '2px solid #2563eb', color: '#fff' },
  code_module: { background: '#8b5cf6', border: '2px solid #7c3aed', color: '#fff' },
  conditional: { background: '#f59e0b', border: '2px solid #d97706', color: '#fff' },
  delay: { background: '#6b7280', border: '2px solid #4b5563', color: '#fff' },
}

const nodeTypeLabels = {
  trigger: '▶ Start',
  end: '■ End',
  device_action: '🤖 Device Action',
  code_module: '💻 Code Module',
  conditional: '🔀 Conditional',
  delay: '⏱️ Delay',
}

function WorkflowsDashboard() {
  // State
  const [workflows, setWorkflows] = useState([])
  const [collections, setCollections] = useState([])
  const [selectedWorkflow, setSelectedWorkflow] = useState(null)
  const [nodes, setNodes] = useState([])
  const [edges, setEdges] = useState([])
  const [logs, setLogs] = useState([])
  const [activeRuns, setActiveRuns] = useState([])
  const [devices, setDevices] = useState([])
  const [labwareTypes, setLabwareTypes] = useState([])
  
  // UI state
  const [workflowListCollapsed, setWorkflowListCollapsed] = useState(false)
  
  // Editor state
  const [workflowName, setWorkflowName] = useState('')
  const [workflowDescription, setWorkflowDescription] = useState('')
  const [selectedCollection, setSelectedCollection] = useState('')
  const [selectedNode, setSelectedNode] = useState(null)
  
  // Workflow labware inventory - plates that this workflow manipulates
  const [workflowLabware, setWorkflowLabware] = useState([])
  
  const log = (msg) => setLogs(prev => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev.slice(0, 49)])

  // Load workflows and collections on mount
  useEffect(() => {
    fetchWorkflows()
    fetchCollections()
    fetchDevices()
    fetchLabwareTypes()
    fetchActiveRuns()
    
    // Poll for active runs
    const interval = setInterval(fetchActiveRuns, 2000)
    return () => clearInterval(interval)
  }, [])

  const fetchWorkflows = async () => {
    try {
      const res = await fetch(`${API_URL}/workflows`)
      if (res.ok) {
        const data = await res.json()
        setWorkflows(data.workflows || [])
      }
    } catch (e) {
      log(`✗ Failed to fetch workflows: ${e.message}`)
    }
  }

  const fetchCollections = async () => {
    try {
      const res = await fetch(`${API_URL}/workflows/collections`)
      if (res.ok) {
        const data = await res.json()
        setCollections(data.collections || [])
      }
    } catch (e) {
      log(`✗ Failed to fetch collections: ${e.message}`)
    }
  }

  const fetchDevices = async () => {
    try {
      const res = await fetch(`${API_URL}/devices`)
      if (res.ok) {
        const data = await res.json()
        setDevices(data.devices || [])
      }
    } catch (e) {
      log(`✗ Failed to fetch devices: ${e.message}`)
    }
  }

  const fetchLabwareTypes = async () => {
    try {
      const res = await fetch(`${API_URL}/labware/types`)
      if (res.ok) {
        const data = await res.json()
        setLabwareTypes(data.labware_types || [])
      }
    } catch (e) {
      log(`✗ Failed to fetch labware types: ${e.message}`)
    }
  }

  const fetchActiveRuns = async () => {
    try {
      const res = await fetch(`${API_URL}/workflows/runs/active`)
      if (res.ok) {
        const data = await res.json()
        setActiveRuns(data.active_runs || [])
      }
    } catch (e) {
      // Silently fail for polling
    }
  }

  // Load workflow into editor
  const loadWorkflow = async (workflowId) => {
    try {
      const res = await fetch(`${API_URL}/workflows/${workflowId}`)
      if (res.ok) {
        const wf = await res.json()
        setSelectedWorkflow(wf)
        setWorkflowName(wf.name || '')
        setWorkflowDescription(wf.description || '')
        setSelectedCollection(wf.device_collection_id || '')
        
        // Convert nodes for React Flow
        const flowNodes = (wf.nodes || []).map(n => ({
          id: n.id,
          type: 'default',
          position: n.position || { x: 100, y: 100 },
          data: { 
            label: n.data?.label || nodeTypeLabels[n.type] || n.type,
            ...n.data,
            nodeType: n.type,
          },
          style: {
            ...nodeStyles[n.type],
            padding: 10,
            borderRadius: 8,
            minWidth: 150,
            textAlign: 'center',
            fontWeight: 'bold',
          },
        }))
        
        // Convert edges for React Flow
        const flowEdges = (wf.edges || []).map(e => ({
          id: e.id || `${e.source}-${e.target}`,
          source: e.source,
          target: e.target,
          sourceHandle: e.sourceHandle,
          targetHandle: e.targetHandle,
          label: e.label,
          animated: true,
          style: { stroke: '#888' },
        }))
        
        setNodes(flowNodes)
        setEdges(flowEdges)
        setWorkflowLabware(wf.labware || [])
        setSelectedNode(null)
        log(`✓ Loaded workflow: ${wf.name}`)
      }
    } catch (e) {
      log(`✗ Failed to load workflow: ${e.message}`)
    }
  }

  // Create new workflow
  const createWorkflow = async () => {
    const name = prompt('Workflow name:')
    if (!name) return
    
    try {
      const res = await fetch(`${API_URL}/workflows`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name,
          description: '',
          nodes: [
            { id: 'start', type: 'trigger', position: { x: 250, y: 50 }, data: { label: '▶ Start' } },
            { id: 'end', type: 'end', position: { x: 250, y: 300 }, data: { label: '■ End' } },
          ],
          edges: [],
        }),
      })
      if (res.ok) {
        const wf = await res.json()
        log(`✓ Created workflow: ${name}`)
        fetchWorkflows()
        loadWorkflow(wf.workflow_id)
      }
    } catch (e) {
      log(`✗ Failed to create workflow: ${e.message}`)
    }
  }

  // Save workflow
  const saveWorkflow = async () => {
    if (!selectedWorkflow) return
    
    // Convert React Flow nodes back to workflow format
    const wfNodes = nodes.map(n => ({
      id: n.id,
      type: n.data?.nodeType || 'device_action',
      position: n.position,
      data: { ...n.data },
    }))
    
    const wfEdges = edges.map(e => ({
      id: e.id,
      source: e.source,
      target: e.target,
      sourceHandle: e.sourceHandle,
      targetHandle: e.targetHandle,
      label: e.label,
    }))
    
    try {
      const res = await fetch(`${API_URL}/workflows/${selectedWorkflow.workflow_id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: workflowName,
          description: workflowDescription,
          device_collection_id: selectedCollection || null,
          nodes: wfNodes,
          edges: wfEdges,
          labware: workflowLabware,
        }),
      })
      if (res.ok) {
        log(`✓ Saved workflow: ${workflowName}`)
        fetchWorkflows()
      }
    } catch (e) {
      log(`✗ Failed to save workflow: ${e.message}`)
    }
  }

  // Delete workflow
  const deleteWorkflow = async () => {
    if (!selectedWorkflow) return
    if (!confirm(`Delete workflow "${selectedWorkflow.name}"?`)) return
    
    try {
      const res = await fetch(`${API_URL}/workflows/${selectedWorkflow.workflow_id}`, {
        method: 'DELETE',
      })
      if (res.ok) {
        log(`✓ Deleted workflow: ${selectedWorkflow.name}`)
        setSelectedWorkflow(null)
        setNodes([])
        setEdges([])
        fetchWorkflows()
      }
    } catch (e) {
      log(`✗ Failed to delete workflow: ${e.message}`)
    }
  }

  // Add node
  const addNode = (type) => {
    const id = `node-${Date.now()}`
    const newNode = {
      id,
      type: 'default',
      position: { x: 250, y: 150 + nodes.length * 80 },
      data: { 
        label: nodeTypeLabels[type] || type,
        nodeType: type,
      },
      style: {
        ...nodeStyles[type],
        padding: 10,
        borderRadius: 8,
        minWidth: 150,
        textAlign: 'center',
        fontWeight: 'bold',
      },
    }
    setNodes(prev => [...prev, newNode])
    log(`+ Added ${type} node`)
  }

  // Run workflow
  const runWorkflow = async (simulate = false) => {
    if (!selectedWorkflow) return
    
    log(`→ Starting ${simulate ? 'simulation' : 'run'}...`)
    
    try {
      const res = await fetch(`${API_URL}/workflows/runs/start`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          workflow_id: selectedWorkflow.workflow_id,
          simulate,
        }),
      })
      if (res.ok) {
        const data = await res.json()
        log(`✓ Started ${simulate ? 'simulation' : 'run'}: ${data.run_id.slice(0, 12)}...`)
        fetchActiveRuns()
        
        // Poll for run completion and show results
        pollRunStatus(data.run_id, simulate)
      } else {
        const errData = await res.json().catch(() => ({}))
        log(`✗ Failed: ${errData.detail || res.status}`)
      }
    } catch (e) {
      log(`✗ Failed to start workflow: ${e.message}`)
    }
  }

  // Poll for run status and log updates
  const pollRunStatus = async (runId, simulate) => {
    let completed = false
    const pollInterval = setInterval(async () => {
      if (completed) return
      try {
        const res = await fetch(`${API_URL}/workflows/runs/${runId}`)
        if (res.ok) {
          const status = await res.json()
          
          if (status.state === 'completed') {
            if (completed) return
            completed = true
            clearInterval(pollInterval)
            log(`✓ ${simulate ? 'Simulation' : 'Run'} completed successfully!`)
            
            // Log step results
            if (status.step_results) {
              const steps = Object.entries(status.step_results)
              steps.forEach(([stepId, result]) => {
                if (result.source && result.target) {
                  log(`  📦 ${result.source} → ${result.target}`)
                } else if (result.action) {
                  log(`  🤖 ${result.action}`)
                }
              })
            }
            fetchActiveRuns()
          } else if (status.state === 'error') {
            if (completed) return
            completed = true
            clearInterval(pollInterval)
            log(`✗ ${simulate ? 'Simulation' : 'Run'} failed: ${status.error || 'Unknown error'}`)
            fetchActiveRuns()
          } else if (status.state === 'cancelled') {
            if (completed) return
            completed = true
            clearInterval(pollInterval)
            log(`⚠ ${simulate ? 'Simulation' : 'Run'} cancelled`)
            fetchActiveRuns()
          }
        }
      } catch (e) {
        // Ignore polling errors
      }
    }, 500)
    
    // Stop polling after 60 seconds max
    setTimeout(() => {
      completed = true
      clearInterval(pollInterval)
    }, 60000)
  }

  // React Flow callbacks
  const onNodesChange = useCallback(
    (changes) => setNodes((nds) => applyNodeChanges(changes, nds)),
    []
  )
  
  const onEdgesChange = useCallback(
    (changes) => setEdges((eds) => applyEdgeChanges(changes, eds)),
    []
  )
  
  const onConnect = useCallback(
    (params) => setEdges((eds) => addEdge({ ...params, animated: true, style: { stroke: '#888' } }, eds)),
    []
  )
  
  const onNodeClick = useCallback((event, node) => {
    setSelectedNode(node)
  }, [])

  return (
    <div style={{ display: 'flex', height: '100%', background: '#0a0a0a' }}>
      {/* Left Panel - Workflow List (Collapsible) */}
      <div style={{ 
        width: workflowListCollapsed ? 40 : 280, 
        borderRight: '2px solid #333', 
        display: 'flex', 
        flexDirection: 'column',
        transition: 'width 0.2s ease',
        overflow: 'hidden',
      }}>
        {/* Header with collapse toggle */}
        <div style={{ 
          padding: workflowListCollapsed ? '15px 8px' : 15, 
          borderBottom: '1px solid #333',
          display: 'flex',
          alignItems: workflowListCollapsed ? 'center' : 'flex-start',
          flexDirection: workflowListCollapsed ? 'column' : 'column',
          gap: 10,
        }}>
          {workflowListCollapsed ? (
            <button
              onClick={() => setWorkflowListCollapsed(false)}
              style={{
                background: 'none',
                border: 'none',
                color: '#888',
                cursor: 'pointer',
                fontSize: '1.2em',
                padding: 5,
              }}
              title="Expand workflow list"
            >
              ▶
            </button>
          ) : (
            <>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', width: '100%' }}>
                <h2 style={{ color: '#fff', margin: 0, fontSize: '1.2em' }}>Workflows</h2>
                <button
                  onClick={() => setWorkflowListCollapsed(true)}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: '#888',
                    cursor: 'pointer',
                    fontSize: '1em',
                    padding: 5,
                  }}
                  title="Collapse workflow list"
                >
                  ◀
                </button>
              </div>
              <button
                onClick={createWorkflow}
                style={{
                  width: '100%',
                  padding: '10px',
                  background: '#1890ff',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 6,
                  cursor: 'pointer',
                  fontWeight: 'bold',
                }}
              >
                + New Workflow
              </button>
            </>
          )}
        </div>
        
        {!workflowListCollapsed && (
          <>
            <div style={{ flex: 1, overflow: 'auto', padding: 10 }}>
              {workflows.map(wf => (
                <div
                  key={wf.workflow_id}
                  onClick={() => loadWorkflow(wf.workflow_id)}
                  style={{
                    padding: '12px',
                    marginBottom: 8,
                    background: selectedWorkflow?.workflow_id === wf.workflow_id ? '#1890ff' : '#1a1a2e',
                    borderRadius: 6,
                    cursor: 'pointer',
                    border: '1px solid #333',
                  }}
                >
                  <div style={{ color: '#fff', fontWeight: 'bold' }}>{wf.name}</div>
                  <div style={{ color: '#888', fontSize: '0.85em' }}>
                    {(wf.nodes || []).length} nodes
                  </div>
                </div>
              ))}
              {workflows.length === 0 && (
                <div style={{ color: '#666', textAlign: 'center', padding: 20 }}>
                  No workflows yet
                </div>
              )}
            </div>

            {/* Active Runs */}
            {activeRuns.length > 0 && (
              <div style={{ borderTop: '1px solid #333', padding: 10 }}>
                <div style={{ color: '#10b981', fontWeight: 'bold', marginBottom: 8 }}>
                  ⚡ Active Runs ({activeRuns.length})
                </div>
                {activeRuns.map(run => (
                  <div key={run.run_id} style={{ 
                    padding: 8, 
                    background: '#1a1a2e', 
                    borderRadius: 4, 
                    marginBottom: 4,
                    fontSize: '0.85em',
                  }}>
                    <div style={{ color: '#fff' }}>{run.run_id.slice(0, 8)}...</div>
                    <div style={{ color: run.simulate ? '#f59e0b' : '#10b981' }}>
                      {run.state} {run.simulate && '(sim)'}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </>
        )}
      </div>

      {/* Center - Flow Editor */}
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Toolbar */}
        {selectedWorkflow && (
          <div style={{ 
            padding: '10px 15px', 
            borderBottom: '1px solid #333', 
            display: 'flex', 
            gap: 10, 
            alignItems: 'center',
            flexWrap: 'wrap',
          }}>
            <input
              type="text"
              value={workflowName}
              onChange={(e) => setWorkflowName(e.target.value)}
              placeholder="Workflow name"
              style={{
                padding: '8px 12px',
                background: '#1a1a2e',
                border: '1px solid #333',
                borderRadius: 4,
                color: '#fff',
                width: 200,
              }}
            />
            
            <select
              value={selectedCollection}
              onChange={(e) => setSelectedCollection(e.target.value)}
              style={{
                padding: '8px 12px',
                background: '#1a1a2e',
                border: '1px solid #333',
                borderRadius: 4,
                color: '#fff',
              }}
            >
              <option value="">No device collection</option>
              {collections.map(c => (
                <option key={c.collection_id} value={c.collection_id}>
                  {c.name}
                </option>
              ))}
            </select>
            
            <div style={{ flex: 1 }} />
            
            <button onClick={saveWorkflow} style={{ padding: '8px 16px', background: '#10b981', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
              💾 Save
            </button>
            <button onClick={() => runWorkflow(true)} style={{ padding: '8px 16px', background: '#f59e0b', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
              🔬 Simulate
            </button>
            <button onClick={() => runWorkflow(false)} style={{ padding: '8px 16px', background: '#1890ff', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
              ▶ Run
            </button>
            <button onClick={deleteWorkflow} style={{ padding: '8px 16px', background: '#ef4444', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
              🗑️
            </button>
          </div>
        )}

        {/* Node Palette */}
        {selectedWorkflow && (
          <div style={{ 
            padding: '8px 15px', 
            borderBottom: '1px solid #333', 
            display: 'flex', 
            gap: 8,
            flexWrap: 'wrap',
          }}>
            <span style={{ color: '#888', alignSelf: 'center', marginRight: 8 }}>Add:</span>
            {Object.entries(nodeTypeLabels).filter(([k]) => k !== 'trigger' && k !== 'end').map(([type, label]) => (
              <button
                key={type}
                onClick={() => addNode(type)}
                style={{
                  padding: '6px 12px',
                  background: nodeStyles[type]?.background || '#333',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  cursor: 'pointer',
                  fontSize: '0.85em',
                }}
              >
                {label}
              </button>
            ))}
          </div>
        )}

        {/* React Flow Canvas */}
        <div style={{ flex: 1 }}>
          {selectedWorkflow ? (
            <ReactFlow
              nodes={nodes}
              edges={edges}
              onNodesChange={onNodesChange}
              onEdgesChange={onEdgesChange}
              onConnect={onConnect}
              onNodeClick={onNodeClick}
              fitView
              style={{ background: '#111' }}
            >
              <Background color="#333" gap={20} />
              <Controls style={{ background: '#1a1a2e', borderRadius: 6 }} />
              <MiniMap 
                style={{ background: '#1a1a2e' }} 
                nodeColor={(n) => nodeStyles[n.data?.nodeType]?.background || '#666'}
              />
            </ReactFlow>
          ) : (
            <div style={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center', 
              height: '100%',
              color: '#666',
              fontSize: '1.2em',
            }}>
              Select a workflow or create a new one
            </div>
          )}
        </div>
      </div>

      {/* Right Panel - Node Properties & Logs */}
      <div style={{ width: 300, borderLeft: '2px solid #333', display: 'flex', flexDirection: 'column' }}>
        {/* Node Properties */}
        <div style={{ flex: 1, overflow: 'auto', padding: 15, borderBottom: '1px solid #333' }}>
          <h3 style={{ color: '#fff', margin: '0 0 15px 0' }}>
            {selectedNode ? 'Node Properties' : 'Properties'}
          </h3>
          
          {selectedNode ? (
            <div>
              <div style={{ marginBottom: 15 }}>
                <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Type</label>
                <div style={{ 
                  padding: '8px 12px', 
                  background: nodeStyles[selectedNode.data?.nodeType]?.background || '#333',
                  borderRadius: 4,
                  color: '#fff',
                  fontWeight: 'bold',
                }}>
                  {nodeTypeLabels[selectedNode.data?.nodeType] || selectedNode.data?.nodeType}
                </div>
              </div>
              
              <div style={{ marginBottom: 15 }}>
                <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Label</label>
                <input
                  type="text"
                  value={selectedNode.data?.label || ''}
                  onChange={(e) => {
                    setNodes(nodes.map(n => 
                      n.id === selectedNode.id 
                        ? { ...n, data: { ...n.data, label: e.target.value } }
                        : n
                    ))
                    setSelectedNode(prev => ({
                      ...prev,
                      data: { ...prev.data, label: e.target.value }
                    }))
                  }}
                  style={{
                    width: '100%',
                    padding: '8px 12px',
                    background: '#1a1a2e',
                    border: '1px solid #333',
                    borderRadius: 4,
                    color: '#fff',
                  }}
                />
              </div>

              {selectedNode.data?.nodeType === 'device_action' && (
                <>
                  <div style={{ marginBottom: 15 }}>
                    <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Robot</label>
                    <select
                      value={selectedNode.data?.robot || ''}
                      onChange={(e) => {
                        const newData = { ...selectedNode.data, robot: e.target.value }
                        setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                        setSelectedNode(prev => ({ ...prev, data: newData }))
                      }}
                      style={{
                        width: '100%',
                        padding: '8px 12px',
                        background: '#1a1a2e',
                        border: '1px solid #333',
                        borderRadius: 4,
                        color: '#fff',
                      }}
                    >
                      <option value="">Select robot...</option>
                      {devices.filter(d => d.name?.includes('PF400') || d.ui_type === 'PF400 Robot').map(d => (
                        <option key={d.name} value={d.name}>{d.name}</option>
                      ))}
                    </select>
                  </div>
                  
                  <div style={{ marginBottom: 15 }}>
                    <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Action</label>
                    <select
                      value={selectedNode.data?.action || ''}
                      onChange={(e) => {
                        const newData = { ...selectedNode.data, action: e.target.value }
                        setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                        setSelectedNode(prev => ({ ...prev, data: newData }))
                      }}
                      style={{
                        width: '100%',
                        padding: '8px 12px',
                        background: '#1a1a2e',
                        border: '1px solid #333',
                        borderRadius: 4,
                        color: '#fff',
                      }}
                    >
                      <option value="">Select action...</option>
                      <option value="pick-place">Pick & Place</option>
                      <option value="move-to">Move To</option>
                      <option value="safe">Safe Position</option>
                      <option value="home">Home</option>
                    </select>
                  </div>

                  {selectedNode.data?.action === 'pick-place' && (
                    <>
                      <div style={{ marginBottom: 15 }}>
                        <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Labware</label>
                        <select
                          value={selectedNode.data?.labware_id || ''}
                          onChange={(e) => {
                            const newData = { ...selectedNode.data, labware_id: e.target.value }
                            setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                            setSelectedNode(prev => ({ ...prev, data: newData }))
                          }}
                          style={{
                            width: '100%',
                            padding: '8px 12px',
                            background: '#1a1a2e',
                            border: '1px solid #333',
                            borderRadius: 4,
                            color: '#fff',
                          }}
                        >
                          <option value="">Select labware...</option>
                          {workflowLabware.map(lw => (
                            <option key={lw.id} value={lw.id}>{lw.name} ({lw.type})</option>
                          ))}
                        </select>
                        {workflowLabware.length === 0 && (
                          <div style={{ color: '#f59e0b', fontSize: '0.8em', marginTop: 4 }}>
                            Add labware in the Labware section below
                          </div>
                        )}
                      </div>

                      <div style={{ marginBottom: 15 }}>
                        <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Pick From (Source)</label>
                        <select
                          value={selectedNode.data?.source_device || ''}
                          onChange={(e) => {
                            const newData = { ...selectedNode.data, source_device: e.target.value }
                            setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                            setSelectedNode(prev => ({ ...prev, data: newData }))
                          }}
                          style={{
                            width: '100%',
                            padding: '8px 12px',
                            background: '#1a1a2e',
                            border: '1px solid #333',
                            borderRadius: 4,
                            color: '#fff',
                          }}
                        >
                          <option value="">Select source...</option>
                          {devices.map(d => (
                            <option key={d.name} value={d.name}>{d.name}</option>
                          ))}
                        </select>
                      </div>

                      <div style={{ marginBottom: 15 }}>
                        <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Place At (Target)</label>
                        <select
                          value={selectedNode.data?.target_device || ''}
                          onChange={(e) => {
                            const newData = { ...selectedNode.data, target_device: e.target.value }
                            setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                            setSelectedNode(prev => ({ ...prev, data: newData }))
                          }}
                          style={{
                            width: '100%',
                            padding: '8px 12px',
                            background: '#1a1a2e',
                            border: '1px solid #333',
                            borderRadius: 4,
                            color: '#fff',
                          }}
                        >
                          <option value="">Select target...</option>
                          {devices.map(d => (
                            <option key={d.name} value={d.name}>{d.name}</option>
                          ))}
                        </select>
                      </div>
                    </>
                  )}

                  {selectedNode.data?.action === 'move-to' && (
                    <div style={{ marginBottom: 15 }}>
                      <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Target Device/Position</label>
                      <select
                        value={selectedNode.data?.target_device || ''}
                        onChange={(e) => {
                          const newData = { ...selectedNode.data, target_device: e.target.value }
                          setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                          setSelectedNode(prev => ({ ...prev, data: newData }))
                        }}
                        style={{
                          width: '100%',
                          padding: '8px 12px',
                          background: '#1a1a2e',
                          border: '1px solid #333',
                          borderRadius: 4,
                          color: '#fff',
                        }}
                      >
                        <option value="">Select target...</option>
                        {devices.map(d => (
                          <option key={d.name} value={d.name}>{d.name}</option>
                        ))}
                      </select>
                    </div>
                  )}
                </>
              )}

              {selectedNode.data?.nodeType === 'delay' && (
                <div style={{ marginBottom: 15 }}>
                  <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Delay (ms)</label>
                  <input
                    type="number"
                    value={selectedNode.data?.delay_ms || 1000}
                    onChange={(e) => {
                      const newData = { ...selectedNode.data, delay_ms: parseInt(e.target.value) || 1000 }
                      setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                      setSelectedNode(prev => ({ ...prev, data: newData }))
                    }}
                    style={{
                      width: '100%',
                      padding: '8px 12px',
                      background: '#1a1a2e',
                      border: '1px solid #333',
                      borderRadius: 4,
                      color: '#fff',
                    }}
                  />
                </div>
              )}

              {selectedNode.data?.nodeType === 'conditional' && (
                <div style={{ marginBottom: 15 }}>
                  <label style={{ color: '#888', display: 'block', marginBottom: 4 }}>Condition</label>
                  <input
                    type="text"
                    value={selectedNode.data?.condition || ''}
                    placeholder="e.g., temperature > 25"
                    onChange={(e) => {
                      const newData = { ...selectedNode.data, condition: e.target.value }
                      setNodes(nodes.map(n => n.id === selectedNode.id ? { ...n, data: newData } : n))
                      setSelectedNode(prev => ({ ...prev, data: newData }))
                    }}
                    style={{
                      width: '100%',
                      padding: '8px 12px',
                      background: '#1a1a2e',
                      border: '1px solid #333',
                      borderRadius: 4,
                      color: '#fff',
                    }}
                  />
                </div>
              )}

              <button
                onClick={() => {
                  setNodes(nodes.filter(n => n.id !== selectedNode.id))
                  setEdges(edges.filter(e => e.source !== selectedNode.id && e.target !== selectedNode.id))
                  setSelectedNode(null)
                }}
                style={{
                  width: '100%',
                  padding: '8px',
                  background: '#ef4444',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  cursor: 'pointer',
                  marginTop: 10,
                }}
              >
                Delete Node
              </button>
            </div>
          ) : (
            <div style={{ color: '#666' }}>
              Click a node to edit its properties
            </div>
          )}
        </div>

        {/* Workflow Labware Inventory */}
        {selectedWorkflow && (
          <div style={{ padding: 15, borderBottom: '1px solid #333', maxHeight: 200, overflow: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}>
              <h4 style={{ color: '#fff', margin: 0 }}>🧫 Labware</h4>
              <button
                onClick={() => {
                  const name = prompt('Labware name (e.g., "Plate 1"):')
                  if (!name) return
                  const newLabware = {
                    id: `lw-${Date.now()}`,
                    name,
                    type: labwareTypes[0]?.name || 'Unknown',
                    initial_location: '',
                  }
                  setWorkflowLabware([...workflowLabware, newLabware])
                  log(`+ Added labware: ${name}`)
                }}
                style={{
                  padding: '4px 10px',
                  background: '#1890ff',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  cursor: 'pointer',
                  fontSize: '0.85em',
                }}
              >
                + Add
              </button>
            </div>
            
            {workflowLabware.length === 0 ? (
              <div style={{ color: '#666', fontSize: '0.85em', textAlign: 'center', padding: 10 }}>
                No labware defined for this workflow
              </div>
            ) : (
              workflowLabware.map((lw, idx) => (
                <div key={lw.id} style={{
                  padding: 8,
                  background: '#1a1a2e',
                  borderRadius: 4,
                  marginBottom: 6,
                  border: '1px solid #333',
                }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                    <input
                      type="text"
                      value={lw.name}
                      onChange={(e) => {
                        const updated = [...workflowLabware]
                        updated[idx] = { ...lw, name: e.target.value }
                        setWorkflowLabware(updated)
                      }}
                      style={{
                        flex: 1,
                        padding: '4px 8px',
                        background: '#0a0a0a',
                        border: '1px solid #444',
                        borderRadius: 3,
                        color: '#fff',
                        fontSize: '0.9em',
                        fontWeight: 'bold',
                      }}
                    />
                    <button
                      onClick={() => {
                        setWorkflowLabware(workflowLabware.filter((_, i) => i !== idx))
                        log(`- Removed labware: ${lw.name}`)
                      }}
                      style={{
                        marginLeft: 8,
                        padding: '4px 8px',
                        background: '#ef4444',
                        color: '#fff',
                        border: 'none',
                        borderRadius: 3,
                        cursor: 'pointer',
                        fontSize: '0.8em',
                      }}
                    >
                      ✕
                    </button>
                  </div>
                  <div style={{ display: 'flex', gap: 8 }}>
                    <select
                      value={lw.type}
                      onChange={(e) => {
                        const updated = [...workflowLabware]
                        updated[idx] = { ...lw, type: e.target.value }
                        setWorkflowLabware(updated)
                      }}
                      style={{
                        flex: 1,
                        padding: '4px 8px',
                        background: '#0a0a0a',
                        border: '1px solid #444',
                        borderRadius: 3,
                        color: '#aaa',
                        fontSize: '0.8em',
                      }}
                    >
                      <option value="">Labware type...</option>
                      {labwareTypes.map(lt => (
                        <option key={lt._id || lt.name} value={lt.name}>{lt.name}</option>
                      ))}
                    </select>
                    <select
                      value={lw.initial_location || ''}
                      onChange={(e) => {
                        const updated = [...workflowLabware]
                        updated[idx] = { ...lw, initial_location: e.target.value }
                        setWorkflowLabware(updated)
                      }}
                      style={{
                        flex: 1,
                        padding: '4px 8px',
                        background: '#0a0a0a',
                        border: '1px solid #444',
                        borderRadius: 3,
                        color: '#aaa',
                        fontSize: '0.8em',
                      }}
                    >
                      <option value="">Start location...</option>
                      {devices.map(d => (
                        <option key={d.name} value={d.name}>{d.name}</option>
                      ))}
                    </select>
                  </div>
                </div>
              ))
            )}
          </div>
        )}

        {/* Logs */}
        <div style={{ height: 200, overflow: 'auto', padding: 10, background: '#111' }}>
          <div style={{ color: '#888', marginBottom: 8, fontWeight: 'bold' }}>Activity Log</div>
          {logs.map((msg, i) => (
            <div key={i} style={{ 
              fontSize: '0.8em', 
              color: msg.includes('✓') ? '#10b981' : msg.includes('✗') ? '#ef4444' : '#888',
              marginBottom: 4,
              fontFamily: 'monospace',
              textAlign: 'left',
            }}>
              {msg}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default WorkflowsDashboard

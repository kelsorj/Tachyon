function ToolsDashboard() {
  return (
    <div style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 30 }}>
        <h1 style={{ fontSize: '2em', marginBottom: 10 }}>Tools</h1>
        <p style={{ color: '#888', fontSize: '1.1em', marginBottom: 20 }}>
          Tools are what a robot can pick up to interact with labware (grippers, pipette heads, probes, etc).
        </p>
      </div>

      <div style={{
        padding: 30,
        background: '#1a1a2e',
        borderRadius: 8,
        border: '1px solid #333'
      }}>
        <div style={{ fontSize: '1.1em', color: '#fff', fontWeight: 'bold', marginBottom: 8 }}>
          Coming soon
        </div>
        <div style={{ color: '#aaa', lineHeight: 1.5 }}>
          Next we can add:
          <ul style={{ marginTop: 10 }}>
            <li>Tool inventory (per robot)</li>
            <li>Tool change / pickup / drop actions</li>
            <li>Compatibility rules (tool ↔ labware ↔ protocol step)</li>
          </ul>
        </div>
      </div>
    </div>
  )
}

export default ToolsDashboard




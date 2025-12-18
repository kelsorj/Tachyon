function LabwareDashboard() {
  return (
    <div style={{ padding: 20, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 30 }}>
        <h1 style={{ fontSize: '2em', marginBottom: 10 }}>Labware</h1>
        <p style={{ color: '#888', fontSize: '1.1em', marginBottom: 20 }}>
          Labware are the physical objects robots move around (plates, lids, racks, carriers, tips, etc).
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
            <li>Labware catalog (types + geometry)</li>
            <li>Instances (barcode + current location)</li>
            <li>Validation (what tools can pick up what labware)</li>
          </ul>
        </div>
      </div>
    </div>
  )
}

export default LabwareDashboard



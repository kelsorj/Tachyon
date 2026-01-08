import { BrowserRouter, Routes, Route, Navigate, useParams } from 'react-router-dom'
import Layout from './components/Layout'
import DeviceDashboard from './components/DeviceDashboard'
import LabwareDashboard from './components/LabwareDashboard'
import ToolsDashboard from './components/ToolsDashboard'
import WorkflowsDashboard from './components/WorkflowsDashboard'
import PF400Diagnostics from './components/PF400Diagnostics'
import PlanarMotorDiagnostics from './components/PlanarMotorDiagnostics'
import './App.css'

function DeviceDiagnosticsRouter() {
  const { deviceName } = useParams()
  
  // Determine which diagnostic component to use based on device name or type
  // For now, we'll check the device name pattern
  if (deviceName && deviceName.toLowerCase().includes('planar')) {
    return <PlanarMotorDiagnostics />
  }
  
  // Default to PF400 for now (can be enhanced with device type lookup)
  return <PF400Diagnostics />
}

function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<Navigate to="/devices" replace />} />
          <Route path="/devices" element={<DeviceDashboard />} />
          <Route path="/labware" element={<LabwareDashboard />} />
          <Route path="/workflows" element={<WorkflowsDashboard />} />
          <Route path="/tools" element={<ToolsDashboard />} />
          <Route path="/devices/:deviceName/diagnostics" element={<DeviceDiagnosticsRouter />} />
          <Route path="*" element={<Navigate to="/devices" replace />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  )
}

export default App

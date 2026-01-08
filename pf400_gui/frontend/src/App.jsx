import { BrowserRouter, Routes, Route, Navigate, useParams } from 'react-router-dom'
import Layout from './components/Layout'
import DeviceDashboard from './components/DeviceDashboard'
import LabwareDashboard from './components/LabwareDashboard'
import ToolsDashboard from './components/ToolsDashboard'
import WorkflowsDashboard from './components/WorkflowsDashboard'
import PF400Diagnostics from './components/PF400Diagnostics'
import PlanarMotorDiagnostics from './components/PlanarMotorDiagnostics'
import PlatePadDiagnostics from './components/PlatePadDiagnostics'
import './App.css'

function DeviceDiagnosticsRouter() {
  const { deviceName } = useParams()
  
  // Determine which diagnostic component to use based on device name or type
  // Check for Planar Motor devices
  if (deviceName && deviceName.toLowerCase().includes('planar')) {
    return <PlanarMotorDiagnostics />
  }
  
  // Check for Plate Pad devices (matches PlatePad-xxx, Plate Pad xxx, platepad, etc.)
  if (deviceName && deviceName.toLowerCase().replace(/[\s-_]/g, '').includes('platepad')) {
    return <PlatePadDiagnostics />
  }
  
  // Default to PF400 for robot devices
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

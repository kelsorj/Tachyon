import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import Layout from './components/Layout'
import DeviceDashboard from './components/DeviceDashboard'
import PF400Diagnostics from './components/PF400Diagnostics'
import './App.css'

function App() {
  return (
    <BrowserRouter>
      <Layout>
        <Routes>
          <Route path="/" element={<DeviceDashboard />} />
          <Route path="/devices/:deviceName/diagnostics" element={<PF400Diagnostics />} />
          {/* Add more device routes here as needed */}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  )
}

export default App

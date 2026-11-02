import { Routes, Route } from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import QuoteFormPage from './pages/QuoteFormPage'
import { Toaster } from 'react-hot-toast'

function App() {
  return (
    <>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/quote" element={<QuoteFormPage />} />
      </Routes>
      <Toaster position="top-right" toastOptions={{ duration: 4000, style: { background: '#333', color: '#fff', borderRadius: '8px', }, }} />
    </>
  )
}

export default App
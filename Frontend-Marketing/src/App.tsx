import { Routes, Route } from 'react-router-dom'
import LandingPage from './pages/LandingPage'
import QuoteFormPage from './pages/QuoteFormPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/quote" element={<QuoteFormPage />} />
    </Routes>
  )
}

export default App
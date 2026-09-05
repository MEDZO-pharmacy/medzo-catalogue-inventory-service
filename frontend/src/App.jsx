import { BrowserRouter as Router, Routes, Route, Outlet } from 'react-router-dom'
import Header from './components/Header'
import Footer from './components/Footer'
import Home from './pages/Home'
import Services from './pages/Services'
import AboutUs from './pages/AboutUs'
import Contact from './pages/Contact'
import Products from './pages/Products'
import ReadMore from './pages/ReadMore'
import Login from './pages/Login'
import SignUp from './pages/SignUp'
import { AuthProvider } from './auth/AuthContext'
import ProtectedRoute from './auth/ProtectedRoute'
import PharmacistDashboard from './pages/PharmacistDashboard'
import AdminDashboard from './pages/AdminDashboard'
import CataloguePage from './features/catalogue/pages/CataloguePage'
import AddMedicinePage from './features/catalogue/pages/AddMedicinePage'
import EditMedicinePage from './features/catalogue/pages/EditMedicinePage'
import InventoryDashboardPage from './features/inventory/pages/InventoryDashboardPage'
import RecordBatchPage from './features/inventory/pages/RecordBatchPage'
import LowStockPage from './features/inventory/pages/LowStockPage'
import './index.css'

const MainLayout = () => {
  return (
    <>
      <Header />
      <Outlet />
      <Footer />
    </>
  )
}

function App() {
  return (
    <Router>
      <AuthProvider>
        <div className="min-h-screen bg-white font-sans text-[#1a202c]">
          <Routes>
            <Route element={<MainLayout />}>
              <Route path="/" element={<Home />} />
              <Route path="/services" element={<Services />} />
              <Route path="/about" element={<AboutUs />} />
              <Route path="/contact" element={<Contact />} />
              <Route path="/read-more" element={<ReadMore />} />
              <Route path="/products" element={<Products />} />
            </Route>
            <Route element={<ProtectedRoute roles={['Pharmacist']} />}>
              <Route path="/pharmacist" element={<PharmacistDashboard />} />
            </Route>
            <Route element={<ProtectedRoute roles={['Pharmacist', 'InventoryManager', 'Admin']} />}>
              <Route path="/catalogue" element={<CataloguePage />} />
              <Route path="/inventory" element={<InventoryDashboardPage />} />
            </Route>
            <Route element={<ProtectedRoute roles={['InventoryManager', 'Admin']} />}>
              <Route path="/catalogue/new" element={<AddMedicinePage />} />
              <Route path="/catalogue/:medicineId/edit" element={<EditMedicinePage />} />
              <Route path="/inventory/batches/new" element={<RecordBatchPage />} />
              <Route path="/inventory/low-stock" element={<LowStockPage />} />
            </Route>
            <Route element={<ProtectedRoute roles={['Admin']} />}>
              <Route path="/admin" element={<AdminDashboard />} />
            </Route>
            <Route path="/login" element={<Login />} />
            <Route path="/signup" element={<SignUp />} />
          </Routes>
        </div>
      </AuthProvider>
    </Router>
  )
}

export default App

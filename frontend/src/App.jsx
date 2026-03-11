import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './context/AuthContext.jsx'
import ProtectedRoute from './components/ProtectedRoute.jsx'
import Layout from './components/Layout.jsx'
import Login from './pages/auth/Login.jsx'
import Register from './pages/auth/Register.jsx'
import Profile from './pages/profile/Profile.jsx'
import ContactList from './pages/contacts/ContactList.jsx'
import ContactDetail from './pages/contacts/ContactDetail.jsx'
import ContactForm from './pages/contacts/ContactForm.jsx'
import AccountList from './pages/accounts/AccountList.jsx'
import AccountDetail from './pages/accounts/AccountDetail.jsx'
import AccountForm from './pages/accounts/AccountForm.jsx'
import Pipeline from './pages/deals/Pipeline.jsx'
import DealDetail from './pages/deals/DealDetail.jsx'
import DealForm from './pages/deals/DealForm.jsx'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route index element={<Navigate to="/contacts" replace />} />
              <Route path="/contacts" element={<ContactList />} />
              <Route path="/contacts/new" element={<ContactForm />} />
              <Route path="/contacts/:id" element={<ContactDetail />} />
              <Route path="/contacts/:id/edit" element={<ContactForm />} />
              <Route path="/accounts" element={<AccountList />} />
              <Route path="/accounts/new" element={<AccountForm />} />
              <Route path="/accounts/:id" element={<AccountDetail />} />
              <Route path="/accounts/:id/edit" element={<AccountForm />} />
              <Route path="/deals" element={<Pipeline />} />
              <Route path="/deals/new" element={<DealForm />} />
              <Route path="/deals/:id" element={<DealDetail />} />
              <Route path="/deals/:id/edit" element={<DealForm />} />
              <Route path="/profile" element={<Profile />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/contacts" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

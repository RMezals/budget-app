import { useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, Navigate, NavLink } from 'react-router-dom'
import { onAuthStateChanged, signOut, type User } from 'firebase/auth'
import { auth, firebaseConfigured } from './firebase'
import LoginPage from './modules/auth/LoginPage'
import DashboardPage from './modules/dashboard/DashboardPage'
import TransactionsPage from './modules/transactions/TransactionsPage'
import SavingsPage from './modules/savings/SavingsPage'
import PortfolioPage from './modules/portfolio/PortfolioPage'

function App() {
  // When Firebase is not configured, skip auth and go straight to the app
  const [user, setUser] = useState<User | null | undefined>(
    firebaseConfigured ? undefined : {} as User
  )

  useEffect(() => {
    if (!firebaseConfigured) return
    return onAuthStateChanged(auth, setUser)
  }, [])

  if (user === undefined) return null // waiting for Firebase to initialise

  if (!user) return <LoginPage />

  return (
    <BrowserRouter>
      <nav className="navbar navbar-expand navbar-dark bg-dark px-4">
        <span className="navbar-brand fw-bold">💰 BudgetApp</span>
        <div className="navbar-nav me-auto">
          <NavLink to="/"             className={({ isActive }) => 'nav-link' + (isActive ? ' active' : '')}>Dashboard</NavLink>
          <NavLink to="/transactions" className={({ isActive }) => 'nav-link' + (isActive ? ' active' : '')}>Transactions</NavLink>
          <NavLink to="/savings"      className={({ isActive }) => 'nav-link' + (isActive ? ' active' : '')}>Savings</NavLink>
          <NavLink to="/portfolio"    className={({ isActive }) => 'nav-link' + (isActive ? ' active' : '')}>Portfolio</NavLink>
        </div>
        {firebaseConfigured && (
          <button className="btn btn-outline-light btn-sm" onClick={() => signOut(auth)}>Sign out</button>
        )}
      </nav>
      <div className="container py-4">
        <Routes>
          <Route path="/"             element={<DashboardPage />} />
          <Route path="/transactions" element={<TransactionsPage />} />
          <Route path="/savings"      element={<SavingsPage />} />
          <Route path="/portfolio"    element={<PortfolioPage />} />
          <Route path="*"             element={<Navigate to="/" />} />
        </Routes>
      </div>
    </BrowserRouter>
  )
}

export default App

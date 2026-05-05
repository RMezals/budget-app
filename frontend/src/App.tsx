import { useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, Navigate, NavLink } from 'react-router-dom'
import { onAuthStateChanged, signOut, type User } from 'firebase/auth'
import { auth } from './firebase'
import LoginPage from './modules/auth/LoginPage'
import DashboardPage from './modules/dashboard/DashboardPage'
import TransactionsPage from './modules/transactions/TransactionsPage'
import SavingsPage from './modules/savings/SavingsPage'
import PortfolioPage from './modules/portfolio/PortfolioPage'

function App() {
  const [user, setUser] = useState<User | null | undefined>(undefined)

  useEffect(() => onAuthStateChanged(auth, setUser), [])

  if (user === undefined) return null // waiting for Firebase to initialise

  if (!user) return <LoginPage />

  return (
    <BrowserRouter>
      <nav>
        <NavLink to="/">Dashboard</NavLink>
        <NavLink to="/transactions">Transactions</NavLink>
        <NavLink to="/savings">Savings</NavLink>
        <NavLink to="/portfolio">Portfolio</NavLink>
        <button onClick={() => signOut(auth)}>Sign out</button>
      </nav>
      <Routes>
        <Route path="/"             element={<DashboardPage />} />
        <Route path="/transactions" element={<TransactionsPage />} />
        <Route path="/savings"      element={<SavingsPage />} />
        <Route path="/portfolio"    element={<PortfolioPage />} />
        <Route path="*"             element={<Navigate to="/" />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App

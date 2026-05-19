import AppNavLink from '@/components/AppNavLink';
import ErrorBoundary from '@/components/ErrorBoundary';
import { CurrencyProvider } from '@/contexts/CurrencyContext';
import { auth, firebaseConfigured } from '@/firebase';
import LoginPage from '@/modules/auth/LoginPage';
import DashboardPage from '@/modules/dashboard/DashboardPage';
import PortfolioPage from '@/modules/portfolio/PortfolioPage';
import SavingsPage from '@/modules/savings/SavingsPage';
import TransactionsPage from '@/modules/transactions/TransactionsPage';
import { type User, onAuthStateChanged, signOut } from 'firebase/auth';
import { useEffect, useState } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import AppNavLink from './components/AppNavLink';
import ErrorBoundary from './components/ErrorBoundary';
import { CurrencyProvider } from './contexts/CurrencyContext';
import GoalPage from './modules/savings/GoalPage';

type AuthState =
  | { status: 'loading' }
  | { status: 'authenticated'; user: User }
  | { status: 'unauthenticated' }
  | { status: 'disabled' };

function App() {
  const [authState, setAuthState] = useState<AuthState>(
    firebaseConfigured ? { status: 'loading' } : { status: 'disabled' },
  );

  useEffect(() => {
    if (!firebaseConfigured || !auth) return;
    return onAuthStateChanged(auth, (user) => {
      setAuthState(user ? { status: 'authenticated', user } : { status: 'unauthenticated' });
    });
  }, []);

  if (authState.status === 'loading') return null;

  if (authState.status === 'unauthenticated') return <LoginPage />;

  const isFirebaseEnabled = firebaseConfigured && auth;

  return (
    <ErrorBoundary>
      <CurrencyProvider>
        <BrowserRouter>
          <nav className="navbar navbar-expand navbar-dark bg-dark px-4">
            <span className="navbar-brand fw-bold">💰 BudgetApp</span>
            <div className="navbar-nav me-auto">
              <AppNavLink to="/">Dashboard</AppNavLink>
              <AppNavLink to="/transactions">Transactions</AppNavLink>
              <AppNavLink to="/savings">Savings</AppNavLink>
              <AppNavLink to="/portfolio">Portfolio</AppNavLink>
            </div>
            {isFirebaseEnabled && (
              <button
                type="button"
                className="btn btn-outline-light btn-sm"
                onClick={() => auth && signOut(auth)}
              >
                Sign out
              </button>
            )}
          </nav>
          <div className="container py-4">
            <Routes>
              <Route path="/" element={<DashboardPage />} />
              <Route path="/transactions" element={<TransactionsPage />} />
              <Route path="/savings" element={<SavingsPage />} />
              <Route path="/savings/:goalId" element={<GoalPage />} />
              <Route path="/portfolio" element={<PortfolioPage />} />
              <Route path="*" element={<Navigate to="/" />} />
            </Routes>
          </div>
        </BrowserRouter>
      </CurrencyProvider>
    </ErrorBoundary>
  );
}

export default App;

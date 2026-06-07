import AppNavLink from '@/components/AppNavLink';
import ErrorBoundary from '@/components/ErrorBoundary';
import { CurrencyProvider } from '@/contexts/CurrencyContext';
import { auth, firebaseConfigured } from '@/firebase';
import LoginPage from '@/modules/auth/LoginPage';
import ProfileModal from '@/modules/auth/ProfileModal';
import DashboardPage from '@/modules/dashboard/DashboardPage';
import PortfolioPage from '@/modules/portfolio/PortfolioPage';
import ReportsPage from '@/modules/reports/ReportsPage';
import SavingsPage from '@/modules/savings/SavingsPage';
import TransactionsPage from '@/modules/transactions/TransactionsPage';
import { type User, onAuthStateChanged, signOut } from 'firebase/auth';
import { useEffect, useState } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import GoalPage from '@/modules/savings/GoalPage';

type AuthState =
  | { status: 'loading' }
  | { status: 'authenticated'; user: User }
  | { status: 'unauthenticated' }
  | { status: 'disabled' };

const IconDashboard = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <rect x="3" y="3" width="7" height="7" rx="1" />
    <rect x="14" y="3" width="7" height="7" rx="1" />
    <rect x="3" y="14" width="7" height="7" rx="1" />
    <rect x="14" y="14" width="7" height="7" rx="1" />
  </svg>
);

const IconTransactions = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M16 3l4 4-4 4" />
    <path d="M20 7H4" />
    <path d="M8 21l-4-4 4-4" />
    <path d="M4 17h16" />
  </svg>
);

const IconSavings = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M12 2a10 10 0 0 1 10 10c0 5.52-4.48 10-10 10S2 17.52 2 12" />
    <path d="M12 6v6l4 2" />
  </svg>
);

const IconPortfolio = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <polyline points="22 12 18 12 15 21 9 3 6 12 2 12" />
  </svg>
);

const IconSignOut = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <polyline points="16 17 21 12 16 7" />
    <line x1="21" y1="12" x2="9" y2="12" />
  </svg>
);

const IconReports = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
    <polyline points="14 2 14 8 20 8" />
    <line x1="16" y1="13" x2="8" y2="13" />
    <line x1="16" y1="17" x2="8" y2="17" />
    <polyline points="10 9 9 9 8 9" />
  </svg>
);

const IconChevronLeft = () => (
  <svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2.5"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <polyline points="15 18 9 12 15 6" />
  </svg>
);

function getInitials(user: User): string {
  if (user.displayName) {
    return user.displayName
      .split(' ')
      .slice(0, 2)
      .map((n) => n[0])
      .join('')
      .toUpperCase();
  }
  return (user.email?.[0] ?? '?').toUpperCase();
}

function App() {
  const [authState, setAuthState] = useState<AuthState>(
    firebaseConfigured ? { status: 'loading' } : { status: 'disabled' },
  );
  const [collapsed, setCollapsed] = useState(false);
  const [showProfile, setShowProfile] = useState(false);

  useEffect(() => {
    if (!firebaseConfigured || !auth) return;
    return onAuthStateChanged(auth, (user) => {
      setAuthState(user ? { status: 'authenticated', user } : { status: 'unauthenticated' });
    });
  }, []);

  if (authState.status === 'loading') return null;

  if (authState.status === 'unauthenticated') return <LoginPage />;

  const isFirebaseEnabled = firebaseConfigured && auth;
  const user = authState.status === 'authenticated' ? authState.user : null;
  const displayName = user?.displayName ?? user?.email ?? 'User';

  return (
    <ErrorBoundary>
      <CurrencyProvider>
        <BrowserRouter>
          <div className={`app-layout${collapsed ? ' sidebar-collapsed' : ''}`}>
            <aside className="app-sidebar">
              <button
                type="button"
                className="app-sidebar-toggle"
                onClick={() => setCollapsed((c) => !c)}
                title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
              >
                <IconChevronLeft />
              </button>

              <div className="app-sidebar-logo">
                <div className="app-sidebar-logo-icon">💰</div>
                <span className="app-sidebar-logo-text">BudgetApp</span>
              </div>

              <nav className="app-sidebar-nav">
                <AppNavLink to="/" icon={<IconDashboard />}>
                  Dashboard
                </AppNavLink>
                <AppNavLink to="/transactions" icon={<IconTransactions />}>
                  Transactions
                </AppNavLink>
                <AppNavLink to="/savings" icon={<IconSavings />}>
                  Savings
                </AppNavLink>
                <AppNavLink to="/portfolio" icon={<IconPortfolio />}>
                  Portfolio
                </AppNavLink>
                <AppNavLink to="/reports" icon={<IconReports />}>
                  Reports
                </AppNavLink>
              </nav>

              <div className="app-sidebar-footer">
                {user && (
                  <button
                    type="button"
                    className="app-sidebar-user app-sidebar-user-btn"
                    onClick={() => setShowProfile(true)}
                    title="Account settings"
                  >
                    <div className="app-sidebar-avatar">{getInitials(user)}</div>
                    <span className="app-sidebar-username">{displayName}</span>
                  </button>
                )}
                {isFirebaseEnabled && (
                  <button
                    type="button"
                    className="app-sidebar-signout"
                    onClick={() => auth && signOut(auth)}
                  >
                    <IconSignOut />
                    <span className="nav-label">Sign out</span>
                  </button>
                )}
              </div>

              {showProfile && user && (
                <ProfileModal user={user} onClose={() => setShowProfile(false)} />
              )}
            </aside>

            <main className="app-main">
              <div className="app-content">
                <Routes>
                  <Route path="/" element={<DashboardPage />} />
                  <Route path="/transactions" element={<TransactionsPage />} />
                  <Route path="/savings" element={<SavingsPage />} />
                  <Route path="/savings/:goalId" element={<GoalPage />} />
                  <Route path="/portfolio" element={<PortfolioPage />} />
                  <Route path="/reports" element={<ReportsPage />} />
                  <Route path="*" element={<Navigate to="/" />} />
                </Routes>
              </div>
            </main>
          </div>
        </BrowserRouter>
      </CurrencyProvider>
    </ErrorBoundary>
  );
}

export default App;

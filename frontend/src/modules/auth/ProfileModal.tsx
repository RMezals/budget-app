import { apiFetch } from '@/api/client';
import type { UpdateProfileRequest } from '@/api/types';
import { useCurrency } from '@/contexts/CurrencyContext';
import { auth } from '@/firebase';
import { SUPPORTED_CURRENCIES } from '@/utils/currency/constants';
import {
  EmailAuthProvider,
  type User,
  reauthenticateWithCredential,
  updatePassword,
  updateProfile,
} from 'firebase/auth';
import { useState } from 'react';

interface Props {
  user: User;
  onClose: () => void;
}

// Each tab in the modal maps to one of these section keys
type Section = 'name' | 'email' | 'currency' | 'password' | 'dev';

// Maps Firebase error codes to user-friendly messages shown in the modal
function firebaseErrorMessage(code: string): string {
  switch (code) {
    case 'auth/wrong-password':
    case 'auth/invalid-credential':
      return 'Current password is incorrect.';
    case 'auth/weak-password':
      return 'New password must be at least 6 characters.';
    case 'auth/requires-recent-login':
      return 'Session expired. Please sign out and sign back in first.';
    case 'auth/too-many-requests':
      return 'Too many attempts. Try again later.';
    default:
      return 'Something went wrong. Please try again.';
  }
}

// Modal component that lets the signed-in user update their display name, email, currency, or password
export default function ProfileModal({ user, onClose }: Props) {
  // Controls which tab panel is visible; defaults to the name tab
  const [section, setSection] = useState<Section>('name');
  const { currency: currentCurrency, refreshCurrency } = useCurrency();

  // Name section state — separate error/success per tab to avoid cross-contamination
  const [nameValue, setNameValue] = useState(user.displayName ?? '');
  const [nameSaving, setNameSaving] = useState(false);
  const [nameSuccess, setNameSuccess] = useState('');
  const [nameError, setNameError] = useState('');

  // Email section state
  const [emailValue, setEmailValue] = useState(user.email ?? '');
  const [emailSaving, setEmailSaving] = useState(false);
  const [emailSuccess, setEmailSuccess] = useState('');
  const [emailError, setEmailError] = useState('');

  // Currency section state
  const [currencyValue, setCurrencyValue] = useState(currentCurrency);
  const [currencySaving, setCurrencySaving] = useState(false);
  const [currencySuccess, setCurrencySuccess] = useState('');
  const [currencyError, setCurrencyError] = useState('');

  // Password section state — three fields: current, new, confirm
  const [currentPw, setCurrentPw] = useState('');
  const [newPw, setNewPw] = useState('');
  const [confirmPw, setConfirmPw] = useState('');
  const [pwSaving, setPwSaving] = useState(false);
  const [pwSuccess, setPwSuccess] = useState('');
  const [pwError, setPwError] = useState('');

  // Dev tools section state
  const [seedLoading, setSeedLoading] = useState<string | null>(null);
  const [seedResult, setSeedResult] = useState<string | null>(null);
  const [seedError, setSeedError] = useState<string | null>(null);

  // Calls the Firebase Auth SDK directly — display name is not stored on the backend
  async function handleNameSave() {
    const trimmed = nameValue.trim();
    if (!trimmed) {
      setNameError('Name cannot be empty.');
      return;
    }
    setNameError('');
    setNameSuccess('');
    setNameSaving(true);
    try {
      await updateProfile(user, { displayName: trimmed });
      setNameSuccess('Display name updated.');
    } catch (e: unknown) {
      setNameError(firebaseErrorMessage((e as { code?: string }).code ?? ''));
    } finally {
      setNameSaving(false);
    }
  }

  // Sends the new email to the backend (which updates Firebase Admin + custom claims), then
  // reloads the local user object so subsequent reads reflect the change
  async function handleEmailSave() {
    const trimmed = emailValue.trim();
    if (!trimmed) {
      setEmailError('Email cannot be empty.');
      return;
    }
    // Guard: do not send a PUT if the email hasn't actually changed
    if (trimmed === user.email) {
      setEmailError('New email must be different from the current one.');
      return;
    }
    setEmailError('');
    setEmailSuccess('');
    setEmailSaving(true);
    try {
      await apiFetch('/api/auth/profile', {
        method: 'PUT',
        body: JSON.stringify({ email: trimmed } satisfies UpdateProfileRequest),
      });
      // Reload the Firebase user to sync the updated email locally
      await auth?.currentUser?.reload();
      setEmailSuccess('Email updated successfully.');
    } catch (e: unknown) {
      setEmailError((e as Error).message ?? 'Something went wrong. Please try again.');
    } finally {
      setEmailSaving(false);
    }
  }

  // Sends the selected currency to the backend, then refreshes the token so the currency
  // context picks up the new value immediately without requiring a sign-out/sign-in
  async function handleCurrencySave() {
    setCurrencyError('');
    setCurrencySuccess('');
    setCurrencySaving(true);
    try {
      await apiFetch('/api/auth/profile', {
        method: 'PUT',
        body: JSON.stringify({ currency: currencyValue } satisfies UpdateProfileRequest),
      });
      // refreshCurrency forces a token refresh so the updated claim is visible app-wide
      await refreshCurrency();
      setCurrencySuccess('Currency updated.');
    } catch (e: unknown) {
      setCurrencyError((e as Error).message ?? 'Something went wrong. Please try again.');
    } finally {
      setCurrencySaving(false);
    }
  }

  // Changing passwords in Firebase requires re-authentication first to confirm identity
  async function handlePasswordSave() {
    setPwError('');
    setPwSuccess('');
    if (!currentPw) {
      setPwError('Enter your current password.');
      return;
    }
    if (newPw.length < 6) {
      setPwError('New password must be at least 6 characters.');
      return;
    }
    if (newPw !== confirmPw) {
      setPwError('Passwords do not match.');
      return;
    }

    setPwSaving(true);
    try {
      if (!auth || !user.email) throw new Error('Not authenticated');
      // Build a credential from the current password and re-authenticate before allowing the change
      const credential = EmailAuthProvider.credential(user.email, currentPw);
      await reauthenticateWithCredential(user, credential);
      // Only call updatePassword after successful re-auth
      await updatePassword(user, newPw);
      setPwSuccess('Password changed successfully.');
      // Clear all password fields after a successful change
      setCurrentPw('');
      setNewPw('');
      setConfirmPw('');
    } catch (e: unknown) {
      setPwError(firebaseErrorMessage((e as { code?: string }).code ?? ''));
    } finally {
      setPwSaving(false);
    }
  }

  async function handleSeed(email: string) {
    setSeedLoading(email);
    setSeedResult(null);
    setSeedError(null);
    try {
      const data = await apiFetch('/api/dev/seed/user', {
        method: 'POST',
        body: JSON.stringify({ email }),
      });
      const r = data as {
        transactions: number;
        budgets: number;
        goals: number;
        contributions: number;
        assets: number;
        liabilities: number;
      };
      setSeedResult(
        `Done — ${r.transactions} transactions, ${r.budgets} budgets, ${r.goals} goals, ${r.assets} assets, ${r.liabilities} liabilities`,
      );
    } catch (e: unknown) {
      setSeedError((e as Error).message ?? 'Seed failed.');
    } finally {
      setSeedLoading(null);
    }
  }

  const tabs: { key: Section; label: string }[] = [
    { key: 'name', label: 'Display Name' },
    { key: 'email', label: 'Email' },
    { key: 'currency', label: 'Currency' },
    { key: 'password', label: 'Change Password' },
    { key: 'dev', label: 'Dev Tools' },
  ];

  return (
    <div
      className="pf-modal-overlay"
      onClick={onClose}
      onKeyDown={(e) => e.key === 'Escape' && onClose()}
    >
      <div
        className="pf-modal"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
      >
        <div className="pf-modal-header">
          <h5 className="pf-modal-title">Account Settings</h5>
          <button type="button" className="btn-close" onClick={onClose} aria-label="Close" />
        </div>

        <div style={{ borderBottom: '1px solid var(--color-border)', padding: '0 1.5rem' }}>
          <ul className="nav nav-tabs" style={{ borderBottom: 'none', marginBottom: '-1px' }}>
            {tabs.map(({ key, label }) => (
              <li key={key} className="nav-item">
                <button
                  type="button"
                  className={`nav-link${section === key ? ' active' : ''}`}
                  onClick={() => setSection(key)}
                >
                  {label}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="pf-modal-body">
          {section === 'name' && (
            <div>
              <p className="text-muted small mb-3">
                This name is shown in the sidebar and is stored in Firebase Auth only — not on any
                backend.
              </p>
              {nameError && <div className="alert alert-danger py-2 small mb-3">{nameError}</div>}
              {nameSuccess && (
                <div className="alert alert-success py-2 small mb-3">{nameSuccess}</div>
              )}
              <div className="mb-3">
                <label className="form-label" htmlFor="profile-name">
                  Display name
                </label>
                <input
                  id="profile-name"
                  className="form-control"
                  type="text"
                  value={nameValue}
                  onChange={(e) => {
                    setNameValue(e.target.value);
                    setNameSuccess('');
                    setNameError('');
                  }}
                  placeholder="Your name"
                  disabled={nameSaving}
                  // biome-ignore lint/a11y/noAutofocus: first field in modal
                  autoFocus
                />
              </div>
            </div>
          )}

          {section === 'email' && (
            <div>
              <p className="text-muted small mb-3">
                Update the email address associated with your account.
              </p>
              {emailError && <div className="alert alert-danger py-2 small mb-3">{emailError}</div>}
              {emailSuccess && (
                <div className="alert alert-success py-2 small mb-3">{emailSuccess}</div>
              )}
              <div className="mb-3">
                <label className="form-label" htmlFor="profile-email">
                  New email address
                </label>
                <input
                  id="profile-email"
                  className="form-control"
                  type="email"
                  value={emailValue}
                  onChange={(e) => {
                    setEmailValue(e.target.value);
                    setEmailSuccess('');
                    setEmailError('');
                  }}
                  disabled={emailSaving}
                  autoComplete="email"
                  // biome-ignore lint/a11y/noAutofocus: first field in modal
                  autoFocus
                />
              </div>
            </div>
          )}

          {section === 'currency' && (
            <div>
              <p className="text-muted small mb-3">
                Your preferred currency is used to display all monetary values throughout the app.
              </p>
              {currencyError && (
                <div className="alert alert-danger py-2 small mb-3">{currencyError}</div>
              )}
              {currencySuccess && (
                <div className="alert alert-success py-2 small mb-3">{currencySuccess}</div>
              )}
              <div className="mb-3">
                <label className="form-label" htmlFor="profile-currency">
                  Preferred currency
                </label>
                <select
                  id="profile-currency"
                  className="form-select"
                  value={currencyValue}
                  onChange={(e) => {
                    setCurrencyValue(e.target.value as (typeof SUPPORTED_CURRENCIES)[number]);
                    setCurrencySuccess('');
                    setCurrencyError('');
                  }}
                  disabled={currencySaving}
                >
                  {SUPPORTED_CURRENCIES.map((code) => (
                    <option key={code} value={code}>
                      {code}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          {section === 'dev' && (
            <div>
              <p className="text-muted small mb-3">
                Wipes your data and fills it with realistic test data. Only available in
                development.
              </p>
              {seedError && <div className="alert alert-danger py-2 small mb-3">{seedError}</div>}
              {seedResult && (
                <div className="alert alert-success py-2 small mb-3">{seedResult}</div>
              )}
              {(
                [
                  {
                    email: 'ausmoons@gmail.com',
                    label: 'Profile A',
                    description: '€3 200/mo · 3 goals · stocks + crypto · student loan',
                  },
                  {
                    email: 'test@test.com',
                    label: 'Profile B',
                    description: '€5 000/mo · 2 goals · ETFs only · debt-free',
                  },
                  {
                    email: 'endercave@gmail.com',
                    label: 'Profile C',
                    description: '€1 800/mo + gig · 3 goals · no investments · credit card debt',
                  },
                ] as const
              ).map(({ email, label, description }) => (
                <div key={email} className="card">
                  <div className="card-body py-2 px-3">
                    <div className="d-flex justify-content-between align-items-center">
                      <div>
                        <div className="small fw-semibold">
                          {label} — {email}
                        </div>
                        <div className="text-muted" style={{ fontSize: '0.75rem' }}>
                          {description}
                        </div>
                      </div>
                      <button
                        type="button"
                        className="btn btn-outline-primary btn-sm"
                        onClick={() => handleSeed(email)}
                        disabled={seedLoading !== null}
                      >
                        {seedLoading === email ? 'Seeding…' : 'Seed'}
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {section === 'password' && (
            <div>
              <p className="text-muted small mb-3">
                Enter your current password to verify, then choose a new one.
              </p>
              {pwError && <div className="alert alert-danger py-2 small mb-3">{pwError}</div>}
              {pwSuccess && <div className="alert alert-success py-2 small mb-3">{pwSuccess}</div>}
              <div className="mb-3">
                <label className="form-label" htmlFor="pw-current">
                  Current password
                </label>
                <input
                  id="pw-current"
                  className="form-control"
                  type="password"
                  value={currentPw}
                  onChange={(e) => {
                    setCurrentPw(e.target.value);
                    setPwError('');
                    setPwSuccess('');
                  }}
                  disabled={pwSaving}
                  autoComplete="current-password"
                  // biome-ignore lint/a11y/noAutofocus: first field in modal
                  autoFocus
                />
              </div>
              <div className="mb-3">
                <label className="form-label" htmlFor="pw-new">
                  New password
                </label>
                <input
                  id="pw-new"
                  className="form-control"
                  type="password"
                  value={newPw}
                  onChange={(e) => {
                    setNewPw(e.target.value);
                    setPwError('');
                    setPwSuccess('');
                  }}
                  placeholder="Min. 6 characters"
                  disabled={pwSaving}
                  autoComplete="new-password"
                />
              </div>
              <div className="mb-0">
                <label className="form-label" htmlFor="pw-confirm">
                  Confirm new password
                </label>
                <input
                  id="pw-confirm"
                  className="form-control"
                  type="password"
                  value={confirmPw}
                  onChange={(e) => {
                    setConfirmPw(e.target.value);
                    setPwError('');
                    setPwSuccess('');
                  }}
                  placeholder="Repeat password"
                  disabled={pwSaving}
                  autoComplete="new-password"
                />
              </div>
            </div>
          )}
        </div>

        <div className="pf-modal-footer">
          <div className="pf-modal-footer-actions">
            <button type="button" className="btn btn-outline-secondary btn-sm" onClick={onClose}>
              Close
            </button>
            {section === 'name' && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={handleNameSave}
                disabled={nameSaving || nameValue.trim() === (user.displayName ?? '')}
              >
                {nameSaving ? 'Saving…' : 'Save Name'}
              </button>
            )}
            {section === 'email' && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={handleEmailSave}
                disabled={emailSaving || emailValue.trim() === (user.email ?? '')}
              >
                {emailSaving ? 'Saving…' : 'Save Email'}
              </button>
            )}
            {section === 'currency' && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={handleCurrencySave}
                disabled={currencySaving || currencyValue === currentCurrency}
              >
                {currencySaving ? 'Saving…' : 'Save Currency'}
              </button>
            )}
            {section === 'password' && (
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={handlePasswordSave}
                disabled={pwSaving}
              >
                {pwSaving ? 'Updating…' : 'Change Password'}
              </button>
            )}
            {section === 'dev' && null}
          </div>
        </div>
      </div>
    </div>
  );
}

import { apiFetch } from '@/api/client';
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

type Section = 'name' | 'email' | 'currency' | 'password';

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

export default function ProfileModal({ user, onClose }: Props) {
  const [section, setSection] = useState<Section>('name');
  const { currency: currentCurrency, refreshCurrency } = useCurrency();

  // Name
  const [nameValue, setNameValue] = useState(user.displayName ?? '');
  const [nameSaving, setNameSaving] = useState(false);
  const [nameSuccess, setNameSuccess] = useState('');
  const [nameError, setNameError] = useState('');

  // Email
  const [emailValue, setEmailValue] = useState(user.email ?? '');
  const [emailSaving, setEmailSaving] = useState(false);
  const [emailSuccess, setEmailSuccess] = useState('');
  const [emailError, setEmailError] = useState('');

  // Currency
  const [currencyValue, setCurrencyValue] = useState(currentCurrency);
  const [currencySaving, setCurrencySaving] = useState(false);
  const [currencySuccess, setCurrencySuccess] = useState('');
  const [currencyError, setCurrencyError] = useState('');

  // Password
  const [currentPw, setCurrentPw] = useState('');
  const [newPw, setNewPw] = useState('');
  const [confirmPw, setConfirmPw] = useState('');
  const [pwSaving, setPwSaving] = useState(false);
  const [pwSuccess, setPwSuccess] = useState('');
  const [pwError, setPwError] = useState('');

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

  async function handleEmailSave() {
    const trimmed = emailValue.trim();
    if (!trimmed) {
      setEmailError('Email cannot be empty.');
      return;
    }
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
        body: JSON.stringify({ email: trimmed }),
      });
      await auth?.currentUser?.reload();
      setEmailSuccess('Email updated successfully.');
    } catch (e: unknown) {
      setEmailError((e as Error).message ?? 'Something went wrong. Please try again.');
    } finally {
      setEmailSaving(false);
    }
  }

  async function handleCurrencySave() {
    setCurrencyError('');
    setCurrencySuccess('');
    setCurrencySaving(true);
    try {
      await apiFetch('/api/auth/profile', {
        method: 'PUT',
        body: JSON.stringify({ currency: currencyValue }),
      });
      await refreshCurrency();
      setCurrencySuccess('Currency updated.');
    } catch (e: unknown) {
      setCurrencyError((e as Error).message ?? 'Something went wrong. Please try again.');
    } finally {
      setCurrencySaving(false);
    }
  }

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
      const credential = EmailAuthProvider.credential(user.email, currentPw);
      await reauthenticateWithCredential(user, credential);
      await updatePassword(user, newPw);
      setPwSuccess('Password changed successfully.');
      setCurrentPw('');
      setNewPw('');
      setConfirmPw('');
    } catch (e: unknown) {
      setPwError(firebaseErrorMessage((e as { code?: string }).code ?? ''));
    } finally {
      setPwSaving(false);
    }
  }

  const tabs: { key: Section; label: string }[] = [
    { key: 'name', label: 'Display Name' },
    { key: 'email', label: 'Email' },
    { key: 'currency', label: 'Currency' },
    { key: 'password', label: 'Change Password' },
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
          </div>
        </div>
      </div>
    </div>
  );
}

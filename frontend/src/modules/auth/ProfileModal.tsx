import { auth } from '@/firebase';
import {
  type User,
  EmailAuthProvider,
  reauthenticateWithCredential,
  updatePassword,
  updateProfile,
} from 'firebase/auth';
import { useState } from 'react';

interface Props {
  user: User;
  onClose: () => void;
}

type Section = 'name' | 'password';

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

  // Name
  const [nameValue, setNameValue] = useState(user.displayName ?? '');
  const [nameSaving, setNameSaving] = useState(false);
  const [nameSuccess, setNameSuccess] = useState('');
  const [nameError, setNameError] = useState('');

  // Password
  const [currentPw, setCurrentPw] = useState('');
  const [newPw, setNewPw] = useState('');
  const [confirmPw, setConfirmPw] = useState('');
  const [pwSaving, setPwSaving] = useState(false);
  const [pwSuccess, setPwSuccess] = useState('');
  const [pwError, setPwError] = useState('');

  async function handleNameSave() {
    const trimmed = nameValue.trim();
    if (!trimmed) { setNameError('Name cannot be empty.'); return; }
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

  async function handlePasswordSave() {
    setPwError('');
    setPwSuccess('');
    if (!currentPw) { setPwError('Enter your current password.'); return; }
    if (newPw.length < 6) { setPwError('New password must be at least 6 characters.'); return; }
    if (newPw !== confirmPw) { setPwError('Passwords do not match.'); return; }

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

        {/* Section tabs */}
        <div style={{ borderBottom: '1px solid var(--color-border)', padding: '0 1.5rem' }}>
          <ul className="nav nav-tabs" style={{ borderBottom: 'none', marginBottom: '-1px' }}>
            <li className="nav-item">
              <button type="button" className={`nav-link${section === 'name' ? ' active' : ''}`} onClick={() => { setSection('name'); setNameError(''); setNameSuccess(''); }}>
                Display Name
              </button>
            </li>
            <li className="nav-item">
              <button type="button" className={`nav-link${section === 'password' ? ' active' : ''}`} onClick={() => { setSection('password'); setPwError(''); setPwSuccess(''); }}>
                Change Password
              </button>
            </li>
          </ul>
        </div>

        <div className="pf-modal-body">
          {/* ── Name section ── */}
          {section === 'name' && (
            <div>
              <p className="text-muted small mb-3">
                This name is shown in the sidebar and is stored in Firebase Auth only — not on any backend.
              </p>
              {nameError && <div className="alert alert-danger py-2 small mb-3">{nameError}</div>}
              {nameSuccess && <div className="alert alert-success py-2 small mb-3">{nameSuccess}</div>}
              <div className="mb-3">
                <label className="form-label" htmlFor="profile-name">Display name</label>
                <input
                  id="profile-name"
                  className="form-control"
                  type="text"
                  value={nameValue}
                  onChange={(e) => { setNameValue(e.target.value); setNameSuccess(''); setNameError(''); }}
                  placeholder="Your name"
                  disabled={nameSaving}
                  // biome-ignore lint/a11y/noAutofocus: first field in modal
                  autoFocus
                />
              </div>
              <div className="text-muted small mb-3">
                <strong>Email:</strong> {user.email}
              </div>
            </div>
          )}

          {/* ── Password section ── */}
          {section === 'password' && (
            <div>
              <p className="text-muted small mb-3">
                Enter your current password to verify, then choose a new one.
              </p>
              {pwError && <div className="alert alert-danger py-2 small mb-3">{pwError}</div>}
              {pwSuccess && <div className="alert alert-success py-2 small mb-3">{pwSuccess}</div>}
              <div className="mb-3">
                <label className="form-label" htmlFor="pw-current">Current password</label>
                <input
                  id="pw-current"
                  className="form-control"
                  type="password"
                  value={currentPw}
                  onChange={(e) => { setCurrentPw(e.target.value); setPwError(''); setPwSuccess(''); }}
                  disabled={pwSaving}
                  autoComplete="current-password"
                  // biome-ignore lint/a11y/noAutofocus: first field in modal
                  autoFocus
                />
              </div>
              <div className="mb-3">
                <label className="form-label" htmlFor="pw-new">New password</label>
                <input
                  id="pw-new"
                  className="form-control"
                  type="password"
                  value={newPw}
                  onChange={(e) => { setNewPw(e.target.value); setPwError(''); setPwSuccess(''); }}
                  placeholder="Min. 6 characters"
                  disabled={pwSaving}
                  autoComplete="new-password"
                />
              </div>
              <div className="mb-0">
                <label className="form-label" htmlFor="pw-confirm">Confirm new password</label>
                <input
                  id="pw-confirm"
                  className="form-control"
                  type="password"
                  value={confirmPw}
                  onChange={(e) => { setConfirmPw(e.target.value); setPwError(''); setPwSuccess(''); }}
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

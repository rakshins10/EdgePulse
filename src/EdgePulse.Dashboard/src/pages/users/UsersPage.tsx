import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getUsers, createUser, updateUserRole, setUserEnabled, resetUserPassword,
} from '../../api/users';
import { getMills } from '../../api/organisation';
import type { IdentityUserDto } from '../../types/api';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useConfirm } from '../../context/ConfirmContext';
import { useToast } from '../../context/ToastContext';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import styles from './UsersPage.module.css';
import f from '../../components/common/FormField.module.css';

const ROLES = ['CustomerAdmin', 'MillManager', 'Operator', 'Executive', 'SuperAdmin'];

export default function UsersPage() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const confirm = useConfirm();
  const toast = useToast();
  const me = useCurrentUser();

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['users'], queryFn: getUsers,
  });
  const { data: mills = [] } = useQuery({ queryKey: ['mills'], queryFn: getMills });

  const assignableRoles = ROLES.filter(
    r => r !== 'SuperAdmin' || me?.role === 'SuperAdmin');

  // ── Create modal state ────────────────────────────────────────────────────
  const [createOpen, setCreateOpen] = useState(false);
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [role, setRole] = useState('Operator');
  const [millId, setMillId] = useState('');
  const [password, setPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // ── Edit-role modal state ─────────────────────────────────────────────────
  const [editUser, setEditUser] = useState<IdentityUserDto | null>(null);
  const [editRole, setEditRole] = useState('Operator');
  const [editMillId, setEditMillId] = useState('');

  // ── Reset-password modal state ────────────────────────────────────────────
  const [pwUser, setPwUser] = useState<IdentityUserDto | null>(null);
  const [newPw, setNewPw] = useState('');

  function openCreate() {
    setEmail(''); setFirstName(''); setLastName('');
    setRole('Operator'); setMillId(''); setPassword('');
    setError(null); setCreateOpen(true);
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      await createUser({
        email, firstName, lastName, role,
        millId: role === 'MillManager' && millId ? millId : null,
        temporaryPassword: password,
      });
      await qc.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.created', { email }));
      setCreateOpen(false);
    } catch {
      setError(t('users.createError'));
    } finally { setSaving(false); }
  }

  function openEdit(u: IdentityUserDto) {
    setEditUser(u);
    setEditRole(u.role ?? 'Operator');
    setEditMillId(u.millId ?? '');
    setError(null);
  }

  async function handleEditRole(e: FormEvent) {
    e.preventDefault();
    if (!editUser) return;
    setSaving(true); setError(null);
    try {
      await updateUserRole(editUser.id, {
        role: editRole,
        millId: editRole === 'MillManager' && editMillId ? editMillId : null,
      });
      await qc.invalidateQueries({ queryKey: ['users'] });
      toast.success(t('users.roleUpdated', { name: editUser.username }));
      setEditUser(null);
    } catch {
      setError(t('users.updateError'));
    } finally { setSaving(false); }
  }

  async function handleToggleEnabled(u: IdentityUserDto) {
    const enabling = !u.enabled;
    if (!enabling) {
      const ok = await confirm({
        message: t('users.disableConfirm', { name: u.username }),
        variant: 'warning',
        confirmLabel: t('users.disable'),
      });
      if (!ok) return;
    }
    try {
      await setUserEnabled(u.id, enabling);
      await qc.invalidateQueries({ queryKey: ['users'] });
      toast.success(enabling
        ? t('users.enabled', { name: u.username })
        : t('users.disabled', { name: u.username }));
    } catch {
      toast.error(t('users.updateError'));
    }
  }

  async function handleResetPw(e: FormEvent) {
    e.preventDefault();
    if (!pwUser) return;
    setSaving(true); setError(null);
    try {
      await resetUserPassword(pwUser.id, newPw);
      toast.success(t('users.passwordReset', { name: pwUser.username }));
      setPwUser(null); setNewPw('');
    } catch {
      setError(t('users.updateError'));
    } finally { setSaving(false); }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  const roleChipClass = (r: string | null) =>
    r === 'SuperAdmin' ? `${styles.roleChip} ${styles.roleSuperAdmin}`
    : r === 'CustomerAdmin' ? `${styles.roleChip} ${styles.roleCustomerAdmin}`
    : styles.roleChip;

  return (
    <div className={styles.page}>
      <div className={styles.toolbar}>
        <span className={styles.count}>{t('users.count', { count: users.length })}</span>
        <button className={styles.addBtn} onClick={openCreate}>{t('users.addBtn')}</button>
      </div>

      <div className={styles.card}>
        {users.length === 0 ? (
          <div className={styles.empty}>{t('users.empty')}</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>{t('users.user')}</th>
                <th>{t('users.role')}</th>
                <th>{t('users.status')}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td>
                    <span className={styles.userName}>
                      {u.firstName || u.lastName
                        ? `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim()
                        : u.username}
                    </span>
                    <span className={styles.userEmail}>{u.email ?? u.username}</span>
                  </td>
                  <td><span className={roleChipClass(u.role)}>{u.role ?? '—'}</span></td>
                  <td>
                    <span className={u.enabled ? styles.statusOn : styles.statusOff}>
                      {u.enabled ? t('users.active') : t('users.inactive')}
                    </span>
                  </td>
                  <td>
                    <div className={styles.actions}>
                      <button className={styles.actionBtn} onClick={() => openEdit(u)}>
                        {t('users.changeRole')}
                      </button>
                      <button className={styles.actionBtn}
                        onClick={() => { setPwUser(u); setNewPw(''); setError(null); }}>
                        {t('users.resetPassword')}
                      </button>
                      {u.id !== me?.userId && (
                        <button
                          className={`${styles.actionBtn} ${u.enabled ? styles.dangerBtn : ''}`}
                          onClick={() => handleToggleEnabled(u)}
                        >
                          {u.enabled ? t('users.disable') : t('users.enable')}
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Create user */}
      <Modal open={createOpen} title={t('users.addTitle')} onClose={() => setCreateOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setCreateOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="user-create" disabled={saving}>
              {saving ? t('common.creating') : t('common.create')}
            </button>
          </>
        }>
        <form id="user-create" className={f.formGrid} onSubmit={handleCreate}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('users.email')}</label>
            <input className={f.input} type="email" required value={email}
              onChange={e => setEmail(e.target.value)} />
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('users.firstName')}</label>
              <input className={f.input} required value={firstName}
                onChange={e => setFirstName(e.target.value)} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('users.lastName')}</label>
              <input className={f.input} required value={lastName}
                onChange={e => setLastName(e.target.value)} />
            </div>
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('users.role')}</label>
              <select className={f.select} value={role} onChange={e => setRole(e.target.value)}>
                {assignableRoles.map(r => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
            {role === 'MillManager' && (
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('users.mill')}</label>
                <select className={f.select} required value={millId}
                  onChange={e => setMillId(e.target.value)}>
                  <option value="">{t('users.selectMill')}</option>
                  {mills.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
                </select>
              </div>
            )}
          </div>
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('users.tempPassword')}</label>
            <input className={f.input} required minLength={8} value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder={t('users.tempPasswordHint')} />
          </div>
        </form>
      </Modal>

      {/* Change role */}
      <Modal open={!!editUser} title={t('users.editTitle', { name: editUser?.username ?? '' })}
        onClose={() => setEditUser(null)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setEditUser(null)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="user-role" disabled={saving}>
              {saving ? t('common.saving') : t('common.save')}
            </button>
          </>
        }>
        <form id="user-role" className={f.formGrid} onSubmit={handleEditRole}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('users.role')}</label>
            <select className={f.select} value={editRole} onChange={e => setEditRole(e.target.value)}>
              {assignableRoles.map(r => <option key={r} value={r}>{r}</option>)}
            </select>
          </div>
          {editRole === 'MillManager' && (
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('users.mill')}</label>
              <select className={f.select} required value={editMillId}
                onChange={e => setEditMillId(e.target.value)}>
                <option value="">{t('users.selectMill')}</option>
                {mills.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
              </select>
            </div>
          )}
        </form>
      </Modal>

      {/* Reset password */}
      <Modal open={!!pwUser} title={t('users.resetTitle', { name: pwUser?.username ?? '' })}
        onClose={() => setPwUser(null)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setPwUser(null)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="user-pw" disabled={saving}>
              {saving ? t('common.saving') : t('users.resetPassword')}
            </button>
          </>
        }>
        <form id="user-pw" className={f.formGrid} onSubmit={handleResetPw}>
          {error && <p className={f.errorText}>{error}</p>}
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('users.tempPassword')}</label>
            <input className={f.input} required minLength={8} value={newPw}
              onChange={e => setNewPw(e.target.value)}
              placeholder={t('users.tempPasswordHint')} />
          </div>
        </form>
      </Modal>
    </div>
  );
}

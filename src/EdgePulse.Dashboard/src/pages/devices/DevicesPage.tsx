import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { getDevices, registerDevice, decommissionDevice, updateDevice } from '../../api/devices';
import { getMills, getAreas } from '../../api/organisation';
import { getDeviceTypes, getDeviceStatuses } from '../../api/configuration';
import type { DeviceListDto } from '../../types/api';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import Badge from '../../components/common/Badge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import styles from './DevicesPage.module.css';
import f from '../../components/common/FormField.module.css';

function canRegisterDevices(role?: string) {
  return role === 'SuperAdmin' || role === 'CustomerAdmin' || role === 'MillManager';
}

function canDecommission(role?: string) {
  return role === 'SuperAdmin' || role === 'CustomerAdmin' || role === 'MillManager';
}

export default function DevicesPage() {
  const navigate = useNavigate();
  const user = useCurrentUser();
  const qc = useQueryClient();

  const [millFilter, setMillFilter] = useState('');
  const [areaFilter, setAreaFilter] = useState('');

  const { data: mills = [] }    = useQuery({ queryKey: ['mills'], queryFn: getMills });
  const { data: areas = [] }    = useQuery({
    queryKey: ['areas', millFilter],
    queryFn:  () => getAreas(millFilter || undefined),
  });
  const { data: types = [] }    = useQuery({ queryKey: ['device-types'],    queryFn: getDeviceTypes });
  const { data: statuses = [] } = useQuery({ queryKey: ['device-statuses'], queryFn: getDeviceStatuses });

  const { data: devices = [], isLoading } = useQuery({
    queryKey: ['devices', millFilter, areaFilter],
    queryFn:  () => getDevices({
      millId: millFilter || undefined,
      areaId: areaFilter || undefined,
    }),
  });

  // ── Register Device modal ─────────────────────────────────────────────────
  const [regOpen,    setRegOpen]    = useState(false);
  const [regSaving,  setRegSaving]  = useState(false);
  const [regError,   setRegError]   = useState<string | null>(null);
  const [regSuccess, setRegSuccess] = useState<{ deviceId: string; apiKey: string } | null>(null);

  const [regMill,   setRegMill]   = useState('');
  const [regArea,   setRegArea]   = useState('');
  const [regType,   setRegType]   = useState('');
  const [regStatus, setRegStatus] = useState('');
  const [regName,   setRegName]   = useState('');
  const [regCode,   setRegCode]   = useState('');
  const [regSerial, setRegSerial] = useState('');
  const [regDate,   setRegDate]   = useState('');
  const [regDesc,   setRegDesc]   = useState('');

  const formAreas = regMill ? areas.filter(a => a.millId === regMill) : areas;

  function openRegister() {
    setRegMill(''); setRegArea(''); setRegType(''); setRegStatus('');
    setRegName(''); setRegCode(''); setRegSerial(''); setRegDate(''); setRegDesc('');
    setRegError(null); setRegSuccess(null); setRegOpen(true);
  }

  async function handleRegister(e: FormEvent) {
    e.preventDefault();
    setRegSaving(true); setRegError(null);
    try {
      const result = await registerDevice({
        areaId: regArea, typeId: regType, statusId: regStatus,
        name: regName, code: regCode,
        serialNumber: regSerial || undefined,
        installDate: regDate || undefined,
        description: regDesc || undefined,
      });
      setRegSuccess(result);
      await qc.invalidateQueries({ queryKey: ['devices'] });
    } catch {
      setRegError('Failed to register device. Please check the fields and try again.');
    } finally {
      setRegSaving(false);
    }
  }

  // ── Decommission ──────────────────────────────────────────────────────────
  async function handleDecommission(device: DeviceListDto) {
    const ok = confirm(`Decommission "${device.name}"?\n\nThis will revoke its API keys. Telemetry data is preserved.`);
    if (!ok) return;
    try {
      await decommissionDevice(device.id);
      await qc.invalidateQueries({ queryKey: ['devices'] });
    } catch {
      alert('Failed to decommission device.');
    }
  }

  // ── Edit Device modal ────────────────────────────────────────────────────
  const [editOpen,    setEditOpen]    = useState(false);
  const [editing,     setEditing]     = useState<DeviceListDto | null>(null);
  const [editSaving,  setEditSaving]  = useState(false);
  const [editError,   setEditError]   = useState<string | null>(null);
  const [editName,    setEditName]    = useState('');
  const [editArea,    setEditArea]    = useState('');
  const [editType,    setEditType]    = useState('');
  const [editSerial,  setEditSerial]  = useState('');
  const [editDate,    setEditDate]    = useState('');
  const [editDesc,    setEditDesc]    = useState('');

  // For edit: only areas in the device's mill can be selected
  const editFormAreas = editing ? areas.filter(a => a.millId === editing.millId) : [];

  function openEdit(device: DeviceListDto) {
    setEditing(device);
    setEditName(device.name);
    setEditArea(device.areaId);
    const t = types.find(x => x.name === device.typeName);
    setEditType(t?.id ?? '');
    setEditSerial(device.serialNumber ?? '');
    setEditDate('');
    setEditDesc('');
    setEditError(null);
    setEditOpen(true);
  }

  async function handleEditSubmit(e: FormEvent) {
    e.preventDefault();
    if (!editing) return;
    setEditSaving(true); setEditError(null);
    try {
      await updateDevice(editing.id, {
        name: editName,
        areaId: editArea,
        typeId: editType,
        serialNumber: editSerial || undefined,
        installDate: editDate || undefined,
        description: editDesc || undefined,
      });
      await qc.invalidateQueries({ queryKey: ['devices'] });
      setEditOpen(false);
    } catch {
      setEditError('Failed to update device.');
    } finally {
      setEditSaving(false);
    }
  }

  if (isLoading) return <LoadingSpinner message="Loading devices…" />;

  const showActionsCol = canDecommission(user?.role);

  return (
    <>
      <div className={styles.toolbar}>
        <div className={styles.filters}>
          <span className={styles.filterLabel}>Filter:</span>
          <select
            className={styles.select}
            value={millFilter}
            onChange={e => { setMillFilter(e.target.value); setAreaFilter(''); }}
          >
            <option value="">All mills</option>
            {mills.map(m => (
              <option key={m.id} value={m.id}>
                {m.name}{m.location ? ` — ${m.location}` : ''}
              </option>
            ))}
          </select>
          <select
            className={styles.select}
            value={areaFilter}
            onChange={e => setAreaFilter(e.target.value)}
            disabled={!millFilter}
          >
            <option value="">All areas</option>
            {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </div>

        <div className={styles.toolbarRight}>
          <div className={styles.count}>
            {devices.length} {devices.length === 1 ? 'device' : 'devices'}
          </div>
          {canRegisterDevices(user?.role) && (
            <button className={styles.registerBtn} onClick={openRegister}>+ Register Device</button>
          )}
        </div>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Device</th>
              <th>Type</th>
              <th>Status</th>
              <th>Mill</th>
              <th>Area</th>
              {showActionsCol && <th className={styles.actionsCol} />}
            </tr>
          </thead>
          <tbody>
            {devices.length === 0 ? (
              <tr>
                <td colSpan={showActionsCol ? 6 : 5} className={styles.empty}>
                  No devices found.{canRegisterDevices(user?.role) ? ' Click "+ Register Device" to add one.' : ''}
                </td>
              </tr>
            ) : (
              devices.map(device => {
                const mill = mills.find(m => m.id === device.millId);
                return (
                  <tr
                    key={device.id}
                    className={styles.clickableRow}
                    onClick={() => navigate(`/devices/${device.id}`)}
                    title="Click to view telemetry"
                  >
                    <td>
                      <div className={styles.deviceName}>{device.name}</div>
                      <div className={styles.deviceCode}>{device.code}</div>
                    </td>
                    <td>{device.typeName}</td>
                    <td>
                      <Badge label={device.statusName} color={device.statusColor} variant="status" />
                    </td>
                    <td className={styles.location}>
                      <div>{device.millName}</div>
                      {mill?.location && (
                        <div className={styles.locationSub}>{mill.location}</div>
                      )}
                    </td>
                    <td className={styles.location}>{device.areaName}</td>
                    {showActionsCol && (
                      <td className={styles.actionsCol} onClick={e => e.stopPropagation()}>
                        <button
                          className={styles.editBtn}
                          onClick={() => openEdit(device)}
                          title="Edit device"
                        >
                          Edit
                        </button>
                        <button
                          className={styles.decommBtn}
                          onClick={() => handleDecommission(device)}
                          title="Decommission"
                        >
                          Decommission
                        </button>
                      </td>
                    )}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Register Device modal */}
      <Modal
        open={regOpen}
        title="Register Device"
        onClose={() => setRegOpen(false)}
        width={560}
        footer={
          regSuccess ? (
            <button className={f.btnPrimary} onClick={() => setRegOpen(false)}>Close</button>
          ) : (
            <>
              <button className={f.btnSecondary} onClick={() => setRegOpen(false)}>Cancel</button>
              <button className={f.btnPrimary} form="reg-form" disabled={regSaving}>
                {regSaving ? 'Registering…' : 'Register Device'}
              </button>
            </>
          )
        }
      >
        {regSuccess ? (
          <div className={styles.successBox}>
            <p className={styles.successTitle}>✓ Device registered</p>
            <p className={styles.successNote}>
              Store the API key below — it is shown <strong>only once</strong> and cannot be recovered.
            </p>
            <div className={styles.apiKeyBox}>
              <span className={styles.apiKeyLabel}>API Key</span>
              <code className={styles.apiKey}>{regSuccess.apiKey}</code>
            </div>
          </div>
        ) : (
          <form id="reg-form" className={f.formGrid} onSubmit={handleRegister}>
            {regError && <p className={f.errorText}>{regError}</p>}

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>Device Name</label>
                <input className={f.input} required value={regName}
                  onChange={e => setRegName(e.target.value)} placeholder="e.g. Feed Pump 1" />
              </div>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>Code</label>
                <input className={f.input} required value={regCode}
                  onChange={e => setRegCode(e.target.value.toUpperCase())} placeholder="e.g. FP-001" />
              </div>
            </div>

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>Device Type</label>
                <select className={f.select} required value={regType}
                  onChange={e => setRegType(e.target.value)}>
                  <option value="">Select type…</option>
                  {types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </div>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>Status</label>
                <select className={f.select} required value={regStatus}
                  onChange={e => setRegStatus(e.target.value)}>
                  <option value="">Select status…</option>
                  {statuses.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
            </div>

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>Mill</label>
                <select className={f.select} required value={regMill}
                  onChange={e => { setRegMill(e.target.value); setRegArea(''); }}>
                  <option value="">Select mill…</option>
                  {mills.map(m => (
                    <option key={m.id} value={m.id}>
                      {m.name}{m.location ? ` — ${m.location}` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>Area</label>
                <select className={f.select} required value={regArea}
                  onChange={e => setRegArea(e.target.value)} disabled={!regMill}>
                  <option value="">Select area…</option>
                  {formAreas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                </select>
              </div>
            </div>

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={f.label}>Serial Number</label>
                <input className={f.input} value={regSerial}
                  onChange={e => setRegSerial(e.target.value)} placeholder="Optional" />
              </div>
              <div className={f.field}>
                <label className={f.label}>Install Date</label>
                <input className={f.input} type="date" value={regDate}
                  onChange={e => setRegDate(e.target.value)} />
              </div>
            </div>

            <div className={f.field}>
              <label className={f.label}>Description</label>
              <textarea className={f.textarea} value={regDesc}
                onChange={e => setRegDesc(e.target.value)} placeholder="Optional notes" />
            </div>
          </form>
        )}
      </Modal>

      {/* Edit Device modal */}
      <Modal
        open={editOpen}
        title={`Edit Device — ${editing?.code ?? ''}`}
        onClose={() => setEditOpen(false)}
        width={560}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setEditOpen(false)}>Cancel</button>
            <button className={f.btnPrimary} form="edit-form" disabled={editSaving}>
              {editSaving ? 'Saving…' : 'Save Changes'}
            </button>
          </>
        }
      >
        <form id="edit-form" className={f.formGrid} onSubmit={handleEditSubmit}>
          {editError && <p className={f.errorText}>{editError}</p>}
          <p className={f.hint}>
            Mill is fixed for an existing device. To move across mills,
            decommission and re-register.
          </p>

          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Device Name</label>
              <input className={f.input} required value={editName}
                onChange={e => setEditName(e.target.value)} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>Device Type</label>
              <select className={f.select} required value={editType}
                onChange={e => setEditType(e.target.value)}>
                <option value="">Select type…</option>
                {types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </div>
          </div>

          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>Area (within {editing?.millName})</label>
            <select className={f.select} required value={editArea}
              onChange={e => setEditArea(e.target.value)}>
              <option value="">Select area…</option>
              {editFormAreas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>

          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>Serial Number</label>
              <input className={f.input} value={editSerial}
                onChange={e => setEditSerial(e.target.value)} placeholder="Optional" />
            </div>
            <div className={f.field}>
              <label className={f.label}>Install Date</label>
              <input className={f.input} type="date" value={editDate}
                onChange={e => setEditDate(e.target.value)} />
            </div>
          </div>

          <div className={f.field}>
            <label className={f.label}>Description</label>
            <textarea className={f.textarea} value={editDesc}
              onChange={e => setEditDesc(e.target.value)} placeholder="Optional notes" />
          </div>
        </form>
      </Modal>
    </>
  );
}

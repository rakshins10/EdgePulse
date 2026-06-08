import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
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
  const { t } = useTranslation();
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
      setRegError(t('devices.registerModal.error'));
    } finally {
      setRegSaving(false);
    }
  }

  // ── Decommission ──────────────────────────────────────────────────────────
  async function handleDecommission(device: DeviceListDto) {
    const ok = confirm(t('devices.decommissionConfirm', { name: device.name }));
    if (!ok) return;
    try {
      await decommissionDevice(device.id);
      await qc.invalidateQueries({ queryKey: ['devices'] });
    } catch {
      alert(t('devices.decommissionError'));
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

  const editFormAreas = editing ? areas.filter(a => a.millId === editing.millId) : [];

  function openEdit(device: DeviceListDto) {
    setEditing(device);
    setEditName(device.name);
    setEditArea(device.areaId);
    const matchedType = types.find(x => x.name === device.typeName);
    setEditType(matchedType?.id ?? '');
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
      setEditError(t('devices.editModal.error'));
    } finally {
      setEditSaving(false);
    }
  }

  if (isLoading) return <LoadingSpinner message={t('devices.loading')} />;

  const showActionsCol = canDecommission(user?.role);

  return (
    <>
      <div className={styles.toolbar}>
        <div className={styles.filters}>
          <span className={styles.filterLabel}>{t('common.filter')}</span>
          <select
            className={styles.select}
            value={millFilter}
            onChange={e => { setMillFilter(e.target.value); setAreaFilter(''); }}
          >
            <option value="">{t('devices.allMills')}</option>
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
            <option value="">{t('devices.allAreas')}</option>
            {areas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
          </select>
        </div>

        <div className={styles.toolbarRight}>
          <div className={styles.count}>
            {t('devices.count', { count: devices.length })}
          </div>
          {canRegisterDevices(user?.role) && (
            <button className={styles.registerBtn} onClick={openRegister}>{t('devices.registerBtn')}</button>
          )}
        </div>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>{t('devices.table.device')}</th>
              <th>{t('devices.table.type')}</th>
              <th>{t('devices.table.status')}</th>
              <th>{t('devices.table.mill')}</th>
              <th>{t('devices.table.area')}</th>
              {showActionsCol && <th className={styles.actionsCol} />}
            </tr>
          </thead>
          <tbody>
            {devices.length === 0 ? (
              <tr>
                <td colSpan={showActionsCol ? 6 : 5} className={styles.empty}>
                  {t('devices.empty')}{canRegisterDevices(user?.role) ? t('devices.emptyHint') : ''}
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
                    title={t('devices.rowTooltip')}
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
                          title={t('common.edit')}
                        >
                          {t('common.edit')}
                        </button>
                        <button
                          className={styles.decommBtn}
                          onClick={() => handleDecommission(device)}
                          title={t('devices.decommission')}
                        >
                          {t('devices.decommission')}
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
        title={t('devices.registerModal.title')}
        onClose={() => setRegOpen(false)}
        width={560}
        footer={
          regSuccess ? (
            <button className={f.btnPrimary} onClick={() => setRegOpen(false)}>{t('common.close')}</button>
          ) : (
            <>
              <button className={f.btnSecondary} onClick={() => setRegOpen(false)}>{t('common.cancel')}</button>
              <button className={f.btnPrimary} form="reg-form" disabled={regSaving}>
                {regSaving ? t('devices.registerModal.registering') : t('devices.registerModal.registerBtn')}
              </button>
            </>
          )
        }
      >
        {regSuccess ? (
          <div className={styles.successBox}>
            <p className={styles.successTitle}>{t('devices.registerModal.successTitle')}</p>
            <p
              className={styles.successNote}
              dangerouslySetInnerHTML={{ __html: t('devices.registerModal.successNote') }}
            />
            <div className={styles.apiKeyBox}>
              <span className={styles.apiKeyLabel}>{t('devices.registerModal.apiKeyLabel')}</span>
              <code className={styles.apiKey}>{regSuccess.apiKey}</code>
            </div>
          </div>
        ) : (
          <form id="reg-form" className={f.formGrid} onSubmit={handleRegister}>
            {regError && <p className={f.errorText}>{regError}</p>}

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.name')}</label>
                <input className={f.input} required value={regName}
                  onChange={e => setRegName(e.target.value)} placeholder={t('devices.registerModal.namePlaceholder')} />
              </div>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.code')}</label>
                <input className={f.input} required value={regCode}
                  onChange={e => setRegCode(e.target.value.toUpperCase())} placeholder={t('devices.registerModal.codePlaceholder')} />
              </div>
            </div>

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.deviceType')}</label>
                <select className={f.select} required value={regType}
                  onChange={e => setRegType(e.target.value)}>
                  <option value="">{t('devices.registerModal.deviceTypePlaceholder')}</option>
                  {types.map(typ => <option key={typ.id} value={typ.id}>{typ.name}</option>)}
                </select>
              </div>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.status')}</label>
                <select className={f.select} required value={regStatus}
                  onChange={e => setRegStatus(e.target.value)}>
                  <option value="">{t('devices.registerModal.statusPlaceholder')}</option>
                  {statuses.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </div>
            </div>

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.mill')}</label>
                <select className={f.select} required value={regMill}
                  onChange={e => { setRegMill(e.target.value); setRegArea(''); }}>
                  <option value="">{t('devices.registerModal.millPlaceholder')}</option>
                  {mills.map(m => (
                    <option key={m.id} value={m.id}>
                      {m.name}{m.location ? ` — ${m.location}` : ''}
                    </option>
                  ))}
                </select>
              </div>
              <div className={f.field}>
                <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.area')}</label>
                <select className={f.select} required value={regArea}
                  onChange={e => setRegArea(e.target.value)} disabled={!regMill}>
                  <option value="">{t('devices.registerModal.areaPlaceholder')}</option>
                  {formAreas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                </select>
              </div>
            </div>

            <div className={f.formRow}>
              <div className={f.field}>
                <label className={f.label}>{t('devices.registerModal.serialNumber')}</label>
                <input className={f.input} value={regSerial}
                  onChange={e => setRegSerial(e.target.value)} placeholder={t('common.optional')} />
              </div>
              <div className={f.field}>
                <label className={f.label}>{t('devices.registerModal.installDate')}</label>
                <input className={f.input} type="date" value={regDate}
                  onChange={e => setRegDate(e.target.value)} />
              </div>
            </div>

            <div className={f.field}>
              <label className={f.label}>{t('devices.registerModal.description')}</label>
              <textarea className={f.textarea} value={regDesc}
                onChange={e => setRegDesc(e.target.value)} placeholder={t('devices.registerModal.descriptionPlaceholder')} />
            </div>
          </form>
        )}
      </Modal>

      {/* Edit Device modal */}
      <Modal
        open={editOpen}
        title={t('devices.editModal.title', { code: editing?.code ?? '' })}
        onClose={() => setEditOpen(false)}
        width={560}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setEditOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="edit-form" disabled={editSaving}>
              {editSaving ? t('common.saving') : t('common.save')}
            </button>
          </>
        }
      >
        <form id="edit-form" className={f.formGrid} onSubmit={handleEditSubmit}>
          {editError && <p className={f.errorText}>{editError}</p>}
          <p className={f.hint}>{t('devices.editModal.millFixedHint')}</p>

          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.name')}</label>
              <input className={f.input} required value={editName}
                onChange={e => setEditName(e.target.value)} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('devices.registerModal.deviceType')}</label>
              <select className={f.select} required value={editType}
                onChange={e => setEditType(e.target.value)}>
                <option value="">{t('devices.registerModal.deviceTypePlaceholder')}</option>
                {types.map(typ => <option key={typ.id} value={typ.id}>{typ.name}</option>)}
              </select>
            </div>
          </div>

          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>
              {t('devices.editModal.areaWithin', { mill: editing?.millName ?? '' })}
            </label>
            <select className={f.select} required value={editArea}
              onChange={e => setEditArea(e.target.value)}>
              <option value="">{t('devices.registerModal.areaPlaceholder')}</option>
              {editFormAreas.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
            </select>
          </div>

          <div className={f.formRow}>
            <div className={f.field}>
              <label className={f.label}>{t('devices.registerModal.serialNumber')}</label>
              <input className={f.input} value={editSerial}
                onChange={e => setEditSerial(e.target.value)} placeholder={t('common.optional')} />
            </div>
            <div className={f.field}>
              <label className={f.label}>{t('devices.registerModal.installDate')}</label>
              <input className={f.input} type="date" value={editDate}
                onChange={e => setEditDate(e.target.value)} />
            </div>
          </div>

          <div className={f.field}>
            <label className={f.label}>{t('devices.registerModal.description')}</label>
            <textarea className={f.textarea} value={editDesc}
              onChange={e => setEditDesc(e.target.value)} placeholder={t('devices.registerModal.descriptionPlaceholder')} />
          </div>
        </form>
      </Modal>
    </>
  );
}

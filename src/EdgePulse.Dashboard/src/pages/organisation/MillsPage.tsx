import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  getMills, getAreas,
  createMill, updateMill, deleteMill,
  createArea, updateArea, deleteArea,
} from '../../api/organisation';
import { getLocationTypes } from '../../api/configuration';
import type { MillDto, AreaDto, DeploymentMode } from '../../types/api';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import Badge from '../../components/common/Badge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import styles from './MillsPage.module.css';
import f from '../../components/common/FormField.module.css';

const TIMEZONES = [
  'UTC',
  'Europe/London', 'Europe/Helsinki', 'Europe/Berlin', 'Europe/Paris',
  'Europe/Stockholm', 'Europe/Oslo',
  'Asia/Kolkata', 'Asia/Tokyo', 'Asia/Shanghai', 'Asia/Singapore',
  'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles',
  'Australia/Sydney',
];

function canManageMills(role?: string) {
  return role === 'SuperAdmin' || role === 'CustomerAdmin';
}

function canManageAreas(role?: string) {
  return role === 'SuperAdmin' || role === 'CustomerAdmin' || role === 'MillManager';
}

export default function MillsPage() {
  const { t } = useTranslation();
  const user = useCurrentUser();
  const qc = useQueryClient();

  const { data: mills = [], isLoading: millsLoading } = useQuery({ queryKey: ['mills'], queryFn: getMills });
  const { data: areas = [], isLoading: areasLoading } = useQuery({ queryKey: ['areas'], queryFn: () => getAreas() });
  const { data: locationTypes = [] } = useQuery({ queryKey: ['location-types'], queryFn: getLocationTypes });

  // ── Mill modal (Add + Edit) ──────────────────────────────────────────────
  const [millOpen,      setMillOpen]      = useState(false);
  const [millEditing,   setMillEditing]   = useState<MillDto | null>(null);
  const [millSaving,    setMillSaving]    = useState(false);
  const [millError,     setMillError]     = useState<string | null>(null);
  const [millName,      setMillName]      = useState('');
  const [millCode,      setMillCode]      = useState('');
  const [millLocation,  setMillLocation]  = useState('');
  const [millTimezone,  setMillTimezone]  = useState('UTC');
  const [millInternet,  setMillInternet]  = useState(true);
  const [millMode,      setMillMode]      = useState<DeploymentMode>('Cloud');

  function openAddMill() {
    setMillEditing(null);
    setMillName(''); setMillCode(''); setMillLocation('');
    setMillTimezone('UTC'); setMillInternet(true); setMillMode('Cloud');
    setMillError(null); setMillOpen(true);
  }

  function openEditMill(mill: MillDto) {
    setMillEditing(mill);
    setMillName(mill.name); setMillCode(mill.code); setMillLocation(mill.location);
    setMillTimezone(mill.timezone); setMillInternet(mill.hasInternet); setMillMode(mill.deploymentMode);
    setMillError(null); setMillOpen(true);
  }

  async function handleMillSubmit(e: FormEvent) {
    e.preventDefault(); setMillSaving(true); setMillError(null);
    try {
      if (millEditing) {
        await updateMill(millEditing.id, {
          name: millName, location: millLocation,
          timezone: millTimezone, hasInternet: millInternet, deploymentMode: millMode,
        });
      } else {
        await createMill({
          name: millName, code: millCode, location: millLocation,
          timezone: millTimezone, hasInternet: millInternet, deploymentMode: millMode,
        });
      }
      await qc.invalidateQueries({ queryKey: ['mills'] });
      setMillOpen(false);
    } catch {
      setMillError(millEditing ? t('mills.modal.errorUpdate') : t('mills.modal.errorCreate'));
    } finally { setMillSaving(false); }
  }

  async function handleDeleteMill(mill: MillDto) {
    if (!confirm(t('mills.deleteConfirm', { name: mill.name }))) return;
    try {
      await deleteMill(mill.id);
      await qc.invalidateQueries({ queryKey: ['mills'] });
    } catch (err) {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title
        ?? t('mills.deleteFallback');
      alert(msg);
    }
  }

  // ── Area modal (Add + Edit) ──────────────────────────────────────────────
  const [areaOpen,      setAreaOpen]      = useState(false);
  const [areaEditing,   setAreaEditing]   = useState<AreaDto | null>(null);
  const [areaTargetMill,setAreaTargetMill] = useState<MillDto | null>(null);
  const [areaSaving,    setAreaSaving]    = useState(false);
  const [areaError,     setAreaError]     = useState<string | null>(null);
  const [areaName,      setAreaName]      = useState('');
  const [areaCode,      setAreaCode]      = useState('');
  const [areaDesc,      setAreaDesc]      = useState('');
  const [areaLocType,   setAreaLocType]   = useState('');

  function openAddArea(mill: MillDto) {
    setAreaEditing(null);
    setAreaName(''); setAreaCode(''); setAreaDesc(''); setAreaLocType('');
    setAreaError(null); setAreaTargetMill(mill); setAreaOpen(true);
  }

  function openEditArea(area: AreaDto) {
    const mill = mills.find(m => m.id === area.millId) ?? null;
    setAreaEditing(area); setAreaTargetMill(mill);
    setAreaName(area.name); setAreaCode(area.code);
    setAreaDesc(area.description ?? '');
    const lt = locationTypes.find(lt => lt.name === area.locationTypeName);
    setAreaLocType(lt?.id ?? '');
    setAreaError(null); setAreaOpen(true);
  }

  async function handleAreaSubmit(e: FormEvent) {
    e.preventDefault();
    setAreaSaving(true); setAreaError(null);
    try {
      if (areaEditing) {
        await updateArea(areaEditing.id, {
          name: areaName,
          description: areaDesc || undefined,
          locationTypeId: areaLocType || undefined,
        });
      } else if (areaTargetMill) {
        await createArea({
          millId: areaTargetMill.id,
          name: areaName, code: areaCode,
          description: areaDesc || undefined,
          locationTypeId: areaLocType || undefined,
        });
      }
      await qc.invalidateQueries({ queryKey: ['areas'] });
      setAreaOpen(false);
    } catch {
      setAreaError(areaEditing ? t('mills.areaModal.errorUpdate') : t('mills.areaModal.errorCreate'));
    } finally { setAreaSaving(false); }
  }

  async function handleDeleteArea(area: AreaDto) {
    if (!confirm(t('mills.deleteAreaConfirm', { name: area.name }))) return;
    try {
      await deleteArea(area.id);
      await qc.invalidateQueries({ queryKey: ['areas'] });
    } catch (err) {
      const msg = (err as { response?: { data?: { title?: string } } })?.response?.data?.title
        ?? t('mills.deleteAreaFallback');
      alert(msg);
    }
  }

  if (millsLoading || areasLoading) return <LoadingSpinner message={t('mills.loading')} />;

  return (
    <>
      <div className={styles.topBar}>
        <p className={styles.summary}>
          {t('mills.summary', { count: mills.length })} · {t('mills.areasSummary', { count: areas.length })}
        </p>
        {canManageMills(user?.role) && (
          <button className={styles.addBtn} onClick={openAddMill}>{t('mills.addMill')}</button>
        )}
      </div>

      {mills.length === 0 ? (
        <div className={styles.empty}>
          <p>{t('mills.emptyMessage')}</p>
          {canManageMills(user?.role) && (
            <button className={styles.addBtn} onClick={openAddMill}>{t('mills.addFirstMill')}</button>
          )}
        </div>
      ) : (
        <div className={styles.grid}>
          {mills.map(mill => {
            const millAreas = areas.filter(a => a.millId === mill.id);
            return (
              <div key={mill.id} className={styles.card}>
                <div className={styles.cardHeader}>
                  <div className={styles.headerLeft}>
                    <div className={styles.millName}>{mill.name}</div>
                    <div className={styles.millMeta}>{mill.location} · {mill.timezone}</div>
                    <div className={styles.badges}>
                      <Badge label={mill.deploymentMode} variant="deployment" />
                      {!mill.hasInternet && <Badge label="No Internet" color="#ef4444" variant="status" />}
                    </div>
                  </div>
                  {canManageMills(user?.role) && (
                    <div className={styles.headerActions}>
                      <button className={styles.iconBtn} onClick={() => openEditMill(mill)} title={t('mills.editTooltip')}>✎</button>
                      <button className={styles.iconBtnDanger} onClick={() => handleDeleteMill(mill)} title={t('mills.deleteTooltip')}>🗑</button>
                    </div>
                  )}
                </div>

                <div className={styles.cardBody}>
                  <div className={styles.areaHeader}>
                    <span>{t('mills.areasHeader', { count: millAreas.length })}</span>
                    {canManageAreas(user?.role) && (
                      <button className={styles.areaAddBtn} onClick={() => openAddArea(mill)}>{t('mills.addArea')}</button>
                    )}
                  </div>
                  {millAreas.length === 0 ? (
                    <p className={styles.noAreas}>
                      {t('mills.noAreas')}{canManageAreas(user?.role) ? t('mills.noAreasHint') : ''}
                    </p>
                  ) : (
                    <ul className={styles.areaList}>
                      {millAreas.map(area => (
                        <li key={area.id} className={styles.areaItem}>
                          <div className={styles.areaLeft}>
                            <span className={styles.areaName}>{area.name}</span>
                            {area.locationTypeName && (
                              <span className={styles.areaType}>{area.locationTypeName}</span>
                            )}
                          </div>
                          <div className={styles.areaRight}>
                            <span className={styles.areaCode}>{area.code}</span>
                            {canManageAreas(user?.role) && (
                              <span className={styles.areaActions}>
                                <button className={styles.areaIconBtn} onClick={() => openEditArea(area)} title={t('mills.editAreaTooltip')}>✎</button>
                                <button className={styles.areaIconBtnDanger} onClick={() => handleDeleteArea(area)} title={t('mills.deleteAreaTooltip')}>🗑</button>
                              </span>
                            )}
                          </div>
                        </li>
                      ))}
                    </ul>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Mill modal (Add + Edit) */}
      <Modal
        open={millOpen}
        title={millEditing ? t('mills.modal.editTitle') : t('mills.modal.addTitle')}
        onClose={() => setMillOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setMillOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="mill-form" disabled={millSaving}>
              {millSaving ? t('common.saving') : millEditing ? t('mills.modal.saveBtn') : t('mills.modal.createBtn')}
            </button>
          </>
        }
      >
        <form id="mill-form" className={f.formGrid} onSubmit={handleMillSubmit}>
          {millError && <p className={f.errorText}>{millError}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('mills.modal.name')}</label>
              <input className={f.input} required value={millName}
                onChange={e => setMillName(e.target.value)} placeholder={t('mills.modal.namePlaceholder')} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('mills.modal.code')}</label>
              <input className={f.input} required value={millCode} disabled={!!millEditing}
                onChange={e => setMillCode(e.target.value.toUpperCase())} placeholder={t('mills.modal.codePlaceholder')} />
            </div>
          </div>
          <div className={f.field}>
            <label className={`${f.label} ${f.required}`}>{t('mills.modal.location')}</label>
            <input className={f.input} required value={millLocation}
              onChange={e => setMillLocation(e.target.value)} placeholder={t('mills.modal.locationPlaceholder')} />
          </div>
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('mills.modal.timezone')}</label>
              <select className={f.select} value={millTimezone}
                onChange={e => setMillTimezone(e.target.value)}>
                {TIMEZONES.map(tz => <option key={tz} value={tz}>{tz}</option>)}
              </select>
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('mills.modal.deploymentMode')}</label>
              <select className={f.select} value={millMode}
                onChange={e => setMillMode(e.target.value as DeploymentMode)}>
                <option value="Cloud">{t('mills.modal.deploymentCloud')}</option>
                <option value="OnPremise">{t('mills.modal.deploymentOnPremise')}</option>
              </select>
            </div>
          </div>
          <div className={f.field}>
            <label className={styles.checkboxLabel}>
              <input type="checkbox" checked={millInternet}
                onChange={e => setMillInternet(e.target.checked)} />
              {t('mills.modal.hasInternet')}
            </label>
          </div>
        </form>
      </Modal>

      {/* Area modal (Add + Edit) */}
      <Modal
        open={areaOpen}
        title={areaEditing
          ? t('mills.areaModal.editTitle', { mill: areaTargetMill?.name ?? '' })
          : t('mills.areaModal.addTitle', { mill: areaTargetMill?.name ?? '' })}
        onClose={() => setAreaOpen(false)}
        footer={
          <>
            <button className={f.btnSecondary} onClick={() => setAreaOpen(false)}>{t('common.cancel')}</button>
            <button className={f.btnPrimary} form="area-form" disabled={areaSaving}>
              {areaSaving ? t('common.saving') : areaEditing ? t('common.save') : t('mills.areaModal.createBtn')}
            </button>
          </>
        }
      >
        <form id="area-form" className={f.formGrid} onSubmit={handleAreaSubmit}>
          {areaError && <p className={f.errorText}>{areaError}</p>}
          <div className={f.formRow}>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('mills.areaModal.name')}</label>
              <input className={f.input} required value={areaName}
                onChange={e => setAreaName(e.target.value)} placeholder={t('mills.areaModal.namePlaceholder')} />
            </div>
            <div className={f.field}>
              <label className={`${f.label} ${f.required}`}>{t('mills.areaModal.code')}</label>
              <input className={f.input} required value={areaCode} disabled={!!areaEditing}
                onChange={e => setAreaCode(e.target.value.toUpperCase())} placeholder={t('mills.areaModal.codePlaceholder')} />
            </div>
          </div>
          <div className={f.field}>
            <label className={f.label}>{t('mills.areaModal.locationType')}</label>
            <select className={f.select} value={areaLocType}
              onChange={e => setAreaLocType(e.target.value)}>
              <option value="">{t('common.none')}</option>
              {locationTypes.map(lt => <option key={lt.id} value={lt.id}>{lt.name}</option>)}
            </select>
            <span className={f.hint}>{t('mills.areaModal.locationTypeHint')}</span>
          </div>
          <div className={f.field}>
            <label className={f.label}>{t('mills.areaModal.description')}</label>
            <textarea className={f.textarea} value={areaDesc}
              onChange={e => setAreaDesc(e.target.value)} placeholder={t('mills.areaModal.descriptionPlaceholder')} />
          </div>
        </form>
      </Modal>
    </>
  );
}

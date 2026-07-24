import { useEffect, useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { getBranding, updateBranding } from '../../../api/branding';
import { useToast } from '../../../context/ToastContext';
import LoadingSpinner from '../../../components/common/LoadingSpinner';
import f from '../../../components/common/FormField.module.css';

/**
 * White-label branding: product name shown in the shell, optional logo URL
 * and accent colour. Values apply live on save (the shell re-reads them).
 */
export default function BrandingTab() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const toast = useToast();

  const { data, isLoading } = useQuery({ queryKey: ['branding'], queryFn: getBranding });

  const [productName, setProductName] = useState('EdgePulse');
  const [logoUrl, setLogoUrl] = useState('');
  const [accent, setAccent] = useState('#3b82f6');
  const [useAccent, setUseAccent] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!data) return;
    setProductName(data.productName);
    setLogoUrl(data.logoUrl ?? '');
    setAccent(data.accentColor ?? '#3b82f6');
    setUseAccent(!!data.accentColor);
  }, [data]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault(); setSaving(true); setError(null);
    try {
      await updateBranding({
        productName,
        logoUrl: logoUrl || null,
        accentColor: useAccent ? accent : null,
      });
      await qc.invalidateQueries({ queryKey: ['branding'] });
      toast.success(t('configuration.branding.saved'));
    } catch {
      setError(t('configuration.branding.saveError'));
    } finally { setSaving(false); }
  }

  if (isLoading) return <LoadingSpinner message={t('common.loading')} />;

  return (
    <form className={f.formGrid} style={{ maxWidth: 560 }} onSubmit={handleSubmit}>
      {error && <p className={f.errorText}>{error}</p>}
      <div className={f.field}>
        <label className={`${f.label} ${f.required}`}>
          {t('configuration.branding.productName')}
        </label>
        <input className={f.input} required maxLength={60} value={productName}
          onChange={e => setProductName(e.target.value)} />
      </div>
      <div className={f.field}>
        <label className={f.label}>{t('configuration.branding.logoUrl')}</label>
        <input className={f.input} type="url" value={logoUrl}
          onChange={e => setLogoUrl(e.target.value)}
          placeholder="https://…/logo.png" />
      </div>
      <div className={f.field}>
        <label className={f.label}>{t('configuration.branding.accent')}</label>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <input type="checkbox" checked={useAccent}
            onChange={e => setUseAccent(e.target.checked)} />
          <input className={f.input} type="color" value={accent} disabled={!useAccent}
            onChange={e => setAccent(e.target.value)}
            style={{ padding: 2, height: 36, width: 80 }} />
          <span style={{ fontSize: 12, color: 'var(--color-text-muted)' }}>
            {useAccent ? accent : t('configuration.branding.accentDefault')}
          </span>
        </div>
      </div>
      <div>
        <button className={f.btnPrimary} disabled={saving}>
          {saving ? t('common.saving') : t('common.save')}
        </button>
      </div>
    </form>
  );
}

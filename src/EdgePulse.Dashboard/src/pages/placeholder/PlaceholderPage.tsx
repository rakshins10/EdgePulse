interface Props {
  title: string;
}

export default function PlaceholderPage({ title }: Props) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      height: '60vh', flexDirection: 'column', gap: 12, color: '#475569',
    }}>
      <div style={{ fontSize: '2rem' }}>🚧</div>
      <div style={{ fontSize: '1rem', fontWeight: 600, color: '#64748b' }}>
        {title}
      </div>
      <div style={{ fontSize: '0.8rem' }}>
        Coming in a future sprint
      </div>
    </div>
  );
}

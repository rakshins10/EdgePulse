import keycloak from '../keycloak';
import type { CurrentUser, UserRole } from '../types/api';

export function useCurrentUser(): CurrentUser | null {
  const p = keycloak.tokenParsed as Record<string, unknown> | undefined;
  if (!p) return null;

  const rawAreas = p['areaIds'];
  const areaIds: string[] = Array.isArray(rawAreas)
    ? (rawAreas as string[])
    : typeof rawAreas === 'string'
    ? [rawAreas]
    : [];

  return {
    userId:   (p['sub'] as string) ?? '',
    email:    (p['email'] as string) ?? '',
    fullName: (p['name'] as string) ?? (p['email'] as string) ?? '',
    tenantId: (p['tenantId'] as string) ?? '',
    role:     (p['role'] as UserRole) ?? 'Operator',
    millId:   (p['millId'] as string | undefined) ?? null,
    areaIds,
  };
}

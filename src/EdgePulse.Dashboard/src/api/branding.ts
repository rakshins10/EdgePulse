import apiClient from './client';

export interface BrandingDto {
  productName: string;
  logoUrl: string | null;
  accentColor: string | null;
}

export const getBranding = (): Promise<BrandingDto> =>
  apiClient.get<BrandingDto>('/branding').then(r => r.data);

export const updateBranding = (body: BrandingDto): Promise<void> =>
  apiClient.put('/branding', body).then(() => undefined);

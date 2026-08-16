import { UserProfileDto } from './api.models';

export interface MobileDeviceInfo {
  deviceName: string;
  platform: string;
  appVersion: string;
}

export interface MobileAuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  sessionId: string;
  user: UserProfileDto;
}

export interface MobileSessionDto {
  id: string;
  deviceName: string;
  platform: string;
  appVersion: string;
  createdAtUtc: string;
  lastUsedAtUtc: string | null;
  expiresAtUtc: string;
  isCurrent: boolean;
}

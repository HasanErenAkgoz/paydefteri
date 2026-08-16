import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { App } from '@capacitor/app';
import { SecureStorage } from '@aparajita/capacitor-secure-storage';
import {
  Observable,
  catchError,
  finalize,
  from,
  map,
  of,
  shareReplay,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { environment } from '../../../environments/environment';
import { MobileAuthResult, MobileDeviceInfo } from '../models/mobile-auth.models';
import { PlatformService } from '../platform/platform.service';

const REFRESH_TOKEN_KEY = 'mobile_refresh_token';
const SESSION_ID_KEY = 'mobile_session_id';

@Injectable({ providedIn: 'root' })
export class MobileSessionService {
  private readonly platform = inject(PlatformService);
  private readonly http: HttpClient;
  private readonly accessTokenSignal = signal<string | null>(null);
  private readonly sessionIdSignal = signal<string | null>(null);
  private memoryRefreshToken: string | null = null;
  private refreshRequest: Observable<string> | null = null;

  constructor(handler: HttpBackend) {
    this.http = new HttpClient(handler);
  }

  get enabled(): boolean {
    return environment.mobile;
  }

  get accessToken(): string | null {
    return this.accessTokenSignal();
  }

  get sessionId(): string | null {
    return this.sessionIdSignal();
  }

  login(email: string, password: string): Observable<MobileAuthResult> {
    return from(this.deviceInfo()).pipe(
      switchMap((device) =>
        this.http.post<MobileAuthResult>(`${environment.apiUrl}/mobile/v1/auth/login`, {
          email,
          password,
          device,
        })
      ),
      switchMap((result) => this.accept(result))
    );
  }

  register(email: string, password: string, displayName: string): Observable<MobileAuthResult> {
    return from(this.deviceInfo()).pipe(
      switchMap((device) =>
        this.http.post<MobileAuthResult>(`${environment.apiUrl}/mobile/v1/auth/register`, {
          email,
          password,
          displayName,
          device,
        })
      ),
      switchMap((result) => this.accept(result))
    );
  }

  ensureAccessToken(): Observable<string> {
    const accessToken = this.accessToken;
    return accessToken ? of(accessToken) : this.refreshAccessToken();
  }

  refreshAccessToken(): Observable<string> {
    if (this.refreshRequest) {
      return this.refreshRequest;
    }

    this.refreshRequest = from(this.readRefreshToken()).pipe(
      switchMap((refreshToken) =>
        refreshToken
          ? this.http.post<MobileAuthResult>(`${environment.apiUrl}/mobile/v1/auth/refresh`, {
              refreshToken,
            })
          : throwError(() => new Error('Mobil oturum bulunamadı.'))
      ),
      switchMap((result) => this.accept(result)),
      map((result) => result.accessToken),
      catchError((error) =>
        from(this.clear()).pipe(switchMap(() => throwError(() => error)))
      ),
      finalize(() => (this.refreshRequest = null)),
      shareReplay({ bufferSize: 1, refCount: false })
    );
    return this.refreshRequest;
  }

  logout(): Observable<void> {
    return from(this.readRefreshToken()).pipe(
      switchMap((refreshToken) =>
        refreshToken
          ? this.http.post<void>(`${environment.apiUrl}/mobile/v1/auth/logout`, { refreshToken })
          : of(undefined)
      ),
      catchError(() => of(undefined)),
      switchMap(() => from(this.clear()))
    );
  }

  async clear(): Promise<void> {
    this.accessTokenSignal.set(null);
    this.sessionIdSignal.set(null);
    this.memoryRefreshToken = null;
    if (this.platform.isNative) {
      await Promise.all([
        SecureStorage.remove(REFRESH_TOKEN_KEY),
        SecureStorage.remove(SESSION_ID_KEY),
      ]);
    }
  }

  private accept(result: MobileAuthResult): Observable<MobileAuthResult> {
    return from(this.persist(result)).pipe(map(() => result));
  }

  private async persist(result: MobileAuthResult): Promise<void> {
    this.accessTokenSignal.set(result.accessToken);
    this.sessionIdSignal.set(result.sessionId);
    this.memoryRefreshToken = result.refreshToken;
    if (this.platform.isNative) {
      await SecureStorage.set(REFRESH_TOKEN_KEY, result.refreshToken);
      await SecureStorage.set(SESSION_ID_KEY, result.sessionId);
    }
  }

  private async readRefreshToken(): Promise<string | null> {
    if (!this.platform.isNative) {
      return this.memoryRefreshToken;
    }

    const value = await SecureStorage.get(REFRESH_TOKEN_KEY, false);
    const sessionId = await SecureStorage.get(SESSION_ID_KEY, false);
    this.sessionIdSignal.set(typeof sessionId === 'string' ? sessionId : null);
    return typeof value === 'string' ? value : null;
  }

  private async deviceInfo(): Promise<MobileDeviceInfo> {
    if (!this.platform.isNative) {
      return { deviceName: 'Web geliştirme', platform: 'web', appVersion: 'dev' };
    }

    const app = await App.getInfo();
    return {
      deviceName: this.platform.platform === 'ios' ? 'iPhone / iPad' : 'Android cihaz',
      platform: this.platform.platform,
      appVersion: app.version,
    };
  }
}

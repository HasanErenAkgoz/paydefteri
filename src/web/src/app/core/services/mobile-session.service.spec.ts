import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlatformService } from '../platform/platform.service';
import { MobileAuthResult } from '../models/mobile-auth.models';
import { MobileSessionService } from './mobile-session.service';

describe('MobileSessionService', () => {
  let service: MobileSessionService;
  let http: HttpTestingController;
  const originalMobile = environment.mobile;

  beforeEach(() => {
    (environment as { mobile: boolean }).mobile = true;
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        MobileSessionService,
        {
          provide: PlatformService,
          useValue: { isNative: false, platform: 'web' },
        },
      ],
    });
    const platform = TestBed.inject(PlatformService);
    Object.defineProperties(platform, {
      isNative: { value: false },
      platform: { value: 'web' },
    });
    localStorage.clear();
    service = TestBed.inject(MobileSessionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    (environment as { mobile: boolean }).mobile = originalMobile;
  });

  it('returns the issued mobile session after login', async () => {
    const result = authResult('access-1', 'refresh-1', 'session-1');
    const promise = firstValueFrom(service.login('user@example.com', 'Secret123!'));
    await Promise.resolve();

    const request = http.expectOne(`${environment.apiUrl}/mobile/v1/auth/login`);
    request.flush(result);

    await expectAsync(promise).toBeResolvedTo(result);
    expect(service.accessToken).toBe('access-1');
    expect(service.sessionId).toBe('session-1');
  });

  it('coalesces simultaneous refresh requests and rotates tokens once', async () => {
    const loginPromise = firstValueFrom(service.login('user@example.com', 'Secret123!'));
    await Promise.resolve();
    http.expectOne(`${environment.apiUrl}/mobile/v1/auth/login`).flush(
      authResult('access-1', 'refresh-1', 'session-1')
    );
    await loginPromise;

    const first = firstValueFrom(service.refreshAccessToken());
    const second = firstValueFrom(service.refreshAccessToken());
    await Promise.resolve();
    const requests = http.match(`${environment.apiUrl}/mobile/v1/auth/refresh`);
    expect(requests.length).toBe(1);
    expect(requests[0]!.request.body.refreshToken).toBe('refresh-1');
    requests[0]!.flush(authResult('access-2', 'refresh-2', 'session-2'));

    await expectAsync(first).toBeResolvedTo('access-2');
    await expectAsync(second).toBeResolvedTo('access-2');
    expect(service.sessionId).toBe('session-2');
  });

  it('keeps the refresh token after a temporary refresh failure', async () => {
    const loginPromise = firstValueFrom(service.login('user@example.com', 'Secret123!'));
    await Promise.resolve();
    http.expectOne(`${environment.apiUrl}/mobile/v1/auth/login`).flush(
      authResult('access-1', 'refresh-1', 'session-1')
    );
    await loginPromise;

    const refreshPromise = firstValueFrom(service.refreshAccessToken());
    await Promise.resolve();
    http.expectOne(`${environment.apiUrl}/mobile/v1/auth/refresh`).flush(
      'Offline',
      { status: 0, statusText: 'Unknown Error' }
    );
    await expectAsync(refreshPromise).toBeRejected();

    const retryPromise = firstValueFrom(service.refreshAccessToken());
    await Promise.resolve();
    const retry = http.expectOne(`${environment.apiUrl}/mobile/v1/auth/refresh`);
    expect(retry.request.body.refreshToken).toBe('refresh-1');
    retry.flush(authResult('access-2', 'refresh-2', 'session-2'));
    await expectAsync(retryPromise).toBeResolvedTo('access-2');
  });

  function authResult(
    accessToken: string,
    refreshToken: string,
    sessionId: string
  ): MobileAuthResult {
    return {
      accessToken,
      accessTokenExpiresAt: '2026-08-14T11:00:00Z',
      refreshToken,
      refreshTokenExpiresAt: '2026-09-14T11:00:00Z',
      sessionId,
      user: {
        userId: 'user-id',
        email: 'user@example.com',
        displayName: 'Test User',
        emailConfirmed: true,
      },
    };
  }
});

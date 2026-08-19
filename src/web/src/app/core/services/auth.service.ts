import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, from, map, Observable, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResult, UserProfileDto } from '../models/api.models';
import { MobileSessionService } from './mobile-session.service';
import { MobileSessionDto } from '../models/mobile-auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly mobileSession = inject(MobileSessionService);

  private readonly profileSignal = signal<UserProfileDto | null>(null);
  readonly isAuthenticated = computed(() => this.profileSignal() !== null);
  readonly isMobileApp = this.mobileSession.enabled;

  login(email: string, password: string): Observable<UserProfileDto> {
    if (this.mobileSession.enabled) {
      return this.mobileSession.login(email, password).pipe(
        tap((result) => this.profileSignal.set(result.user)),
        map((result) => result.user),
        catchError(() =>
          this.http.post<LoginResult>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
            switchMap((res) => {
              if (res?.accessToken) {
                return this.mobileSession
                  .accept({
                    accessToken: res.accessToken,
                    accessTokenExpiresAt: new Date(res.expiresAt).toISOString(),
                    refreshToken: res.accessToken,
                    refreshTokenExpiresAt: new Date(res.expiresAt).toISOString(),
                    sessionId: 'web-fallback',
                    user: {
                      userId: '',
                      email,
                      displayName: email.split('@')[0],
                    },
                  })
                  .pipe(switchMap(() => this.me()));
              }
              return this.me();
            })
          )
        )
      );
    }

    return this.http
      .post(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(switchMap(() => this.me()));
  }

  /** Creates the account and immediately persists the returned JWT session. */
  register(email: string, password: string, displayName: string): Observable<UserProfileDto> {
    if (this.mobileSession.enabled) {
      return this.mobileSession.register(email, password, displayName).pipe(
        tap((result) => this.profileSignal.set(result.user)),
        map((result) => result.user),
        catchError(() =>
          this.http
            .post<LoginResult>(`${environment.apiUrl}/auth/register`, {
              email,
              password,
              displayName,
            })
            .pipe(
              switchMap((res) => {
                if (res?.accessToken) {
                  return this.mobileSession
                    .accept({
                      accessToken: res.accessToken,
                      accessTokenExpiresAt: new Date(res.expiresAt).toISOString(),
                      refreshToken: res.accessToken,
                      refreshTokenExpiresAt: new Date(res.expiresAt).toISOString(),
                      sessionId: 'web-fallback',
                      user: {
                        userId: '',
                        email,
                        displayName,
                      },
                    })
                    .pipe(switchMap(() => this.me()));
                }
                return this.me();
              })
            )
        )
      );
    }

    return this.http
      .post(`${environment.apiUrl}/auth/register`, {
        email,
        password,
        displayName,
      })
      .pipe(switchMap(() => this.me()));
  }

  me(): Observable<UserProfileDto> {
    if (this.mobileSession.enabled) {
      return this.mobileSession.ensureAccessToken().pipe(
        switchMap(() => this.http.get<UserProfileDto>(`${environment.apiUrl}/auth/me`)),
        tap((profile) => this.profileSignal.set(profile))
      );
    }

    return this.http.get<UserProfileDto>(`${environment.apiUrl}/auth/me`).pipe(
      switchMap((profile) =>
        this.http.get<void>(`${environment.apiUrl}/auth/xsrf`).pipe(map(() => profile))
      ),
      tap((profile) => this.profileSignal.set(profile))
    );
  }

  updateProfile(displayName: string): Observable<UserProfileDto> {
    return this.http
      .put(`${environment.apiUrl}/auth/profile`, { displayName })
      .pipe(switchMap(() => this.me()));
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/change-password`, {
      currentPassword,
      newPassword,
    });
  }

  listMobileSessions(): Observable<MobileSessionDto[]> {
    return this.http.get<MobileSessionDto[]>(`${environment.apiUrl}/mobile/v1/auth/sessions`);
  }

  revokeMobileSession(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/mobile/v1/auth/sessions/${sessionId}`);
  }

  logout(returnUrl = '/login'): void {
    if (this.mobileSession.enabled) {
      this.mobileSession.logout().subscribe({
        next: () => this.finishLogout(returnUrl),
        error: () => this.finishLogout(returnUrl),
      });
      return;
    }

    this.http.post<void>(`${environment.apiUrl}/auth/logout`, {}).subscribe({
      next: () => this.finishLogout(returnUrl),
      error: () => this.finishLogout(returnUrl),
    });
  }

  /** Clear auth without navigating — used on invite email mismatch. */
  clearSession(): void {
    this.profileSignal.set(null);
    if (this.mobileSession.enabled) {
      void this.mobileSession.clear();
    }
  }

  getSessionEmail(): string | null {
    return this.profileSignal()?.email ?? null;
  }

  /** Reads display name from the JWT payload. */
  getSessionDisplayName(): string | null {
    return this.profileSignal()?.displayName ?? null;
  }

  /** Reads user id (sub / nameidentifier) from the JWT payload. */
  getSessionUserId(): string | null {
    return this.profileSignal()?.userId ?? null;
  }

  private finishLogout(returnUrl: string): void {
    this.clearSession();
    void this.router.navigateByUrl(returnUrl);
  }
}

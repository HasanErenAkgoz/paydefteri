import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResult, RegisterResult, UserProfileDto } from '../models/api.models';

const TOKEN_KEY = 'ftt_access_token';
const EXPIRES_KEY = 'ftt_expires_at';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly tokenSignal = signal<string | null>(this.readToken());
  readonly token = this.tokenSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenSignal());

  login(email: string, password: string): Observable<LoginResult> {
    return this.http
      .post<LoginResult>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap((result) => this.persistSession(result)));
  }

  register(email: string, password: string, displayName: string): Observable<RegisterResult> {
    return this.http.post<RegisterResult>(`${environment.apiUrl}/auth/register`, {
      email,
      password,
      displayName,
    });
  }

  me(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${environment.apiUrl}/auth/me`);
  }

  updateProfile(displayName: string): Observable<LoginResult> {
    return this.http
      .put<LoginResult>(`${environment.apiUrl}/auth/profile`, { displayName })
      .pipe(tap((result) => this.persistSession(result)));
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/change-password`, {
      currentPassword,
      newPassword,
    });
  }

  logout(returnUrl = '/login'): void {
    this.clearSession();
    void this.router.navigateByUrl(returnUrl);
  }

  /** Clear auth without navigating — used on invite email mismatch. */
  clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    this.tokenSignal.set(null);
  }

  getAccessToken(): string | null {
    return this.tokenSignal();
  }

  /** Reads email claim from the JWT payload (no API round-trip). */
  getSessionEmail(): string | null {
    return this.readClaim(['email', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']);
  }

  /** Reads display name from the JWT payload. */
  getSessionDisplayName(): string | null {
    return this.readClaim(['display_name', 'name', 'unique_name']);
  }

  /** Reads user id (sub / nameidentifier) from the JWT payload. */
  getSessionUserId(): string | null {
    return this.readClaim([
      'sub',
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
    ]);
  }

  private readClaim(keys: string[]): string | null {
    const token = this.tokenSignal();
    if (!token) {
      return null;
    }
    try {
      const segment = token.split('.')[1];
      if (!segment) {
        return null;
      }
      const json = atob(segment.replace(/-/g, '+').replace(/_/g, '/'));
      const payload = JSON.parse(json) as Record<string, unknown>;
      for (const key of keys) {
        const value = payload[key];
        if (typeof value === 'string' && value.trim()) {
          return value.trim();
        }
      }
      return null;
    } catch {
      return null;
    }
  }

  private persistSession(result: LoginResult): void {
    localStorage.setItem(TOKEN_KEY, result.accessToken);
    localStorage.setItem(EXPIRES_KEY, result.expiresAt);
    this.tokenSignal.set(result.accessToken);
  }

  private readToken(): string | null {
    const token = localStorage.getItem(TOKEN_KEY);
    const expires = localStorage.getItem(EXPIRES_KEY);
    if (!token) {
      return null;
    }
    if (expires && new Date(expires).getTime() <= Date.now()) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(EXPIRES_KEY);
      return null;
    }
    return token;
  }
}

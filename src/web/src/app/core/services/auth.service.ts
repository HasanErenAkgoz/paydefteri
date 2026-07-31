import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResult, RegisterResult } from '../models/api.models';

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

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EXPIRES_KEY);
    this.tokenSignal.set(null);
    void this.router.navigateByUrl('/login');
  }

  getAccessToken(): string | null {
    return this.tokenSignal();
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

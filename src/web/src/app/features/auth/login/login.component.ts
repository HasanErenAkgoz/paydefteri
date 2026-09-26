import { isPlatformBrowser } from '@angular/common';
import { Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { GoogleAuthService } from '../../../core/services/google-auth.service';
import { GoogleSignInButtonComponent } from '../../../shared/google-sign-in/google-sign-in-button.component';
import { ToastService } from '../../../shared/toast/toast.service';
import { apiErrorMessage } from '../../../shared/utils/api-error';

const REMEMBER_EMAIL_KEY = 'paydefteri.rememberEmail';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, GoogleSignInButtonComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly google = inject(GoogleAuthService);

  email = '';
  password = '';
  rememberMe = false;
  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly googleLoading = signal(false);
  /** Drops the "veya" divider when the Google button cannot be shown. */
  readonly googleVisible = signal(this.google.available);

  ngOnInit(): void {
    if (!this.isBrowser) {
      return;
    }
    const saved = localStorage.getItem(REMEMBER_EMAIL_KEY);
    if (saved) {
      this.email = saved;
      this.rememberMe = true;
    }
  }

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  forgotPassword(): void {
    this.toast.info('Şifre sıfırlama yakında eklenecek.');
  }

  signInWithGoogle(idToken: string): void {
    if (this.googleLoading()) {
      return;
    }

    this.googleLoading.set(true);
    this.auth.loginWithGoogle(idToken).subscribe({
      next: () => {
        this.googleLoading.set(false);
        void this.router.navigateByUrl('/home');
      },
      error: (err) => {
        this.googleLoading.set(false);
        this.toast.error(apiErrorMessage(err, 'Google ile giriş başarısız.'));
      },
    });
  }

  submit(): void {
    const email = this.email.trim().toLowerCase();
    const password = this.password;
    if (!email || !password) {
      this.toast.error('E-posta ve şifre gerekli.');
      return;
    }

    if (this.isBrowser) {
      if (this.rememberMe) {
        localStorage.setItem(REMEMBER_EMAIL_KEY, email);
      } else {
        localStorage.removeItem(REMEMBER_EMAIL_KEY);
      }
    }

    this.loading.set(true);
    this.auth.login(email, password, this.rememberMe).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/home');
      },
      error: (err) => {
        this.loading.set(false);
        const status = (err as { status?: number } | null)?.status;
        const detail =
          (err as { error?: { detail?: string } } | null)?.error?.detail?.trim() ||
          (status === 401 || status === 403
            ? 'E-posta veya şifre hatalı.'
            : 'Giriş başarısız.');
        this.toast.error(detail);
      },
    });
  }
}

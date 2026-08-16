import { isPlatformBrowser } from '@angular/common';
import { Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/toast/toast.service';

const REMEMBER_EMAIL_KEY = 'paydefteri.rememberEmail';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  email = '';
  password = '';
  rememberMe = false;
  readonly showPassword = signal(false);
  readonly loading = signal(false);

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

  googleLogin(): void {
    this.toast.info('Google ile giriş yakında eklenecek.');
  }

  submit(): void {
    const email = this.email.trim();
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
    this.auth.login(email, password).subscribe({
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

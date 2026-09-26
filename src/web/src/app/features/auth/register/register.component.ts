import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { GoogleAuthService } from '../../../core/services/google-auth.service';
import { GoogleSignInButtonComponent } from '../../../shared/google-sign-in/google-sign-in-button.component';
import { ToastService } from '../../../shared/toast/toast.service';
import { apiErrorMessage } from '../../../shared/utils/api-error';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, GoogleSignInButtonComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly google = inject(GoogleAuthService);

  displayName = '';
  email = '';
  password = '';
  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly googleLoading = signal(false);
  /** Drops the "veya" divider when the Google button cannot be shown. */
  readonly googleVisible = signal(this.google.available);

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  /** The native picker opened but failed; a dismissal never reaches here. */
  reportGoogleFailure(): void {
    this.toast.error('Google ile kayıt başarısız.');
  }

  signInWithGoogle(idToken: string): void {
    if (this.googleLoading()) {
      return;
    }

    // Google's button covers sign-up too: the API creates the account the first
    // time an address turns up, so there is nothing extra to collect here.
    this.googleLoading.set(true);
    this.auth.loginWithGoogle(idToken).subscribe({
      next: () => {
        this.googleLoading.set(false);
        void this.router.navigateByUrl('/home');
      },
      error: (err) => {
        this.googleLoading.set(false);
        this.toast.error(apiErrorMessage(err, 'Google ile kayıt başarısız.'));
      },
    });
  }

  submit(): void {
    const email = this.email.trim().toLowerCase();
    const password = this.password;
    const displayName = this.displayName.trim();
    if (!displayName || !email || !password) {
      this.toast.error('Ad, e-posta ve şifre gerekli.');
      return;
    }
    if (password.length < 10 || !/[A-Za-z]/.test(password) || !/\d/.test(password)) {
      this.toast.error('Şifre en az 10 karakter; en az bir harf ve rakam içermeli.');
      return;
    }

    this.loading.set(true);
    this.auth.register(email, password, displayName).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/home');
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, 'Kayıt başarısız.'));
      },
    });
  }
}

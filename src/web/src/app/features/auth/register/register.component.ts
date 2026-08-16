import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/toast/toast.service';
import { apiErrorMessage } from '../../../shared/utils/api-error';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  displayName = '';
  email = '';
  password = '';
  readonly showPassword = signal(false);
  readonly loading = signal(false);

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  googleLogin(): void {
    this.toast.info('Google ile giriş yakında eklenecek.');
  }

  submit(): void {
    const email = this.email.trim();
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

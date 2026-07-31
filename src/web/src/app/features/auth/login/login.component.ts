import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  submit(): void {
    this.error.set(null);
    this.loading.set(true);
    this.auth.login(this.email.trim(), this.password).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/plans');
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.title ?? 'Giriş başarısız.');
      },
    });
  }
}

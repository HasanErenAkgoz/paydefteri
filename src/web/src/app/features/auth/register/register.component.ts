import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';

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

  displayName = '';
  email = '';
  password = '';
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  submit(): void {
    this.error.set(null);
    this.loading.set(true);
    const email = this.email.trim();
    const password = this.password;
    this.auth
      .register(email, password, this.displayName.trim())
      .pipe(switchMap(() => this.auth.login(email, password)))
      .subscribe({
        next: () => {
          this.loading.set(false);
          void this.router.navigateByUrl('/plans');
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.detail ?? err?.error?.title ?? 'Kayıt başarısız.');
        },
      });
  }
}

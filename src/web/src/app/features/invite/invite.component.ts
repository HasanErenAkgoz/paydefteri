import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { InvitePreviewDto } from '../../core/models/api.models';
import { AuthService } from '../../core/services/auth.service';
import { MembershipApi } from '../../core/services/membership.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { ToastService } from '../../shared/toast/toast.service';

type AuthMode = 'login' | 'register';

@Component({
  selector: 'app-invite',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './invite.component.html',
  styleUrl: './invite.component.scss',
})
export class InviteComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  private readonly membershipApi = inject(MembershipApi);
  private readonly planContext = inject(PlanContextService);
  private readonly toast = inject(ToastService);

  readonly preview = signal<InvitePreviewDto | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null); // shown in card for mismatch/invalid
  readonly mode = signal<AuthMode>('register');
  readonly emailMismatch = signal(false);
  readonly sessionEmail = signal<string | null>(null);

  token = '';
  email = '';
  password = '';
  displayName = '';

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token')?.trim() ?? '';
    if (!this.token) {
      this.loading.set(false);
      this.flashError('Davet bağlantısı geçersiz.');
      return;
    }
    this.loadPreview();
  }

  setMode(mode: AuthMode): void {
    this.mode.set(mode);
    this.clearError();
  }

  switchAccount(): void {
    this.auth.clearSession();
    this.emailMismatch.set(false);
    this.sessionEmail.set(null);
    this.clearError();
    this.email = this.preview()?.email ?? '';
    this.password = '';
    this.mode.set('register');
  }

  submitAuth(): void {
    if (!this.preview()?.isAcceptable) {
      return;
    }
    this.clearError();
    this.busy.set(true);
    const email = this.email.trim();
    const password = this.password;
    const expected = this.preview()!.email;

    if (email.toLowerCase() !== expected.toLowerCase()) {
      this.busy.set(false);
      this.flashError(`Bu davet ${expected} adresine ait. Aynı e-posta ile giriş yapın.`);
      return;
    }

    const auth$ =
      this.mode() === 'register'
        ? this.auth
            .register(email, password, this.displayName.trim() || email.split('@')[0])
            .pipe(switchMap(() => this.auth.login(email, password)))
        : this.auth.login(email, password);

    auth$.subscribe({
      next: () => this.acceptInvite(),
      error: (err) => {
        this.busy.set(false);
        this.flashError(err?.error?.detail ?? err?.error?.title ?? 'İşlem başarısız.');
      },
    });
  }

  acceptInvite(): void {
    this.busy.set(true);
    this.clearError();
    this.membershipApi.accept(this.token).subscribe({
      next: (plan) => {
        this.busy.set(false);
        this.planContext.setPlan(plan.id, plan.title, plan.description);
        void this.router.navigate(['/plans', plan.id, 'dashboard']);
      },
      error: (err) => {
        this.busy.set(false);
        const detail = err?.error?.detail ?? err?.error?.title ?? 'Davet kabul edilemedi.';
        if (typeof detail === 'string' && detail.toLowerCase().includes('e-posta')) {
          this.emailMismatch.set(true);
          this.sessionEmail.set(this.auth.getSessionEmail());
        }
        this.flashError(detail);
      },
    });
  }

  private loadPreview(): void {
    this.loading.set(true);
    this.clearError();
    this.membershipApi.preview(this.token).subscribe({
      next: (preview) => {
        this.preview.set(preview);
        this.email = preview.email;
        this.loading.set(false);
        if (!preview.isAcceptable) {
          this.flashError(
            preview.status === 'Pending'
              ? 'Bu davetin süresi dolmuş.'
              : `Bu davet artık geçerli değil (${preview.status}).`
          );
          return;
        }
        if (!this.auth.isAuthenticated()) {
          return;
        }

        const sessionEmail = this.auth.getSessionEmail();
        this.sessionEmail.set(sessionEmail);
        if (sessionEmail && sessionEmail.toLowerCase() === preview.email.toLowerCase()) {
          this.acceptInvite();
          return;
        }

        this.emailMismatch.set(true);
        this.flashError(
          sessionEmail
            ? `Bu davet ${preview.email} adresine ait. Şu an ${sessionEmail} ile giriş yapmışsınız.`
            : `Bu davet ${preview.email} adresine ait. Farklı bir hesapla giriş yapmışsınız.`
        );
      },
      error: (err) => {
        this.loading.set(false);
        this.preview.set(null);
        this.flashError(err?.error?.detail ?? err?.error?.title ?? 'Davet bulunamadı.');
      },
    });
  }

  private clearError(): void {
    this.error.set(null);
  }

  private flashError(message: string): void {
    this.error.set(message);
    this.toast.error(message);
  }
}

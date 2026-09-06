import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { apiErrorMessage } from '../../../shared/utils/api-error';

type VerifyState = 'working' | 'done' | 'failed';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="verify-page">
      <section class="verify-card">
        <div class="verify-icon" aria-hidden="true">
          {{ state() === 'done' ? '✅' : state() === 'failed' ? '⚠️' : '⏳' }}
        </div>

        @switch (state()) {
          @case ('working') {
            <h1>E-postanız doğrulanıyor…</h1>
            <p>Bu işlem birkaç saniye sürebilir.</p>
          }
          @case ('done') {
            <h1>E-postanız doğrulandı</h1>
            <p>Artık plan davetleri gönderebilir ve tüm özellikleri kullanabilirsiniz.</p>
            <a class="verify-btn" routerLink="/plans">Planlarıma git</a>
          }
          @case ('failed') {
            <h1>Doğrulama tamamlanamadı</h1>
            <p>{{ error() }}</p>
            <p class="verify-hint">
              Bağlantının süresi dolmuş olabilir. Profil sayfanızdan yeni bir doğrulama e-postası
              isteyebilirsiniz.
            </p>
            <a class="verify-btn" routerLink="/profile">Profile git</a>
          }
        }
      </section>
    </main>
  `,
  styles: [
    `
      .verify-page {
        display: grid;
        place-items: center;
        min-height: 70vh;
        padding: 24px 16px;
      }

      .verify-card {
        width: 100%;
        max-width: 460px;
        padding: 32px 28px;
        border: 1px solid rgba(148, 163, 184, 0.2);
        border-radius: 16px;
        background: rgba(15, 23, 42, 0.55);
        text-align: center;
      }

      .verify-icon {
        font-size: 2.4rem;
      }

      h1 {
        margin: 12px 0 8px;
        font-size: 1.25rem;
        font-weight: 750;
      }

      p {
        margin: 0 0 8px;
        color: rgba(226, 232, 240, 0.78);
        font-size: 0.9rem;
        line-height: 1.55;
      }

      .verify-hint {
        font-size: 0.82rem;
        color: rgba(226, 232, 240, 0.6);
      }

      .verify-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        min-height: 44px;
        margin-top: 16px;
        padding: 0 22px;
        border-radius: 10px;
        background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
        color: #ffffff;
        font-weight: 700;
        text-decoration: none;
      }
    `,
  ],
})
export class VerifyEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly state = signal<VerifyState>('working');
  readonly error = signal('Doğrulama bağlantısı geçersiz.');

  ngOnInit(): void {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!userId || !token) {
      this.state.set('failed');
      return;
    }

    this.auth.confirmEmail(userId, token).subscribe({
      next: () => this.state.set('done'),
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'Doğrulama bağlantısı geçersiz veya süresi dolmuş.'));
        this.state.set('failed');
      },
    });
  }
}

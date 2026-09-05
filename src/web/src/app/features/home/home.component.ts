import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { planHomeCommands } from '../../core/utils/plan-routes';

/** Giriş sonrası: plan varsa ana ekranına, yoksa plan listesindeki şablon/boş duruma. */
@Component({
  selector: 'app-home',
  standalone: true,
  template: `
    <p class="muted" style="padding: 48px 16px; text-align: center">
      {{ message() }}
    </p>
  `,
})
export class HomeComponent implements OnInit {
  private readonly plansApi = inject(PlansApi);
  private readonly planContext = inject(PlanContextService);
  private readonly router = inject(Router);

  readonly message = signal('Yönlendiriliyor…');

  ngOnInit(): void {
    this.plansApi.list().subscribe({
      next: (plans) => {
        if (plans.length > 0) {
          const plan = [...plans].sort((a, b) =>
            String(b.createdAtUtc).localeCompare(String(a.createdAtUtc))
          )[0];
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          void this.router.navigate(planHomeCommands(plan.id, plan.planType));
          return;
        }

        // İlk kullanım: hesabı boş bir "Yeni Özel Plan" ile kirletmek yerine
        // plan listesindeki hazır şablon / boş durum ekranına yönlendir.
        // manage=1 doğrudan veriliyor ki plan listesi boş hesapta kendini
        // ?manage=1'e yeniden yönlendirmek zorunda kalmasın (fazladan tur + istek).
        this.planContext.clear();
        void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
      },
      error: (err) => {
        this.message.set(err?.error?.detail ?? 'Planlar yüklenemedi.');
      },
    });
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { switchMap } from 'rxjs';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { planHomeCommands } from '../../core/utils/plan-routes';

/** Giriş sonrası: plan varsa ana ekranına, yoksa boş plan + Kurulum. */
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

        this.bootstrapEmptyPlan();
      },
      error: (err) => {
        this.message.set(err?.error?.detail ?? 'Planlar yüklenemedi.');
      },
    });
  }

  private bootstrapEmptyPlan(): void {
    this.message.set('Plan hazırlanıyor…');
    this.plansApi
      .create({
        title: 'Yeni Özel Plan',
        description: 'Özel takip planı',
        planType: 'Installment',
      })
      .pipe(switchMap((plan) => this.plansApi.seed(plan.id, 'empty')))
      .subscribe({
        next: (plan) => {
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          void this.router.navigate(['/plans', plan.id, 'setup']);
        },
        error: (err) => {
          this.message.set(err?.error?.detail ?? 'Plan oluşturulamadı.');
        },
      });
  }
}

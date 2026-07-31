import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PlanDto } from '../../core/models/api.models';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { formatDateTr } from '../../shared/utils/format';

@Component({
  selector: 'app-plan-list',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './plan-list.component.html',
  styleUrl: './plan-list.component.scss',
})
export class PlanListComponent implements OnInit {
  private readonly plansApi = inject(PlansApi);
  private readonly planContext = inject(PlanContextService);
  private readonly router = inject(Router);

  readonly plans = signal<PlanDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly creating = signal(false);

  newTitle = '';
  newDescription = '';

  formatDateTr = formatDateTr;

  ngOnInit(): void {
    this.planContext.clear();
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.plansApi.list().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail ?? 'Planlar yüklenemedi.');
      },
    });
  }

  create(): void {
    const title = this.newTitle.trim();
    if (!title) {
      return;
    }
    this.creating.set(true);
    this.plansApi.create({ title, description: this.newDescription.trim() }).subscribe({
      next: (plan) => {
        this.creating.set(false);
        this.newTitle = '';
        this.newDescription = '';
        void this.router.navigate(['/plans', plan.id, 'dashboard']);
      },
      error: (err) => {
        this.creating.set(false);
        this.error.set(err?.error?.detail ?? 'Plan oluşturulamadı.');
      },
    });
  }

  remove(plan: PlanDto, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (!confirm(`“${plan.title}” planını silmek istediğinize emin misiniz?`)) {
      return;
    }
    this.plansApi.delete(plan.id).subscribe({
      next: () => this.reload(),
      error: (err) => this.error.set(err?.error?.detail ?? 'Plan silinemedi.'),
    });
  }
}

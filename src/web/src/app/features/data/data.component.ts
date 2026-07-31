import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PlanExportDto } from '../../core/models/api.models';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';

@Component({
  selector: 'app-data',
  standalone: true,
  templateUrl: './data.component.html',
  styleUrl: './data.component.scss',
})
export class DataComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly plansApi = inject(PlansApi);
  private readonly planContext = inject(PlanContextService);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);

  private planId = '';

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    this.plansApi.get(this.planId).subscribe({
      next: (plan) => this.planContext.setPlan(plan.id, plan.title),
      error: () => undefined,
    });
  }

  exportJson(): void {
    this.busy.set(true);
    this.clearFlash();
    this.plansApi.export(this.planId).subscribe({
      next: (data) => {
        this.busy.set(false);
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `plan-${this.planId}.json`;
        a.click();
        URL.revokeObjectURL(url);
        this.message.set('JSON dışa aktarıldı.');
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err?.error?.detail ?? 'Dışa aktarma başarısız.');
      },
    });
  }

  onImportFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    if (file.size > 2_000_000) {
      this.error.set('Dosya çok büyük (maks. 2 MB).');
      input.value = '';
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    const reader = new FileReader();
    reader.onload = () => {
      try {
        const data = JSON.parse(String(reader.result)) as PlanExportDto;
        if (!data || typeof data.title !== 'string' || !Array.isArray(data.partners) || !Array.isArray(data.installments)) {
          throw new Error('Geçersiz şema');
        }
        this.plansApi.import(this.planId, data).subscribe({
          next: () => {
            this.busy.set(false);
            this.message.set('JSON içe aktarıldı.');
            input.value = '';
          },
          error: (err) => {
            this.busy.set(false);
            this.error.set(err?.error?.detail ?? 'İçe aktarma başarısız.');
            input.value = '';
          },
        });
      } catch {
        this.busy.set(false);
        this.error.set('Geçersiz JSON dosyası.');
        input.value = '';
      }
    };
    reader.onerror = () => {
      this.busy.set(false);
      this.error.set('Dosya okunamadı.');
      input.value = '';
    };
    reader.readAsText(file);
  }

  private clearFlash(): void {
    this.error.set(null);
    this.message.set(null);
  }
}

import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  PlanActivityItemDto,
  PlanExportDto,
  ReminderHistoryItemDto,
  ReportSummaryDto,
  SettlementBalanceDto,
} from '../../core/models/api.models';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { CsvInstallmentRow, downloadCsv, downloadIcs } from '../../shared/utils/export-files';
import { ToastService } from '../../shared/toast/toast.service';
import { formatDateTr } from '../../shared/utils/format';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-data',
  standalone: true,
  imports: [CurrencyTryPipe, DecimalPipe, DatePipe],
  templateUrl: './data.component.html',
  styleUrl: './data.component.scss',
})
export class DataComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plansApi = inject(PlansApi);
  private readonly http = inject(HttpClient);
  private readonly planContext = inject(PlanContextService);
  private readonly toast = inject(ToastService);

  readonly busy = signal(false);
  readonly partnerNames = signal<string[]>([]);
  readonly planTitle = signal('Plan');
  readonly planDescription = signal('');
  readonly report = signal<ReportSummaryDto | null>(null);
  readonly settlements = signal<SettlementBalanceDto[]>([]);
  readonly reminders = signal<ReminderHistoryItemDto[]>([]);
  readonly activity = signal<PlanActivityItemDto[]>([]);
  readonly isOwner = signal(false);
  readonly generatedAtLabel = signal('');

  private planId = '';
  private lastExport: PlanExportDto | null = null;

  readonly formatDateTr = formatDateTr;

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    this.refreshGeneratedAt();

    this.plansApi.get(this.planId).subscribe({
      next: (plan) => {
        this.planTitle.set(plan.title);
        this.planDescription.set(plan.description ?? '');
        this.planContext.setPlan(plan.id, plan.title, plan.description);
        this.loadExtras();
      },
      error: (err) => {
        const detail = err?.error?.detail ?? 'Plan yüklenemedi.';
        this.toast.error(detail);
        if (err?.status === 404 || /not found/i.test(String(detail))) {
          this.planContext.clear();
          void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
        }
      },
    });
    this.plansApi.dashboard(this.planId).subscribe({
      next: (d) => {
        this.isOwner.set(!!d.isOwner);
        this.settlements.set(d.settlements ?? []);
        if (!this.partnerNames().length && d.partners?.length) {
          this.partnerNames.set(d.partners.map((p) => p.name));
        }
      },
      error: () => undefined,
    });
  }

  private loadExtras(): void {
    this.plansApi.reportSummary(this.planId).subscribe({
      next: (r) => this.report.set(r),
      error: () => this.report.set(null),
    });
    this.plansApi.reminders(this.planId).subscribe({
      next: (r) => this.reminders.set(r),
      error: () => this.reminders.set([]),
    });
    this.plansApi.activity(this.planId).subscribe({
      next: (a) => this.activity.set(a),
      error: () => this.activity.set([]),
    });
  }

  private refreshGeneratedAt(): void {
    this.generatedAtLabel.set(
      new Date().toLocaleDateString('tr-TR', {
        day: 'numeric',
        month: 'long',
        year: 'numeric',
      })
    );
  }

  barPaidPct(paid: number, total: number): number {
    if (total <= 0) {
      return 0;
    }
    return Math.min(100, Math.round((paid / total) * 100));
  }

  formatYearMonth(ym: string): string {
    const [y, m] = ym.split('-').map((x) => Number(x));
    if (!y || !m) {
      return ym;
    }
    return new Date(y, m - 1, 1).toLocaleDateString('tr-TR', {
      month: 'long',
      year: 'numeric',
    });
  }

  processReminders(): void {
    this.busy.set(true);
    this.http.post(`${environment.apiUrl}/reminders/process`, {}).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('Hatırlatmalar işlendi.');
        this.loadExtras();
      },
      error: (err) => {
        this.busy.set(false);
        this.toast.error(err?.error?.detail ?? 'Hatırlatma çalıştırılamadı.');
      },
    });
  }

  exportJson(): void {
    this.busy.set(true);
    this.plansApi.export(this.planId).subscribe({
      next: (data) => {
        this.busy.set(false);
        this.lastExport = data;
        this.partnerNames.set(data.partners.map((p) => p.name));
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `plan-${this.planId}.json`;
        a.click();
        URL.revokeObjectURL(url);
        this.toast.success('JSON dışa aktarıldı.');
      },
      error: (err) => {
        this.busy.set(false);
        this.toast.error(err?.error?.detail ?? 'Dışa aktarma başarısız.');
      },
    });
  }

  exportCsv(): void {
    this.withExport((data) => {
      const partnerIds = data.partners.map((p) => p.id);
      const partnerNames = data.partners.map((p) => p.name);
      const nameById = new Map(data.partners.map((p) => [p.id, p.name]));
      const rows: CsvInstallmentRow[] = data.installments.map((inst) => ({
        id: inst.id,
        name: inst.name,
        dueDate: formatDateTr(inst.dueDate),
        totalAmount: inst.totalAmount,
        partners: partnerIds.map((pid) => {
          const pay = inst.payments?.find((x) => x.partnerId === pid);
          const share =
            inst.customShares?.find((c) => c.partnerId === pid)?.amount ??
            this.fallbackShare(inst.totalAmount, data.partners.length, pid, data);
          const paidById = pay?.paidByPartnerId ?? pid;
          return {
            name: nameById.get(pid) ?? pid,
            share,
            isPaid: !!pay?.isPaid,
            paidByName: nameById.get(paidById) ?? nameById.get(pid) ?? '',
            note: pay?.note ?? '',
          };
        }),
      }));
      downloadCsv(partnerNames, rows);
      this.toast.success('CSV indirildi.');
    });
  }

  exportIcs(): void {
    this.withExport((data) => {
      downloadIcs(
        data.title,
        data.installments.map((i) => ({
          name: i.name,
          dueDate: i.dueDate,
          totalAmount: i.totalAmount,
        }))
      );
      this.toast.success('Takvim (.ics) indirildi.');
    });
  }

  printReport(): void {
    this.busy.set(true);
    this.refreshGeneratedAt();
    forkJoin({
      report: this.plansApi.reportSummary(this.planId),
      exported: this.plansApi.export(this.planId),
      dash: this.plansApi.dashboard(this.planId),
    }).subscribe({
      next: ({ report, exported, dash }) => {
        this.busy.set(false);
        this.report.set(report);
        this.lastExport = exported;
        this.planTitle.set(exported.title || dash.title);
        this.planDescription.set(exported.description ?? '');
        this.partnerNames.set(exported.partners.map((p) => p.name));
        this.settlements.set(dash.settlements ?? []);
        setTimeout(() => window.print(), 80);
      },
      error: (err) => {
        this.busy.set(false);
        this.toast.error(err?.error?.detail ?? 'Rapor hazırlanamadı.');
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
      this.toast.error('Dosya çok büyük (maks. 2 MB).');
      input.value = '';
      return;
    }
    this.busy.set(true);
    const reader = new FileReader();
    reader.onload = () => {
      try {
        const data = JSON.parse(String(reader.result)) as PlanExportDto;
        if (
          !data ||
          typeof data.title !== 'string' ||
          !Array.isArray(data.partners) ||
          !Array.isArray(data.installments)
        ) {
          throw new Error('Geçersiz şema');
        }
        this.plansApi.import(this.planId, data).subscribe({
          next: () => {
            this.busy.set(false);
            this.toast.success('JSON içe aktarıldı.');
            input.value = '';
            this.loadExtras();
          },
          error: (err) => {
            this.busy.set(false);
            this.toast.error(err?.error?.detail ?? 'İçe aktarma başarısız.');
            input.value = '';
          },
        });
      } catch {
        this.busy.set(false);
        this.toast.error('Geçersiz JSON dosyası.');
        input.value = '';
      }
    };
    reader.onerror = () => {
      this.busy.set(false);
      this.toast.error('Dosya okunamadı.');
      input.value = '';
    };
    reader.readAsText(file);
  }

  private withExport(fn: (data: PlanExportDto) => void): void {
    if (this.lastExport) {
      fn(this.lastExport);
      return;
    }
    this.busy.set(true);
    this.plansApi.export(this.planId).subscribe({
      next: (data) => {
        this.busy.set(false);
        this.lastExport = data;
        this.partnerNames.set(data.partners.map((p) => p.name));
        fn(data);
      },
      error: (err) => {
        this.busy.set(false);
        this.toast.error(err?.error?.detail ?? 'Veri yüklenemedi.');
      },
    });
  }

  private fallbackShare(
    total: number,
    partnerCount: number,
    partnerId: string,
    data: PlanExportDto
  ): number {
    const partner = data.partners.find((p) => p.id === partnerId);
    if (partner && partner.defaultPct != null) {
      return Math.round(((total * Number(partner.defaultPct)) / 100) * 100) / 100;
    }
    if (partnerCount <= 0) {
      return 0;
    }
    return Math.round((total / partnerCount) * 100) / 100;
  }
}

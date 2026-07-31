import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  DashboardDto,
  DashboardInstallmentDto,
  PartnerPaymentStatusDto,
  PaymentRequest,
} from '../../core/models/api.models';
import { InstallmentsApi } from '../../core/services/installments.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import { formatDateTr, statusLabel } from '../../shared/utils/format';

type StatusFilter = 'all' | 'pending' | 'partial' | 'full';

interface PaymentDialogState {
  installment: DashboardInstallmentDto;
  payment: PartnerPaymentStatusDto;
  isPaid: boolean;
  paidAt: string;
  paidByPartnerId: string;
  note: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe, DecimalPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly plansApi = inject(PlansApi);
  private readonly installmentsApi = inject(InstallmentsApi);
  private readonly planContext = inject(PlanContextService);

  readonly dashboard = signal<DashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly filter = signal<StatusFilter>('all');
  readonly search = signal('');
  readonly dialog = signal<PaymentDialogState | null>(null);

  readonly formatDateTr = formatDateTr;
  readonly statusLabel = statusLabel;

  readonly filteredInstallments = computed(() => {
    const data = this.dashboard();
    if (!data) {
      return [];
    }
    const q = this.search().trim().toLowerCase();
    const f = this.filter();
    return data.installments.filter((inst) => {
      const status = String(inst.status);
      const matchFilter =
        f === 'all' ||
        (f === 'pending' && (status === 'Pending' || status === '0')) ||
        (f === 'partial' && (status === 'Partial' || status === '1')) ||
        (f === 'full' && (status === 'Full' || status === '2'));
      if (!matchFilter) {
        return false;
      }
      if (!q) {
        return true;
      }
      return (
        inst.name.toLowerCase().includes(q) ||
        inst.dueDate.includes(q) ||
        formatDateTr(inst.dueDate).includes(q)
      );
    });
  });

  private planId = '';

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.plansApi.dashboard(this.planId).subscribe({
      next: (dto) => {
        this.dashboard.set(dto);
        this.planContext.setPlan(dto.planId, dto.title);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail ?? 'Dashboard yüklenemedi.');
      },
    });
  }

  setFilter(f: StatusFilter): void {
    this.filter.set(f);
  }

  openPayment(installment: DashboardInstallmentDto, payment: PartnerPaymentStatusDto): void {
    this.dialog.set({
      installment,
      payment,
      isPaid: payment.isPaid,
      paidAt: payment.paidAt ?? new Date().toISOString().slice(0, 10),
      paidByPartnerId: payment.paidByPartnerId ?? payment.partnerId,
      note: payment.note ?? '',
    });
  }

  closeDialog(): void {
    this.dialog.set(null);
  }

  savePayment(): void {
    const d = this.dialog();
    if (!d) {
      return;
    }
    const body: PaymentRequest = {
      isPaid: d.isPaid,
      paidAt: d.isPaid ? d.paidAt || null : null,
      paidByPartnerId: d.isPaid ? d.paidByPartnerId || null : null,
      note: d.note?.trim() || null,
    };
    this.saving.set(true);
    this.installmentsApi
      .upsertPayment(this.planId, d.installment.id, d.payment.partnerId, body)
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.closeDialog();
          this.reload();
        },
        error: (err) => {
          this.saving.set(false);
          this.error.set(err?.error?.detail ?? 'Ödeme kaydedilemedi.');
        },
      });
  }

  countdownText(days: number | null): string {
    if (days == null) {
      return 'Teslimat taksiti seçilmedi';
    }
    if (days < 0) {
      return `Teslimat ${Math.abs(days)} gün geçti`;
    }
    if (days === 0) {
      return 'Teslimat bugün';
    }
    return `Teslimata ${days} gün`;
  }

  settlementText(balance: number): string {
    if (balance > 0) {
      return 'alacaklı';
    }
    if (balance < 0) {
      return 'borçlu';
    }
    return 'denk';
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  InstallmentDto,
  InstallmentRequest,
  PartnerDto,
  PartnerRequest,
  PlanDto,
} from '../../core/models/api.models';
import { InstallmentsApi } from '../../core/services/installments.api';
import { PartnersApi } from '../../core/services/partners.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import { formatDateTr, shareTypeLabel, shareTypeToNumber } from '../../shared/utils/format';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe],
  templateUrl: './setup.component.html',
  styleUrl: './setup.component.scss',
})
export class SetupComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly plansApi = inject(PlansApi);
  private readonly partnersApi = inject(PartnersApi);
  private readonly installmentsApi = inject(InstallmentsApi);
  private readonly planContext = inject(PlanContextService);

  readonly plan = signal<PlanDto | null>(null);
  readonly partners = signal<PartnerDto[]>([]);
  readonly installments = signal<InstallmentDto[]>([]);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);

  readonly formatDateTr = formatDateTr;
  readonly shareTypeLabel = shareTypeLabel;

  title = '';
  description = '';
  deliveryInstallmentId = '';

  partnerName = '';
  partnerColor = '#6366f1';
  partnerPct = 50;
  editingPartnerId: string | null = null;

  instName = '';
  instDueDate = '';
  instAmount = 0;
  instShareType = 0;
  editingInstallmentId: string | null = null;

  bulkFromId = '';
  bulkType = 0;
  bulkValue: number | null = null;

  private planId = '';

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.plansApi.get(this.planId).subscribe({
      next: (plan) => {
        this.plan.set(plan);
        this.title = plan.title;
        this.description = plan.description;
        this.deliveryInstallmentId = plan.deliveryInstallmentId ?? '';
        this.planContext.setPlan(plan.id, plan.title);
        this.partnersApi.list(this.planId).subscribe({
          next: (partners) => {
            this.partners.set(partners);
            this.installmentsApi.list(this.planId).subscribe({
              next: (installments) => {
                this.installments.set(installments);
                if (!this.bulkFromId && installments.length) {
                  this.bulkFromId = installments[0].id;
                }
                this.loading.set(false);
              },
              error: (err) => this.fail(err, 'Taksitler yüklenemedi.'),
            });
          },
          error: (err) => this.fail(err, 'Ortaklar yüklenemedi.'),
        });
      },
      error: (err) => this.fail(err, 'Plan yüklenemedi.'),
    });
  }

  savePlan(): void {
    this.busy.set(true);
    this.clearFlash();
    this.plansApi
      .update(this.planId, {
        title: this.title.trim(),
        description: this.description.trim(),
        deliveryInstallmentId: this.deliveryInstallmentId || null,
      })
      .subscribe({
        next: (plan) => {
          this.busy.set(false);
          this.plan.set(plan);
          this.planContext.setPlan(plan.id, plan.title);
          this.message.set('Plan kaydedildi.');
        },
        error: (err) => this.fail(err, 'Plan kaydedilemedi.'),
      });
  }

  seedFuzul(): void {
    if (!confirm('Fuzul şablonu mevcut ortak/taksit verisinin üzerine yazabilir. Devam?')) {
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    this.plansApi.seedFuzul(this.planId).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set('Fuzul şablonu yüklendi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Şablon yüklenemedi.'),
    });
  }

  startEditPartner(p: PartnerDto): void {
    this.editingPartnerId = p.id;
    this.partnerName = p.name;
    this.partnerColor = p.color;
    this.partnerPct = p.defaultPct;
  }

  resetPartnerForm(): void {
    this.editingPartnerId = null;
    this.partnerName = '';
    this.partnerColor = '#6366f1';
    this.partnerPct = 50;
  }

  savePartner(): void {
    const body: PartnerRequest = {
      name: this.partnerName.trim(),
      color: this.partnerColor,
      defaultPct: Number(this.partnerPct),
      sortOrder: this.editingPartnerId
        ? this.partners().find((x) => x.id === this.editingPartnerId)?.sortOrder ?? 0
        : this.partners().length,
    };
    if (!body.name) {
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    const req = this.editingPartnerId
      ? this.partnersApi.update(this.planId, this.editingPartnerId, body)
      : this.partnersApi.create(this.planId, body);
    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.resetPartnerForm();
        this.message.set('Ortak kaydedildi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Ortak kaydedilemedi.'),
    });
  }

  deletePartner(p: PartnerDto): void {
    if (!confirm(`“${p.name}” ortağını silmek istiyor musunuz?`)) {
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    this.partnersApi.delete(this.planId, p.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set('Ortak silindi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Ortak silinemedi.'),
    });
  }

  startEditInstallment(i: InstallmentDto): void {
    this.editingInstallmentId = i.id;
    this.instName = i.name;
    this.instDueDate = i.dueDate;
    this.instAmount = i.totalAmount;
    this.instShareType = shareTypeToNumber(i.shareType);
  }

  resetInstallmentForm(): void {
    this.editingInstallmentId = null;
    this.instName = '';
    this.instDueDate = '';
    this.instAmount = 0;
    this.instShareType = 0;
  }

  saveInstallment(): void {
    const body: InstallmentRequest = {
      name: this.instName.trim(),
      dueDate: this.instDueDate,
      totalAmount: Number(this.instAmount),
      shareType: Number(this.instShareType),
      sortOrder: this.editingInstallmentId
        ? this.installments().find((x) => x.id === this.editingInstallmentId)?.sortOrder ?? 0
        : this.installments().length,
      customShares: null,
    };
    if (!body.name || !body.dueDate) {
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    const req = this.editingInstallmentId
      ? this.installmentsApi.update(this.planId, this.editingInstallmentId, body)
      : this.installmentsApi.create(this.planId, body);
    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.resetInstallmentForm();
        this.message.set('Taksit kaydedildi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Taksit kaydedilemedi.'),
    });
  }

  deleteInstallment(i: InstallmentDto): void {
    if (!confirm(`“${i.name}” taksitini silmek istiyor musunuz?`)) {
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    this.installmentsApi.delete(this.planId, i.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set('Taksit silindi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Taksit silinemedi.'),
    });
  }

  runBulkIncrease(): void {
    if (!this.bulkFromId || this.bulkValue == null || Number.isNaN(Number(this.bulkValue))) {
      this.error.set('Toplu artış için başlangıç taksiti ve değer gerekli.');
      return;
    }
    this.busy.set(true);
    this.clearFlash();
    this.installmentsApi
      .bulkIncrease(this.planId, {
        fromInstallmentId: this.bulkFromId,
        type: Number(this.bulkType),
        value: Number(this.bulkValue),
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.message.set('Toplu artış uygulandı.');
          this.reload();
        },
        error: (err) => this.fail(err, 'Toplu artış başarısız.'),
      });
  }

  private fail(err: { error?: { detail?: string } }, fallback: string): void {
    this.busy.set(false);
    this.loading.set(false);
    this.error.set(err?.error?.detail ?? fallback);
  }

  private clearFlash(): void {
    this.error.set(null);
    this.message.set(null);
  }
}

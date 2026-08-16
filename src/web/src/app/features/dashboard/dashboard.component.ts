import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  DashboardDto,
  DashboardInstallmentDto,
  PartnerPaymentStatusDto,
  PartnerSummaryDto,
  PaymentRequest,
} from '../../core/models/api.models';
import { InstallmentsApi } from '../../core/services/installments.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { isExpensePlan } from '../../core/utils/plan-routes';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import { downloadIcs } from '../../shared/utils/export-files';
import { ToastService } from '../../shared/toast/toast.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { MoneyInputDirective } from '../../shared/directives/money-input.directive';
import { formatDateTr, formatTry, shareTypeToNumber } from '../../shared/utils/format';

type StatusFilter = 'all' | 'pending' | 'partial' | 'full';
type PartnerViewFilter = 'all' | 'mine' | string;

interface PaymentDialogState {
  installment: DashboardInstallmentDto;
  payment: PartnerPaymentStatusDto;
  isPaid: boolean;
  paidAt: string;
  paidByPartnerId: string;
  note: string;
  receiptFile: File | null;
  hasReceipt: boolean;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe, DecimalPipe, RouterLink, MoneyInputDirective],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plansApi = inject(PlansApi);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly installmentsApi = inject(InstallmentsApi);
  private readonly planContext = inject(PlanContextService);

  readonly dashboard = signal<DashboardDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly filter = signal<StatusFilter>('all');
  readonly partnerView = signal<PartnerViewFilter>('all');
  readonly search = signal('');
  readonly dialog = signal<PaymentDialogState | null>(null);
  readonly showBulkModal = signal(false);
  readonly showEditModal = signal(false);

  bulkFromId = '';
  bulkType = 0;
  bulkValue: number | null = null;

  editingInstId: string | null = null;
  editName = '';
  editDueDate = '';
  editAmount = 0;
  editShareType = 0;
  editCustomShares: Record<string, number> = {};
  editSortOrder = 0;

  readonly formatDateTr = formatDateTr;
  readonly formatTry = formatTry;

  readonly filteredInstallments = computed(() => {
    const data = this.dashboard();
    if (!data) {
      return [];
    }
    const q = this.search().trim().toLowerCase();
    const f = this.filter();
    const pv = this.partnerView();
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
      if (pv === 'mine') {
        const mid = data.myPartnerId;
        if (!mid) {
          return false;
        }
        const mine = inst.partnerPayments.find((p) => p.partnerId === mid);
        if (!mine || mine.isPaid) {
          // still show installment but we'll dim others in UI — keep all for context when filtering unpaid mine
        }
      } else if (pv !== 'all') {
        const pay = inst.partnerPayments.find((p) => p.partnerId === pv);
        if (!pay || pay.isPaid) {
          // show all rows for partner column focus
        }
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

  readonly pendingApprovals = computed(() => {
    const data = this.dashboard();
    if (!data) {
      return [] as { installment: DashboardInstallmentDto; payment: PartnerPaymentStatusDto }[];
    }
    const rows: { installment: DashboardInstallmentDto; payment: PartnerPaymentStatusDto }[] = [];
    for (const inst of data.installments) {
      for (const pay of inst.partnerPayments) {
        if (this.isPendingReview(pay)) {
          rows.push({ installment: inst, payment: pay });
        }
      }
    }
    return rows;
  });

  private planId = '';

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    this.plansApi.get(this.planId).subscribe({
      next: (plan) => {
        if (isExpensePlan(plan)) {
          void this.router.navigate(['/plans', this.planId, 'expenses']);
          return;
        }
        this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
        this.reload();
      },
      error: () => this.reload(),
    });
  }

  reload(): void {
    this.loading.set(true);
        this.plansApi.dashboard(this.planId).subscribe({
      next: (dto) => {
        this.dashboard.set(dto);
        this.planContext.setPlan(dto.planId, dto.title, dto.description, 'Installment');
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        const detail = err?.error?.detail ?? 'Dashboard yüklenemedi.';
        this.toast.error(detail);
        if (err?.status === 404 || /not found/i.test(String(detail))) {
          this.planContext.clear();
          void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
        }
      },
    });
  }

  setFilter(f: StatusFilter): void {
    this.filter.set(f);
  }

  setPartnerView(v: PartnerViewFilter): void {
    this.partnerView.set(v);
  }

  isPendingReview(pay: PartnerPaymentStatusDto): boolean {
    const s = String(pay.reviewStatus ?? 'None');
    return s === 'Pending' || s === '1';
  }

  isPayColumnDimmed(partnerId: string): boolean {
    const pv = this.partnerView();
    const d = this.dashboard();
    if (pv === 'all' || !d) {
      return false;
    }
    if (pv === 'mine') {
      return d.myPartnerId !== partnerId;
    }
    return pv !== partnerId;
  }

  approvePayment(inst: DashboardInstallmentDto, pay: PartnerPaymentStatusDto): void {
    this.saving.set(true);
    this.installmentsApi.approvePayment(this.planId, inst.id, pay.partnerId).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Ödeme onaylandı.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.detail ?? 'Onay başarısız.');
      },
    });
  }

  rejectPayment(inst: DashboardInstallmentDto, pay: PartnerPaymentStatusDto): void {
    this.saving.set(true);
    this.installmentsApi.rejectPayment(this.planId, inst.id, pay.partnerId).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Ödeme reddedildi.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.detail ?? 'Red başarısız.');
      },
    });
  }

  completedCount(): number {
    const d = this.dashboard();
    if (!d) {
      return 0;
    }
    return d.installments.filter((i) => this.isFull(i.status)).length;
  }

  isFull(status: string | number): boolean {
    const s = String(status);
    return s === 'Full' || s === '2';
  }

  isPartial(status: string | number): boolean {
    const s = String(status);
    return s === 'Partial' || s === '1';
  }

  paidCount(inst: DashboardInstallmentDto): number {
    return inst.partnerPayments.filter((p) => p.isPaid).length;
  }

  statusBadgeText(inst: DashboardInstallmentDto): string {
    const total = inst.partnerPayments.length;
    const paid = this.paidCount(inst);
    if (this.isFull(inst.status)) {
      return '✅ Tamamlandı';
    }
    if (this.isPartial(inst.status)) {
      return `⚡ Kısmi (${paid}/${total})`;
    }
    return '⏳ Bekliyor';
  }

  statusClass(status: string | number): string {
    const s = String(status);
    if (s === 'Full' || s === '2') {
      return 'full';
    }
    if (s === 'Partial' || s === '1') {
      return 'partial';
    }
    return 'pending';
  }

  deliveryInstallment(): DashboardInstallmentDto | null {
    const d = this.dashboard();
    if (!d?.deliveryInstallmentId) {
      return null;
    }
    return d.installments.find((i) => i.id === d.deliveryInstallmentId) ?? null;
  }

  deliveryBadge(): string {
    return this.deliveryInstallment()?.name ?? 'Seçilmedi';
  }

  countdownText(): string {
    const d = this.dashboard();
    if (!d?.deliveryInstallmentId) {
      return 'Opsiyonel — seçilmedi';
    }
    const days = d.daysUntilDelivery;
    if (days == null) {
      return '-- Ay -- Gün';
    }
    if (days <= 0) {
      return 'Tahsisat Ayı Geldi!';
    }
    const months = Math.floor(days / 30);
    const rem = days % 30;
    if (months > 0) {
      return `${months} Ay ${rem} Gün`;
    }
    return `${rem} Gün`;
  }

  /** HTML: Debtor ➔ Creditor or dengede */
  settlementHeadline(): string {
    const pair = this.settlementPair();
    if (!pair) {
      return 'Hesaplar Dengede (0 ₺)';
    }
    return `${pair.debtor} ➔ ${pair.creditor}`;
  }

  settlementSubText(): string {
    const pair = this.settlementPair();
    if (!pair) {
      return 'Ortaklar birbirine borçlu değil.';
    }
    return `Borç Tutarı: ${formatTry(pair.amount)}`;
  }

  settlementHasDebt(): boolean {
    return !!this.settlementPair();
  }

  settlementPairAmount(): number {
    return this.settlementPair()?.amount ?? 0;
  }

  private settlementPair(): { debtor: string; creditor: string; amount: number } | null {
    const d = this.dashboard();
    if (!d?.settlements.length) {
      return null;
    }
    const debtor = d.settlements.find((s) => s.balance < -1);
    const creditor = d.settlements.find((s) => s.balance > 1);
    if (!debtor || !creditor) {
      return null;
    }
    return {
      debtor: debtor.partnerName,
      creditor: creditor.partnerName,
      amount: Math.abs(debtor.balance),
    };
  }

  /** HTML window: -5..30 days from today for incomplete installments */
  upcomingUrgent(): { inst: DashboardInstallmentDto; days: number } | null {
    const d = this.dashboard();
    if (!d) {
      return null;
    }
    const today = this.startOfToday();
    let best: { inst: DashboardInstallmentDto; days: number } | null = null;
    for (const inst of d.installments) {
      if (this.isFull(inst.status)) {
        continue;
      }
      const days = this.daysFromToday(inst.dueDate, today);
      if (days >= -5 && days <= 30) {
        if (!best || days < best.days) {
          best = { inst, days };
        }
      }
    }
    return best;
  }

  upcomingCount(): number {
    const d = this.dashboard();
    if (!d) {
      return 0;
    }
    const today = this.startOfToday();
    return d.installments.filter((inst) => {
      if (this.isFull(inst.status)) {
        return false;
      }
      const days = this.daysFromToday(inst.dueDate, today);
      return days >= -5 && days <= 30;
    }).length;
  }

  upcomingTitle(): string {
    return this.upcomingUrgent()?.inst.name ?? 'Yaklaşan Ödeme Yok';
  }

  upcomingSub(): string {
    const u = this.upcomingUrgent();
    if (!u) {
      return 'Önümüzdeki 30 gün içinde ödemesi yaklaşan taksit bulunmuyor.';
    }
    return `Vade: ${formatDateTr(u.inst.dueDate)} (${u.days} gün kaldı) • ${formatTry(u.inst.totalAmount)}`;
  }

  partnerPct(p: PartnerSummaryDto): string {
    if (p.totalShare <= 0) {
      return '0.0';
    }
    return ((p.paidAmount / p.totalShare) * 100).toFixed(1);
  }

  isUrgent(inst: DashboardInstallmentDto): boolean {
    if (this.isFull(inst.status)) {
      return false;
    }
    const days = this.daysFromToday(inst.dueDate, this.startOfToday());
    return days >= 0 && days <= 7;
  }

  /** Due month is after the current calendar month. */
  isFutureDueMonth(inst: DashboardInstallmentDto): boolean {
    const [y, m] = inst.dueDate.split('-').map((x) => Number(x));
    if (!y || !m) {
      return false;
    }
    const now = new Date();
    return y > now.getFullYear() || (y === now.getFullYear() && m > now.getMonth() + 1);
  }

  canOpenPayment(inst: DashboardInstallmentDto, pay: PartnerPaymentStatusDto): boolean {
    if (!this.canMark(pay.partnerId)) {
      return false;
    }
    // Future months: only allow opening if already paid (to unmark).
    if (this.isFutureDueMonth(inst) && !pay.isPaid) {
      return false;
    }
    return true;
  }

  paymentCellTitle(inst: DashboardInstallmentDto, pay: PartnerPaymentStatusDto): string {
    if (this.isFutureDueMonth(inst) && !pay.isPaid) {
      return 'İleri aylara ait taksitler için ödeme işaretlenemez';
    }
    if (!this.canMark(pay.partnerId)) {
      return 'Sadece kendi payınızı işaretleyebilirsiniz';
    }
    return `${pay.partnerName} — ${formatTry(pay.shareAmount)}`;
  }

  rowClass(inst: DashboardInstallmentDto): string {
    if (this.isFull(inst.status)) {
      return 'paid-row';
    }
    if (this.isUrgent(inst)) {
      return 'upcoming-urgent';
    }
    return '';
  }

  sharePct(pay: PartnerPaymentStatusDto, total: number): number {
    if (total <= 0) {
      return 0;
    }
    return Math.round((pay.shareAmount / total) * 100);
  }

  partnerColor(partnerId: string): string {
    return this.dashboard()?.partners.find((p) => p.partnerId === partnerId)?.color ?? 'var(--primary)';
  }

  paymentNote(pay: PartnerPaymentStatusDto): string {
    if (pay.note) {
      return `📝 ${pay.note}`;
    }
    if (pay.isPaid && pay.paidByPartnerId && pay.paidByPartnerId !== pay.partnerId) {
      const name = this.dashboard()?.partners.find((p) => p.partnerId === pay.paidByPartnerId)?.name;
      if (name) {
        return `👤 ${name} Ödedi`;
      }
    }
    return '';
  }

  canMark(partnerId: string): boolean {
    const d = this.dashboard();
    if (!d) {
      return false;
    }
    // Owner can always mark every partner row.
    if (d.isOwner === true) {
      return true;
    }
    const mine = d.myPartnerId;
    return !!mine && mine === partnerId;
  }

  openPayment(installment: DashboardInstallmentDto, payment: PartnerPaymentStatusDto): void {
    const d = this.dashboard();
    if (this.isFutureDueMonth(installment) && !payment.isPaid) {
      this.toast.error('İleri aylara ait taksitler için ödeme işaretlenemez.');
      return;
    }
    if (d?.isOwner === true || this.canMark(payment.partnerId)) {
      this.dialog.set({
        installment,
        payment,
        isPaid: true,
        paidAt: payment.paidAt ?? new Date().toISOString().slice(0, 10),
        paidByPartnerId: payment.paidByPartnerId ?? payment.partnerId,
        note: payment.note ?? '',
        receiptFile: null,
        hasReceipt: !!payment.hasReceipt,
      });
      return;
    }
    this.toast.error('Sadece kendi ortağınızın ödemesini işaretleyebilirsiniz.');
  }

  closeDialog(): void {
    this.dialog.set(null);
  }

  onReceiptSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    const d = this.dialog();
    if (!d) {
      return;
    }
    this.dialog.set({ ...d, receiptFile: file });
  }

  savePayment(): void {
    const d = this.dialog();
    if (!d) {
      return;
    }
    const dash = this.dashboard();
    if (d.isPaid && this.isFutureDueMonth(d.installment)) {
      this.toast.error('İleri aylara ait taksitler için ödeme işaretlenemez.');
      return;
    }
    if (d.isPaid && dash?.requireReceipt && !d.hasReceipt && !d.receiptFile) {
      this.toast.error('Dekont zorunlu. Lütfen dosya yükleyin.');
      return;
    }

    const body: PaymentRequest = {
      isPaid: d.isPaid,
      paidAt: d.isPaid ? d.paidAt || null : null,
      paidByPartnerId: d.isPaid ? d.paidByPartnerId || null : null,
      note: d.note?.trim() || null,
    };

    this.saving.set(true);
    
    const afterUpload = () => {
      this.installmentsApi
        .upsertPayment(this.planId, d.installment.id, d.payment.partnerId, body)
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            this.closeDialog();
            const pending = String(res.reviewStatus ?? '') === 'Pending' || String(res.reviewStatus) === '1';
            this.toast.success(
              pending ? 'Ödeme onay için gönderildi.' : 'Ödeme kaydedildi.'
            );
            this.reload();
          },
          error: (err) => {
            this.saving.set(false);
            this.toast.error(err?.error?.detail ?? 'Ödeme kaydedilemedi.');
          },
        });
    };

    if (d.isPaid && d.receiptFile) {
      this.installmentsApi
        .uploadReceipt(this.planId, d.installment.id, d.payment.partnerId, d.receiptFile)
        .subscribe({
          next: () => afterUpload(),
          error: (err) => {
            this.saving.set(false);
            this.toast.error(err?.error?.detail ?? 'Dekont yüklenemedi.');
          },
        });
      return;
    }

    afterUpload();
  }

  exportIcs(): void {
    const d = this.dashboard();
    if (!d) {
      return;
    }
    downloadIcs(
      d.title,
      d.installments.map((i) => ({
        name: i.name,
        dueDate: i.dueDate,
        totalAmount: i.totalAmount,
      }))
    );
  }

  async settleUp(): Promise<void> {
    if (!this.settlementHasDebt()) {
      this.toast.info('Ortaklar arası mahsup bakiyesi zaten sıfır. Kapatılacak iç borç yok.');
      return;
    }
    if (
      !(await this.confirm.ask({
        title: 'Hesabı kapat',
        message:
          'Ortaklar arasındaki iç mahsuplaşma bakiye kaydı sıfırlanacak (tüm ödemeler sahiplerince yapılmış olarak işaretlenecek). Emin misiniz?',
        confirmLabel: 'Evet, kapat',
        danger: true,
      }))
    ) {
      return;
    }
    this.saving.set(true);
    this.plansApi.settleUp(this.planId).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('İç hesaplaşma dengelendi.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.detail ?? 'Hesap kapatılamadı.');
      },
    });
  }

  openBulkModal(): void {
    const d = this.dashboard();
    if (!d?.isOwner) {
      this.toast.error('Bu değişikliği yalnızca planı kuran kişi yapabilir.');
      return;
    }
    if (!d?.installments.length) {
      this.toast.error('Artış için önce taksit ekleyin.');
      return;
    }
    this.bulkFromId = d.installments[0].id;
    this.bulkType = 0;
    this.bulkValue = null;
    this.showBulkModal.set(true);
  }

  closeBulkModal(): void {
    this.showBulkModal.set(false);
  }

  runBulkIncrease(): void {
    if (!this.bulkFromId || this.bulkValue == null || Number.isNaN(Number(this.bulkValue))) {
      this.toast.error('Toplu artış için başlangıç taksiti ve değer gerekli.');
      return;
    }
    this.saving.set(true);
        this.installmentsApi
      .bulkIncrease(this.planId, {
        fromInstallmentId: this.bulkFromId,
        type: Number(this.bulkType),
        value: Number(this.bulkValue),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.closeBulkModal();
          this.reload();
        },
        error: (err) => {
          this.saving.set(false);
          this.toast.error(err?.error?.detail ?? 'Toplu artış başarısız.');
        },
      });
  }

  goAddInstallment(): void {
    if (!this.dashboard()?.isOwner) {
      this.toast.error('Bu değişikliği yalnızca planı kuran kişi yapabilir.');
      return;
    }
    void this.router.navigate(['/plans', this.planId, 'setup'], { queryParams: { add: '1' } });
  }

  goEditInstallment(inst: DashboardInstallmentDto): void {
    if (!this.dashboard()?.isOwner) {
      this.toast.error('Bu değişikliği yalnızca planı kuran kişi yapabilir.');
      return;
    }
    this.editingInstId = inst.id;
    this.editName = inst.name;
    this.editDueDate = (inst.dueDate ?? '').slice(0, 10);
    this.editAmount = inst.totalAmount;
    this.editShareType = shareTypeToNumber(inst.shareType);
    this.editSortOrder = inst.sortOrder;
    this.editCustomShares = {};
    for (const p of inst.partnerPayments) {
      this.editCustomShares[p.partnerId] = p.shareAmount;
    }
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
    this.editingInstId = null;
  }

  onEditShareTypeOrAmountChange(): void {
    if (this.editShareType !== 2) {
      return;
    }
    const partners = this.dashboard()?.partners ?? [];
    if (!partners.length) {
      return;
    }
    const equal = Math.round((Number(this.editAmount) / partners.length) * 100) / 100;
    const next: Record<string, number> = { ...this.editCustomShares };
    for (const p of partners) {
      if (next[p.partnerId] == null || Number.isNaN(Number(next[p.partnerId]))) {
        next[p.partnerId] = equal;
      }
    }
    this.editCustomShares = next;
  }

  editCustomShareSum(): number {
    return Object.values(this.editCustomShares).reduce((s, v) => s + (Number(v) || 0), 0);
  }

  editCustomSharesMatch(): boolean {
    return Math.abs(this.editCustomShareSum() - Number(this.editAmount)) < 0.02;
  }

  saveEditedInstallment(): void {
    if (!this.dashboard()?.isOwner) {
      this.toast.error('Bu değişikliği yalnızca planı kuran kişi yapabilir.');
      return;
    }
    if (!this.editingInstId) {
      return;
    }
    const name = this.editName.trim();
    if (!name) {
      this.toast.error('Taksit adı gerekli.');
      return;
    }
    if (!this.editDueDate) {
      this.toast.error('Vade tarihi gerekli.');
      return;
    }
    if (!(Number(this.editAmount) > 0)) {
      this.toast.error('Tutar sıfırdan büyük olmalı.');
      return;
    }
    if (this.editShareType === 2 && !this.editCustomSharesMatch()) {
      this.toast.error('Özel payların toplamı taksit tutarına eşit olmalı.');
      return;
    }

    const body = {
      name,
      dueDate: this.editDueDate.slice(0, 10),
      totalAmount: Number(this.editAmount),
      shareType: this.editShareType,
      sortOrder: this.editSortOrder,
      customShares:
        this.editShareType === 2
          ? Object.entries(this.editCustomShares).map(([partnerId, amount]) => ({
              partnerId,
              amount: Number(amount) || 0,
            }))
          : null,
    };

    this.saving.set(true);
    this.installmentsApi.update(this.planId, this.editingInstId, body).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEditModal();
        this.toast.success('Taksit güncellendi.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(err?.error?.detail ?? 'Taksit güncellenemedi.');
      },
    });
  }

  openDatePicker(event: Event): void {
    const input = event.target as HTMLInputElement;
    try {
      input.showPicker?.();
    } catch {
      // native control still works
    }
  }

  private startOfToday(): Date {
    const t = new Date();
    t.setHours(0, 0, 0, 0);
    return t;
  }

  private daysFromToday(isoDate: string, today: Date): number {
    const d = new Date(`${isoDate.slice(0, 10)}T00:00:00`);
    return Math.ceil((d.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  }
}

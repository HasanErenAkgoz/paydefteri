import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ExpenseBoardDto,
  ExpenseDto,
  ExpenseReceiptDraftDto,
  ExpenseRequest,
  PartnerDto,
  SettlementTransferRequest,
} from '../../core/models/api.models';
import { ExpensesApi } from '../../core/services/expenses.api';
import { PartnersApi } from '../../core/services/partners.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { ToastService } from '../../shared/toast/toast.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { isExpensePlan } from '../../core/utils/plan-routes';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { ExpenseOverviewComponent } from './expense-overview/expense-overview.component';
import { ExpenseTransferPanelComponent } from './expense-transfer-panel/expense-transfer-panel.component';
import { ExpenseListControlsComponent } from './expense-list-controls/expense-list-controls.component';
import { MarkExpensePaidEvent } from './expense-row-actions/expense-row-actions.component';
import { ExpenseAddFormComponent } from './expense-form/expense-add-form.component';
import {
  ExpenseEditModalComponent,
  ExpenseEditSaveEvent,
} from './expense-form/expense-edit-modal.component';
import { ExpensePartnerOption } from './expense-partner-option';
import { ExpenseListComponent } from './expense-list/expense-list.component';

import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';

type ExpenseFilter = 'all' | 'paid' | 'planned';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [
    RouterLink,
    ExpenseTransferPanelComponent,
    ExpenseListControlsComponent,
    ExpenseListComponent,
    ExpenseAddFormComponent,
    ExpenseEditModalComponent,
    CurrencyTryPipe,
  ],
  templateUrl: './expenses.component.html',
  styleUrl: './expenses.component.scss',
})
export class ExpensesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly expensesApi = inject(ExpensesApi);
  private readonly partnersApi = inject(PartnersApi);
  private readonly plansApi = inject(PlansApi);
  private readonly planContext = inject(PlanContextService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly board = signal<ExpenseBoardDto | null>(null);
  readonly pagedExpenses = signal<ExpenseDto[]>([]);
  readonly expensePage = signal(1);
  readonly expenseTotalCount = signal(0);
  readonly expensePageSize = 50;
  readonly partners = signal<PartnerDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly markingId = signal<string | null>(null);
  readonly filter = signal<ExpenseFilter>('all');
  readonly showAddForm = signal(false);
  readonly activeTab = signal<'list' | 'summary' | 'transfers'>('list');
  readonly editingExpense = signal<ExpenseDto | null>(null);
  readonly analyzingReceipt = signal(false);
  readonly receiptDraft = signal<ExpenseReceiptDraftDto | null>(null);
  readonly isOwner = computed(() => !!this.board()?.isOwner);
  readonly partnerOptions = computed<ExpensePartnerOption[]>(() => {
    const partners = this.partners();
    if (partners.length) {
      return partners.map((partner) => ({ id: partner.id, name: partner.name, color: partner.color }));
    }
    return (this.board()?.balances ?? []).map((balance) => ({
      id: balance.partnerId,
      name: balance.partnerName,
      color: balance.color,
    }));
  });

  canManageExpense(expense: ExpenseDto): boolean {
    return expense.canManage;
  }

  private ensureOwnerAction(): boolean {
    if (this.isOwner()) {
      return true;
    }
    this.toast.error('Bu değişikliği yalnızca planı kuran kişi yapabilir.');
    return false;
  }

  fromPartnerId = '';
  toPartnerId = '';
  transferAmount: number | null = null;

  planId = '';

  readonly sortedExpenses = computed(() => {
    const list = [...this.pagedExpenses()];
    list.sort((a, b) => String(b.occurredOn).localeCompare(String(a.occurredOn)));
    const f = this.filter();
    if (f === 'paid') {
      return list.filter((e) => this.isPaid(e));
    }
    if (f === 'planned') {
      return list.filter((e) => !this.isPaid(e));
    }
    return list;
  });

  readonly paidTotal = computed(() =>
    (this.board()?.expenses ?? [])
      .filter((e) => this.isPaid(e))
      .reduce((sum, e) => sum + Number(e.totalAmount || 0), 0)
  );

  readonly plannedTotal = computed(() =>
    (this.board()?.expenses ?? [])
      .filter((e) => !this.isPaid(e))
      .reduce((sum, e) => sum + Number(e.totalAmount || 0), 0)
  );

  readonly paidCount = computed(
    () => (this.board()?.expenses ?? []).filter((e) => this.isPaid(e)).length
  );

  readonly plannedCount = computed(
    () => (this.board()?.expenses ?? []).filter((e) => !this.isPaid(e)).length
  );

  /** True when someone owes someone else — transfer is the natural next step. */
  readonly hasOpenBalance = computed(() =>
    (this.board()?.balances ?? []).some((b) => Math.abs(Number(b.balance) || 0) > 0.005)
  );

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.planId) {
      void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
      return;
    }
    this.plansApi.get(this.planId).subscribe({
      next: (plan) => {
        if (!isExpensePlan(plan)) {
          void this.router.navigate(['/plans', this.planId]);
          return;
        }
        this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
        this.loadPartners();
        this.reload();
      },
      error: () => {
        void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
      },
    });
  }

  loadPartners(): void {
    this.partnersApi.list(this.planId).subscribe({
      next: (partners) => {
        this.partners.set(partners);
        this.initTransferPartners(partners);
      },
      error: (err) => {
        this.toast.error(apiErrorMessage(err, 'Ortaklar yüklenemedi.'));
      },
    });
  }

  reload(): void {
    this.loading.set(true);
    this.expensesApi.board(this.planId).subscribe({
      next: (board) => {
        this.board.set(board);
        this.loadExpensePage(1);
        if (!this.partners().length) {
          this.initTransferPartners(board.balances.map((b) => ({ id: b.partnerId, name: b.partnerName })));
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, 'Gider tablosu yüklenemedi.'));
      },
    });
  }

  loadExpensePage(page: number): void {
    const p = Math.max(1, page);
    this.expensesApi.list(this.planId, p, this.expensePageSize).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.pagedExpenses.set(res.items);
        this.expensePage.set(res.page);
        this.expenseTotalCount.set(res.totalCount);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, 'Gider listesi yüklenemedi.'));
      },
    });
  }

  get expensePageCount(): number {
    const total = this.expenseTotalCount();
    if (total <= 0) return 1;
    return Math.max(1, Math.ceil(total / this.expensePageSize));
  }

  changeExpensePage(newPage: number): void {
    if (newPage < 1 || newPage > this.expensePageCount || newPage === this.expensePage()) {
      return;
    }
    this.loadExpensePage(newPage);
  }

  private initTransferPartners(partners: { id: string; name: string }[]): void {
    if (partners.length < 2) {
      return;
    }
    const balances = this.board()?.balances ?? [];
    const debtor = balances.find((b) => b.balance < -0.005);
    const creditor = balances.find((b) => b.balance > 0.005);
    if (debtor && creditor) {
      this.fromPartnerId = debtor.partnerId;
      this.toPartnerId = creditor.partnerId;
      this.transferAmount = Math.min(Math.abs(debtor.balance), creditor.balance);
      return;
    }
    const opts = partners;
    if (opts.length >= 2) {
      this.fromPartnerId = opts[0].id;
      this.toPartnerId = opts[1].id;
    } else if (opts.length === 1) {
      this.fromPartnerId = opts[0].id;
      this.toPartnerId = opts[0].id;
    }
  }

  isPaid(e: ExpenseDto): boolean {
    return e.status === 'Paid' || e.status === 1;
  }

  partnerName(id: string | null | undefined): string {
    if (!id) {
      return '—';
    }
    return (
      this.partners().find((p) => p.id === id)?.name ??
      this.board()?.balances.find((b) => b.partnerId === id)?.partnerName ??
      '—'
    );
  }

  private formatTry(amount: number): string {
    return (
      new Intl.NumberFormat('tr-TR', {
        style: 'currency',
        currency: 'TRY',
        maximumFractionDigits: 2,
      }).format(Number(amount) || 0)
    );
  }

  balanceHint(balance: number): string {
    if (Math.abs(balance) < 0.005) {
      return 'Dengede';
    }
    return balance > 0 ? 'Alacaklı' : 'Borçlu';
  }

  setTab(tab: 'list' | 'summary' | 'transfers'): void {
    this.activeTab.set(tab);
  }

  setFilter(f: ExpenseFilter): void {
    this.filter.set(f);
  }

  openAddModal(): void {
    this.showAddForm.set(true);
  }

  toggleAddForm(): void {
    this.showAddForm.set(!this.showAddForm());
    if (!this.showAddForm()) {
      this.receiptDraft.set(null);
    }
  }

  closeAddForm(): void {
    this.showAddForm.set(false);
    this.receiptDraft.set(null);
    this.activeTab.set('list');
  }

  showAddFormError(message: string): void {
    this.toast.error(message);
  }

  analyzeReceipt(file: File): void {
    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.toast.error('Yalnızca JPEG, PNG veya WebP görseller desteklenir.');
      return;
    }
    if (file.size > 8 * 1024 * 1024) {
      this.toast.error('Görsel en fazla 8 MB olabilir.');
      return;
    }

    this.analyzingReceipt.set(true);
    this.expensesApi.analyzeReceipt(this.planId, file).subscribe({
      next: (draft) => {
        this.analyzingReceipt.set(false);
        this.receiptDraft.set(draft);
        this.toast.success('Fiş bilgileri forma aktarıldı; kaydetmeden önce kontrol edin.');
      },
      error: (err) => {
        this.analyzingReceipt.set(false);
        this.toast.error(apiErrorMessage(err, 'Fiş analiz edilemedi.'));
      },
    });
  }

  addExpense(body: ExpenseRequest): void {
    const installmentCount = Number(body.installmentCount ?? 1);
    this.saving.set(true);
    this.expensesApi.create(this.planId, body).subscribe({
      next: () => {
        this.saving.set(false);
        this.showAddForm.set(false);
        this.receiptDraft.set(null);
        this.activeTab.set('list');
        this.toast.success(installmentCount > 1 ? `${installmentCount} taksit eklendi.` : 'Gider eklendi.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, 'Gider eklenemedi.'));
      },
    });
  }

  addTransfer(request: SettlementTransferRequest): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const amount = Number(request.amount);
    const fromPartnerId = request.fromPartnerId;
    const toPartnerId = request.toPartnerId;
    if (!(amount > 0) || !fromPartnerId || !toPartnerId) {
      this.toast.error('Transfer bilgilerini kontrol edin.');
      return;
    }
    if (fromPartnerId === toPartnerId) {
      this.toast.error('Kimden ve kime farklı olmalı.');
      return;
    }
    const body: SettlementTransferRequest = {
      fromPartnerId,
      toPartnerId,
      amount,
      transferredOn: request.transferredOn,
    };
    this.saving.set(true);
    this.expensesApi.createTransfer(this.planId, body).subscribe({
      next: () => {
        this.saving.set(false);
        this.transferAmount = null;
        this.toast.success('Transfer kaydedildi.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, 'Transfer eklenemedi.'));
      },
    });
  }

  async removeTransfer(id: string): Promise<void> {
    if (!this.ensureOwnerAction()) {
      return;
    }
    if (
      !(await this.confirm.ask({
        title: 'Transferi sil',
        message: 'Bu transfer silinsin mi?',
        danger: true,
      }))
    ) {
      return;
    }
    this.expensesApi.deleteTransfer(this.planId, id).subscribe({
      next: () => {
        this.toast.success('Transfer silindi.');
        this.reload();
      },
      error: (err) => this.toast.error(apiErrorMessage(err, 'Silinemedi.')),
    });
  }

  markPaidFromAction(event: MarkExpensePaidEvent): void {
    const { expense: e, payerId, payments } = event;
    if (!this.canManageExpense(e)) {
      this.toast.error('Yalnızca eklediğiniz giderleri düzenleyebilirsiniz.');
      return;
    }
    if (!payerId || Math.abs(payments.reduce((sum, payment) => sum + payment.amount, 0) - e.totalAmount) > 0.01) {
      this.toast.error('Ödeyen tutarlarının toplamı gider tutarına eşit olmalıdır.');
      return;
    }
    const body: ExpenseRequest = {
      name: e.name,
      occurredOn: String(e.occurredOn).slice(0, 10),
      totalAmount: e.totalAmount,
      shareType: e.shareType,
      status: 'Paid',
      paidByPartnerId: payments.length === 1 ? payerId : null,
      categoryId: e.categoryId,
      note: e.note,
      customShares: e.customShares ?? [],
      payments,
    };
    this.markingId.set(e.id);
    this.expensesApi.update(this.planId, e.id, body).subscribe({
      next: () => {
        this.markingId.set(null);
        const summary = payments
          .map((p) => `${this.partnerName(p.partnerId)} ${this.formatTry(p.amount)}`)
          .join(' · ');
        this.toast.success(`“${e.name}” ödendi · ${summary}`);
        this.reload();
      },
      error: (err) => {
        this.markingId.set(null);
        this.toast.error(apiErrorMessage(err, 'Ödendi işaretlenemedi.'));
      },
    });
  }

  openEdit(e: ExpenseDto): void {
    if (!this.canManageExpense(e)) {
      this.toast.error('Yalnızca eklediğiniz giderleri düzenleyebilirsiniz.');
      return;
    }
    this.editingExpense.set(e);
  }

  closeEditModal(): void {
    this.editingExpense.set(null);
  }

  saveEditedExpense(event: ExpenseEditSaveEvent): void {
    this.saving.set(true);
    this.expensesApi.update(this.planId, event.expenseId, event.request).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEditModal();
        this.toast.success('Gider güncellendi.');
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, 'Gider güncellenemedi.'));
      },
    });
  }

  async removeExpense(id: string, title: string): Promise<void> {
    if (
      !(await this.confirm.ask({
        title: 'Gideri sil',
        message: `“${title}” silinsin mi?`,
        confirmLabel: 'Sil',
        danger: true,
      }))
    ) {
      return;
    }
    this.expensesApi.delete(this.planId, id).subscribe({
      next: () => {
        this.toast.success('Silindi.');
        this.reload();
      },
      error: (err) => this.toast.error(apiErrorMessage(err, 'Silinemedi.')),
    });
  }
}

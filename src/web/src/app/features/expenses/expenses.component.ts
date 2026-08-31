import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ExpenseBoardDto,
  ExpenseDto,
  ExpenseReceiptDraftDto,
  ExpenseRequest,
  PartnerDto,
} from '../../core/models/api.models';
import { ExpensesApi } from '../../core/services/expenses.api';
import { PartnersApi } from '../../core/services/partners.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { ToastService } from '../../shared/toast/toast.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { isExpensePlan } from '../../core/utils/plan-routes';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { ExpenseListControlsComponent } from './expense-list-controls/expense-list-controls.component';
import { MarkExpensePaidEvent } from './expense-row-actions/expense-row-actions.component';
import { ExpenseAddFormComponent } from './expense-form/expense-add-form.component';
import {
  ExpenseEditModalComponent,
  ExpenseEditSaveEvent,
} from './expense-form/expense-edit-modal.component';
import { ExpensePartnerOption } from './expense-partner-option';
import { ExpenseListComponent } from './expense-list/expense-list.component';
import { StatementImportModalComponent } from './statement-import/statement-import-modal.component';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import {
  DEFAULT_EXPENSE_FILTER_STATE,
  ExpenseFilterState,
  FilteredExpenseStats,
  applyExpenseFilters,
  sortExpenses,
} from './expense-filter.models';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [
    RouterLink,
    ExpenseListControlsComponent,
    ExpenseListComponent,
    ExpenseAddFormComponent,
    ExpenseEditModalComponent,
    StatementImportModalComponent,
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
  readonly filterState = signal<ExpenseFilterState>({ ...DEFAULT_EXPENSE_FILTER_STATE });
  readonly expensePage = signal(1);
  readonly expensePageSize = 50;
  readonly partners = signal<PartnerDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly markingId = signal<string | null>(null);
  readonly showAddForm = signal(false);
  readonly activeTab = signal<'list' | 'summary'>('list');
  readonly editingExpense = signal<ExpenseDto | null>(null);
  readonly analyzingReceipt = signal(false);
  readonly receiptDraft = signal<ExpenseReceiptDraftDto | null>(null);
  readonly showStatementImportModal = signal(false);

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

  // All expenses from board
  readonly allExpenses = computed<ExpenseDto[]>(() => this.board()?.expenses ?? []);

  // Filtered and sorted expenses
  readonly filteredExpenses = computed<ExpenseDto[]>(() => {
    const raw = this.allExpenses();
    const categories = this.board()?.categories ?? [];
    const filtered = applyExpenseFilters(raw, this.filterState(), categories);
    return sortExpenses(filtered, this.filterState().sortBy);
  });

  // Filtered statistics
  readonly filteredStats = computed<FilteredExpenseStats>(() => {
    const all = this.allExpenses();
    const filtered = this.filteredExpenses();
    const paid = filtered.filter((e) => this.isPaid(e));
    const planned = filtered.filter((e) => !this.isPaid(e));

    return {
      totalCount: all.length,
      filteredCount: filtered.length,
      totalAmount: filtered.reduce((sum, e) => sum + Number(e.totalAmount || 0), 0),
      paidAmount: paid.reduce((sum, e) => sum + Number(e.totalAmount || 0), 0),
      plannedAmount: planned.reduce((sum, e) => sum + Number(e.totalAmount || 0), 0),
    };
  });

  // Paged slice of filtered expenses
  readonly sortedExpenses = computed<ExpenseDto[]>(() => {
    const list = this.filteredExpenses();
    const page = this.expensePage();
    const pageSize = this.expensePageSize;
    const start = (page - 1) * pageSize;
    return list.slice(start, start + pageSize);
  });

  get expensePageCount(): number {
    const total = this.filteredExpenses().length;
    if (total <= 0) return 1;
    return Math.max(1, Math.ceil(total / this.expensePageSize));
  }

  get expenseTotalCount(): number {
    return this.filteredExpenses().length;
  }

  canManageExpense(expense: ExpenseDto): boolean {
    return expense.canManage;
  }

  planId = '';

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
        this.loading.set(false);
        this.board.set(board);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(err, 'Gider tablosu yüklenemedi.'));
      },
    });
  }

  changeExpensePage(newPage: number): void {
    if (newPage < 1 || newPage > this.expensePageCount || newPage === this.expensePage()) {
      return;
    }
    this.expensePage.set(newPage);
  }

  setFilterState(newState: ExpenseFilterState): void {
    this.filterState.set(newState);
    this.expensePage.set(1);
  }

  resetAllFilters(): void {
    this.filterState.set({ ...DEFAULT_EXPENSE_FILTER_STATE });
    this.expensePage.set(1);
  }

  isPaid(e: ExpenseDto): boolean {
    return e.status === 'Paid' || e.status === (1 as unknown as ExpenseDto['status']);
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

  setTab(tab: 'list' | 'summary'): void {
    this.activeTab.set(tab);
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

  openStatementImportModal(): void {
    this.showStatementImportModal.set(true);
  }

  closeStatementImportModal(): void {
    this.showStatementImportModal.set(false);
  }

  onStatementImported(_count: number): void {
    this.reload();
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

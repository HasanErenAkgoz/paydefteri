import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ExpenseBalanceDto,
  ExpenseBoardDto,
  PlanDto,
  SettlementBalanceDto,
  SettlementTransferRequest,
} from '../../core/models/api.models';
import { ExpensesApi } from '../../core/services/expenses.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { isExpensePlan } from '../../core/utils/plan-routes';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import { ToastService } from '../../shared/toast/toast.service';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { ExpensePartnerOption } from '../expenses/expense-partner-option';
import { ExpenseTransferPanelComponent } from '../expenses/expense-transfer-panel/expense-transfer-panel.component';

type BalanceRow = SettlementBalanceDto | ExpenseBalanceDto;

@Component({
  selector: 'app-balances',
  standalone: true,
  imports: [RouterLink, CurrencyTryPipe, ExpenseTransferPanelComponent],
  templateUrl: './balances.component.html',
  styleUrl: './balances.component.scss',
})
export class BalancesComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plansApi = inject(PlansApi);
  private readonly expensesApi = inject(ExpensesApi);
  private readonly planContext = inject(PlanContextService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly plan = signal<PlanDto | null>(null);
  readonly installmentBoard = signal<{ isOwner: boolean; settlements: SettlementBalanceDto[] } | null>(null);
  readonly expenseBoard = signal<ExpenseBoardDto | null>(null);

  readonly isExpense = computed(() => isExpensePlan(this.plan()));
  readonly balanceRows = computed<BalanceRow[]>(() =>
    this.isExpense() ? this.expenseBoard()?.balances ?? [] : this.installmentBoard()?.settlements ?? []
  );
  readonly isOwner = computed(() =>
    this.isExpense() ? !!this.expenseBoard()?.isOwner : !!this.installmentBoard()?.isOwner
  );
  readonly partners = computed<ExpensePartnerOption[]>(() =>
    (this.expenseBoard()?.balances ?? []).map((balance) => ({
      id: balance.partnerId,
      name: balance.partnerName,
      color: balance.color,
    }))
  );
  readonly hasOpenBalance = computed(() =>
    this.balanceRows().some((row) => Math.abs(Number(row.balance) || 0) > 0.005)
  );
  readonly debtor = computed(() => this.balanceRows().find((row) => row.balance < -0.005) ?? null);
  readonly creditor = computed(() => this.balanceRows().find((row) => row.balance > 0.005) ?? null);
  readonly initialTransferAmount = computed(() => {
    const debtor = this.debtor();
    const creditor = this.creditor();
    return debtor && creditor ? Math.min(-debtor.balance, creditor.balance) : null;
  });

  planId = '';

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.planId) {
      void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
      return;
    }

    this.plansApi.get(this.planId).subscribe({
      next: (plan) => {
        this.plan.set(plan);
        this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
        this.loadBoard();
      },
      error: () => void this.router.navigate(['/plans'], { queryParams: { manage: '1' } }),
    });
  }

  backLink(): string[] {
    return ['/plans', this.planId, this.isExpense() ? 'expenses' : 'dashboard'];
  }

  balanceHint(balance: number): string {
    if (Math.abs(balance) < 0.005) return 'Dengede';
    return balance > 0 ? 'Alacaklı' : 'Borçlu';
  }

  async settleInstallmentPlan(): Promise<void> {
    if (!this.isOwner() || !this.hasOpenBalance()) return;
    if (
      !(await this.confirm.ask({
        title: 'Hesabı kapat',
        message: 'Ortaklar arası iç bakiye sıfırlanacak. Devam etmek istiyor musunuz?',
        confirmLabel: 'Hesabı kapat',
        danger: true,
      }))
    ) {
      return;
    }
    this.saving.set(true);
    this.plansApi.settleUp(this.planId).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('İç bakiye sıfırlandı.');
        this.loadBoard();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, 'Hesap kapatılamadı.'));
      },
    });
  }

  addTransfer(request: SettlementTransferRequest): void {
    if (!this.isOwner()) {
      this.toast.error('Transfer kaydını yalnızca plan sahibi ekleyebilir.');
      return;
    }
    this.saving.set(true);
    this.expensesApi.createTransfer(this.planId, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success('Transfer kaydedildi.');
        this.loadBoard();
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error(apiErrorMessage(err, 'Transfer eklenemedi.'));
      },
    });
  }

  async removeTransfer(id: string): Promise<void> {
    if (!this.isOwner()) return;
    if (!(await this.confirm.ask({ title: 'Transferi sil', message: 'Bu transfer silinsin mi?', danger: true }))) {
      return;
    }
    this.expensesApi.deleteTransfer(this.planId, id).subscribe({
      next: () => {
        this.toast.success('Transfer silindi.');
        this.loadBoard();
      },
      error: (err) => this.toast.error(apiErrorMessage(err, 'Transfer silinemedi.')),
    });
  }

  private loadBoard(): void {
    this.loading.set(true);
    if (this.isExpense()) {
      this.expensesApi.board(this.planId).subscribe({
        next: (board) => {
          this.expenseBoard.set(board);
          this.loading.set(false);
        },
        error: (err) => this.showLoadError(err),
      });
      return;
    }
    this.plansApi.dashboard(this.planId).subscribe({
      next: (board) => {
        this.installmentBoard.set({ isOwner: board.isOwner, settlements: board.settlements });
        this.loading.set(false);
      },
      error: (err) => this.showLoadError(err),
    });
  }

  private showLoadError(err: unknown): void {
    this.loading.set(false);
    this.toast.error(apiErrorMessage(err, 'Bakiye bilgileri yüklenemedi.'));
  }
}

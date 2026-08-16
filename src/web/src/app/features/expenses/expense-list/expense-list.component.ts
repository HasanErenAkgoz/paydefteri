import { Component, input, output } from '@angular/core';
import { ExpenseDto, ExpenseShareLineDto } from '../../../core/models/api.models';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { formatDateTr } from '../../../shared/utils/format';
import { ExpenseMobileSummaryComponent } from '../expense-mobile-summary/expense-mobile-summary.component';
import { ExpensePartnerOption } from '../expense-partner-option';
import {
  ExpenseRowActionsComponent,
  MarkExpensePaidEvent,
} from '../expense-row-actions/expense-row-actions.component';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [CurrencyTryPipe, ExpenseMobileSummaryComponent, ExpenseRowActionsComponent],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.scss',
})
export class ExpenseListComponent {
  readonly expenses = input.required<ExpenseDto[]>();
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly markingId = input<string | null>(null);
  readonly markPaid = output<MarkExpensePaidEvent>();
  readonly edit = output<ExpenseDto>();
  readonly remove = output<ExpenseDto>();
  readonly formatDateTr = formatDateTr;

  isPaid(expense: ExpenseDto): boolean {
    return expense.status === 'Paid' || expense.status === 1;
  }

  visibleShareLines(expense: ExpenseDto): ExpenseShareLineDto[] {
    return (expense.shareLines ?? []).filter(
      (line) => Math.abs(Number(line.shareAmount) || 0) > 0.005
    );
  }

  paymentSummary(expense: ExpenseDto): string {
    const payments = expense.payments?.filter((payment) => Number(payment.amount) > 0.005) ?? [];
    if (!payments.length) {
      return this.partnerName(expense.paidByPartnerId);
    }
    return payments
      .map((payment) => `${this.partnerName(payment.partnerId)} ${this.formatTry(payment.amount)}`)
      .join(' · ');
  }

  private partnerName(partnerId: string | null | undefined): string {
    if (!partnerId) return '—';
    return this.partners().find((partner) => partner.id === partnerId)?.name ?? '—';
  }

  private formatTry(amount: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      maximumFractionDigits: 2,
    }).format(Number(amount) || 0);
  }
}

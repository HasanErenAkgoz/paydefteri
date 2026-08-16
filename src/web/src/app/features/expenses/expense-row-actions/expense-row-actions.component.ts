import { Component, OnChanges, SimpleChanges, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ExpenseDto, ExpensePaymentDto } from '../../../core/models/api.models';
import { ExpensePartnerOption } from '../expense-partner-option';

export interface MarkExpensePaidEvent {
  expense: ExpenseDto;
  payerId: string;
  payments: ExpensePaymentDto[];
}

@Component({
  selector: 'app-expense-row-actions',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './expense-row-actions.component.html',
  styleUrl: './expense-row-actions.component.scss',
})
export class ExpenseRowActionsComponent implements OnChanges {
  readonly expense = input.required<ExpenseDto>();
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly canManage = input.required<boolean>();
  readonly marking = input.required<boolean>();
  readonly markPaid = output<MarkExpensePaidEvent>();
  readonly edit = output<ExpenseDto>();
  readonly remove = output<ExpenseDto>();
  singlePayment = false;
  payerId = '';
  payments: Record<string, number> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['expense']) return;
    const expense = this.expense();
    this.payerId = expense.paidByPartnerId ?? this.partners()[0]?.id ?? '';
    this.payments = Object.fromEntries(this.sharePartners().map((partner) => [partner.id, this.shareAmount(partner.id)]));
  }

  isPaid(): boolean { return String(this.expense().status) === 'Paid' || this.expense().status === 1; }
  sharePartners(): ExpensePartnerOption[] {
    const ids = new Set(this.expense().shareLines.filter((line) => line.shareAmount > 0.005).map((line) => line.partnerId));
    return this.partners().filter((partner) => ids.size === 0 || ids.has(partner.id));
  }
  canUseSplit(): boolean { return this.sharePartners().length > 1 && !this.singlePayment; }
  shareAmount(id: string): number { return Number(this.expense().shareLines.find((line) => line.partnerId === id)?.shareAmount ?? 0); }
  paymentTotal(): number { return this.sharePartners().reduce((sum, partner) => sum + Number(this.payments[partner.id] ?? 0), 0); }
  matchesTotal(): boolean { return Math.abs(this.paymentTotal() - Number(this.expense().totalAmount)) <= 0.01; }
  submit(): void {
    const payments = this.singlePayment
      ? [{ partnerId: this.payerId, amount: Number(this.expense().totalAmount) }]
      : this.sharePartners().map((partner) => ({ partnerId: partner.id, amount: Number(this.payments[partner.id] ?? 0) })).filter((payment) => payment.amount > 0);
    if (!this.payerId || (!this.singlePayment && !this.matchesTotal())) return;
    this.markPaid.emit({ expense: this.expense(), payerId: this.payerId, payments });
  }
}

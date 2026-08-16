import { Component, OnChanges, SimpleChanges, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { amountsMatchTotal } from './expense-form-calculations';
import { ExpensePartnerOption } from '../expense-partner-option';

export interface ExpensePayerState {
  singlePayment: boolean;
  payerId: string;
  payments: Record<string, number>;
}

@Component({
  selector: 'app-expense-payer-inputs',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe],
  templateUrl: './expense-payer-inputs.component.html',
  styleUrl: './expense-payer-inputs.component.scss',
})
export class ExpensePayerInputsComponent implements OnChanges {
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly sharePartners = input.required<ExpensePartnerOption[]>();
  readonly totalAmount = input.required<number>();
  readonly initialSinglePayment = input(false);
  readonly initialPayerId = input('');
  readonly initialPayments = input<Record<string, number>>({});
  readonly stateChange = output<ExpensePayerState>();

  singlePayment = false;
  payerId = '';
  payments: Record<string, number> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialSinglePayment']) this.singlePayment = this.initialSinglePayment();
    if (changes['initialPayerId']) this.payerId = this.initialPayerId() || this.partners()[0]?.id || '';
    if (changes['initialPayments']) this.payments = { ...this.initialPayments() };
  }

  toggleMode(): void {
    this.singlePayment = !this.singlePayment;
    this.emitState();
  }

  updatePayment(partnerId: string, amount: number): void {
    this.payments = { ...this.payments, [partnerId]: Number(amount) || 0 };
    this.emitState();
  }

  emitState(): void {
    this.stateChange.emit({ singlePayment: this.singlePayment, payerId: this.payerId, payments: this.payments });
  }

  paymentTotal(): number {
    return this.sharePartners().reduce((sum, partner) => sum + Number(this.payments[partner.id] || 0), 0);
  }

  matches(): boolean {
    return amountsMatchTotal(this.payments, Number(this.totalAmount()) || 0);
  }
}

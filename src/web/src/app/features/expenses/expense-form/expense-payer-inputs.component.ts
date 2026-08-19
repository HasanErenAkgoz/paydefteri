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
  readonly initialSinglePayment = input(true);
  readonly initialPayerId = input('');
  readonly initialPayments = input<Record<string, number>>({});
  readonly stateChange = output<ExpensePayerState>();

  singlePayment = true;
  payerId = '';
  payments: Record<string, number> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialSinglePayment'] && this.initialSinglePayment() !== undefined) {
      this.singlePayment = this.initialSinglePayment();
    }
    if (changes['initialPayerId'] || (!this.payerId && this.partners().length)) {
      this.payerId = this.initialPayerId() || this.payerId || this.partners()[0]?.id || '';
    }
    if (changes['initialPayments']) {
      this.payments = { ...this.initialPayments() };
    }
  }

  selectPayer(partnerId: string): void {
    this.payerId = partnerId;
    this.emitState();
  }

  toggleMode(): void {
    this.singlePayment = !this.singlePayment;
    if (!this.singlePayment && Object.keys(this.payments).length === 0) {
      this.distributeEvenly();
    } else {
      this.emitState();
    }
  }

  distributeEvenly(): void {
    const amount = Number(this.totalAmount()) || 0;
    const partners = this.sharePartners().length ? this.sharePartners() : this.partners();
    if (!partners.length) return;
    const equal = Math.floor((amount / partners.length) * 100) / 100;
    let assigned = 0;
    const nextPayments: Record<string, number> = {};
    partners.forEach((p, idx) => {
      const val = idx === partners.length - 1 ? Math.round((amount - assigned) * 100) / 100 : equal;
      nextPayments[p.id] = val;
      assigned += idx === partners.length - 1 ? 0 : equal;
    });
    this.payments = nextPayments;
    this.emitState();
  }

  updatePayment(partnerId: string, amount: number): void {
    this.payments = { ...this.payments, [partnerId]: Number(amount) || 0 };
    this.emitState();
  }

  emitState(): void {
    this.stateChange.emit({
      singlePayment: this.singlePayment,
      payerId: this.payerId,
      payments: this.payments,
    });
  }

  paymentTotal(): number {
    return (this.sharePartners().length ? this.sharePartners() : this.partners()).reduce(
      (sum, partner) => sum + Number(this.payments[partner.id] || 0),
      0
    );
  }

  matches(): boolean {
    return amountsMatchTotal(this.payments, Number(this.totalAmount()) || 0);
  }

  getPartnerColor(partner: ExpensePartnerOption, idx: number): string {
    if (partner.color) return partner.color;
    const defaultColors = ['#38bdf8', '#fb923c', '#a855f7', '#34d399', '#f43f5e', '#eab308'];
    return defaultColors[idx % defaultColors.length];
  }
}

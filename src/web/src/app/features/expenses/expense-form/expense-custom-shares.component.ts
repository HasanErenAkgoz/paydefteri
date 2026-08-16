import { Component, OnChanges, SimpleChanges, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ExpensePartnerOption } from '../expense-partner-option';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { amountsMatchTotal } from './expense-form-calculations';

@Component({
  selector: 'app-expense-custom-shares',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe],
  templateUrl: './expense-custom-shares.component.html',
  styleUrl: './expense-custom-shares.component.scss',
})
export class ExpenseCustomSharesComponent implements OnChanges {
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly expectedTotal = input.required<number>();
  readonly amounts = input.required<Record<string, number>>();
  readonly amountsChange = output<Record<string, number>>();

  values: Record<string, number> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['amounts']) {
      this.values = { ...this.amounts() };
    }
  }

  update(partnerId: string, amount: number): void {
    this.values = { ...this.values, [partnerId]: Number(amount) || 0 };
    this.amountsChange.emit(this.values);
  }

  total(): number {
    return Object.values(this.values).reduce((sum, amount) => sum + (Number(amount) || 0), 0);
  }

  matches(): boolean {
    return amountsMatchTotal(this.values, Number(this.expectedTotal()) || 0);
  }
}

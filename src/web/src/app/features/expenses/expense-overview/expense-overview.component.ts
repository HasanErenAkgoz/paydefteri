import { Component, input, output } from '@angular/core';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { ExpenseBalanceDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-expense-overview',
  standalone: true,
  imports: [CurrencyTryPipe],
  templateUrl: './expense-overview.component.html',
  styleUrl: './expense-overview.component.scss',
})
export class ExpenseOverviewComponent {
  readonly planTitle = input.required<string>();
  readonly isOwner = input.required<boolean>();
  readonly isAddFormVisible = input.required<boolean>();
  readonly paidTotal = input.required<number>();
  readonly paidCount = input.required<number>();
  readonly plannedTotal = input.required<number>();
  readonly plannedCount = input.required<number>();
  readonly recurrenceCount = input.required<number>();
  readonly transferCount = input.required<number>();
  readonly balances = input.required<ExpenseBalanceDto[]>();
  readonly toggleAddForm = output<void>();

  balanceHint(balance: number): string {
    if (balance > 0.005) return 'Alacaklı';
    if (balance < -0.005) return 'Borçlu';
    return 'Dengede';
  }
}

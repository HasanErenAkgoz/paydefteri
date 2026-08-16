import { Component, input } from '@angular/core';
import { ExpenseDto, ExpenseShareLineDto } from '../../../core/models/api.models';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { formatDateTr } from '../../../shared/utils/format';

@Component({
  selector: 'app-expense-mobile-summary',
  standalone: true,
  imports: [CurrencyTryPipe],
  templateUrl: './expense-mobile-summary.component.html',
  styleUrl: './expense-mobile-summary.component.scss',
})
export class ExpenseMobileSummaryComponent {
  readonly expense = input.required<ExpenseDto>();
  readonly paid = input.required<boolean>();
  readonly paymentSummary = input.required<string>();
  readonly shareLines = input.required<ExpenseShareLineDto[]>();
  readonly formatDateTr = formatDateTr;
}

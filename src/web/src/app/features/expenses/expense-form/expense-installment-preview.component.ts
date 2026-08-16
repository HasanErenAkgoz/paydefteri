import { Component, input } from '@angular/core';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { formatDateTr } from '../../../shared/utils/format';
import { InstallmentPreview } from './expense-form-calculations';

@Component({
  selector: 'app-expense-installment-preview',
  standalone: true,
  imports: [CurrencyTryPipe],
  templateUrl: './expense-installment-preview.component.html',
})
export class ExpenseInstallmentPreviewComponent {
  readonly preview = input.required<InstallmentPreview>();
  readonly firstInstallmentDate = input.required<string>();
  readonly formatDateTr = formatDateTr;
}

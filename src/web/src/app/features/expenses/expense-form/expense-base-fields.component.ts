import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ExpenseCategoryDto } from '../../../core/models/api.models';
import { ExpensePartnerOption } from '../expense-partner-option';

export type ExpenseShareUi = 'Equal' | 'Default' | 'sole' | 'Custom';
export interface ExpenseBaseFieldsState {
  name: string; amount: number | null; paymentMode: 'Cash' | 'Installment'; installmentCount: number;
  occurredOn: string; categoryId: string; shareUi: ExpenseShareUi; solePartnerId: string;
  status: 'Paid' | 'Planned'; note: string;
}
@Component({
  selector: 'app-expense-base-fields',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './expense-base-fields.component.html',
  styleUrl: './expense-base-fields.component.scss',
})
export class ExpenseBaseFieldsComponent {
  readonly state = input.required<ExpenseBaseFieldsState>();
  readonly categories = input.required<ExpenseCategoryDto[]>();
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly stateChange = output<ExpenseBaseFieldsState>();

  update<K extends keyof ExpenseBaseFieldsState>(key: K, value: ExpenseBaseFieldsState[K]): void {
    const next = { ...this.state(), [key]: value };
    if (key === 'paymentMode') next.installmentCount = value === 'Installment' ? 2 : 1;
    this.stateChange.emit(next);
  }
}

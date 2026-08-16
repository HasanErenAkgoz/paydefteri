import { Component, input, output } from '@angular/core';

export type ExpenseListFilter = 'all' | 'paid' | 'planned';

@Component({
  selector: 'app-expense-list-controls',
  standalone: true,
  templateUrl: './expense-list-controls.component.html',
  styleUrl: './expense-list-controls.component.scss',
})
export class ExpenseListControlsComponent {
  readonly isOwner = input.required<boolean>();
  readonly filter = input.required<ExpenseListFilter>();
  readonly page = input.required<number>();
  readonly pageCount = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly filterChange = output<ExpenseListFilter>();
  readonly pageChange = output<number>();
}

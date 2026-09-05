import { Component, inject, input, output, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CreditCardStatementAnalysisResultDto,
  ExpenseCategoryDto,
  StatementExpenseImportItem,
  StatementTransactionItemDto,
} from '../../../core/models/api.models';
import { ExpensesApi } from '../../../core/services/expenses.api';
import { ToastService } from '../../../shared/toast/toast.service';
import { apiErrorMessage } from '../../../shared/utils/api-error';
import { ExpensePartnerOption } from '../expense-partner-option';
import { FocusTrapDirective } from '../../../shared/directives/focus-trap.directive';

type ImportRow = StatementTransactionItemDto & { selected: boolean; categoryId: string | null };

@Component({
  selector: 'app-statement-import-modal',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe, FocusTrapDirective],
  templateUrl: './statement-import-modal.component.html',
  styleUrl: './statement-import-modal.component.scss',
})
export class StatementImportModalComponent {
  private readonly expensesApi = inject(ExpensesApi);
  private readonly toast = inject(ToastService);

  readonly planId = input.required<string>();
  readonly categories = input<ExpenseCategoryDto[]>([]);
  readonly partners = input<ExpensePartnerOption[]>([]);
  readonly closed = output<void>();
  readonly imported = output<number>();

  readonly file = signal<File | null>(null);
  readonly analysis = signal<CreditCardStatementAnalysisResultDto | null>(null);
  readonly rows = signal<ImportRow[]>([]);
  readonly defaultPayerId = signal<string | null>(null);
  readonly analyzing = signal(false);
  readonly importing = signal(false);

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    if (!file) return;
    if (file.size > 15 * 1024 * 1024) {
      this.toast.error('Ekstre dosyası en fazla 15 MB olabilir.');
      return;
    }
    this.file.set(file);
    this.analysis.set(null);
    this.rows.set([]);
  }

  analyze(): void {
    const file = this.file();
    if (!file) {
      this.toast.error('Önce ekstre dosyanızı seçin.');
      return;
    }
    this.analyzing.set(true);
    this.expensesApi.analyzeStatement(this.planId(), file, this.defaultPayerId()).subscribe({
      next: (analysis) => {
        this.analyzing.set(false);
        this.analysis.set(analysis);
        this.rows.set(analysis.transactions
          .filter((transaction) => !transaction.isIncomeOrRefund)
          .map((transaction) => ({
            ...transaction,
            selected: !transaction.isPossibleDuplicate,
            categoryId: transaction.suggestedCategoryId,
          })));
      },
      error: (error) => {
        this.analyzing.set(false);
        this.toast.error(apiErrorMessage(error, 'Ekstre analiz edilemedi.'));
      },
    });
  }

  selectedCount(): number {
    return this.rows().filter((row) => row.selected).length;
  }

  selectedTotal(): number {
    return this.rows().filter((row) => row.selected).reduce((total, row) => total + Number(row.amount), 0);
  }

  toggleAll(selected: boolean): void {
    this.rows.update((rows) => rows.map((row) => ({ ...row, selected })));
  }

  updateRow(index: number, change: Partial<ImportRow>): void {
    this.rows.update((rows) => rows.map((row, rowIndex) => rowIndex === index ? { ...row, ...change } : row));
  }

  importSelected(): void {
    const selected = this.rows().filter((row) => row.selected);
    if (!selected.length) {
      this.toast.error('İçe aktarılacak en az bir harcama seçin.');
      return;
    }
    if (!this.defaultPayerId()) {
      this.toast.error('Ekstre borcunu ödeyen ortağı seçin.');
      return;
    }
    const items: StatementExpenseImportItem[] = selected.map((row) => ({
      name: row.merchantName?.trim() || row.description.trim(),
      occurredOn: String(row.occurredOn).slice(0, 10),
      totalAmount: Number(row.amount),
      shareType: 'Default',
      status: 'Paid',
      paidByPartnerId: this.defaultPayerId(),
      categoryId: row.categoryId,
      note: row.note ?? undefined,
      payments: this.defaultPayerId() ? [{ partnerId: this.defaultPayerId()!, amount: Number(row.amount) }] : [],
      installmentCount: 1,
    }));

    this.importing.set(true);
    this.expensesApi.importStatementExpenses(this.planId(), items).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.toast.success(`${result.importedCount} harcama içe aktarıldı.`);
        this.imported.emit(result.importedCount);
        this.closed.emit();
      },
      error: (error) => {
        this.importing.set(false);
        this.toast.error(apiErrorMessage(error, 'Ekstre harcamaları içe aktarılamadı.'));
      },
    });
  }
}

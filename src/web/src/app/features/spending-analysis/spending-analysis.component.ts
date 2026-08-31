import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SpendingAnalysisApi } from '../../core/services/spending-analysis.api';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { ToastService } from '../../shared/toast/toast.service';
import { apiErrorMessage } from '../../shared/utils/api-error';
import {
  SPENDING_CATEGORIES,
  SpendingStatementDetail,
  SpendingStatementListItem,
  SpendingBudgetStatus,
  SpendingDashboard,
  SpendingPatterns,
  SpendingCoachResponse,
} from './spending-analysis.models';

@Component({
  selector: 'app-spending-analysis',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './spending-analysis.component.html',
  styleUrls: ['./spending-analysis.component.scss', './spending-analysis.review-fixes.scss'],
})
export class SpendingAnalysisComponent implements OnInit {
  readonly Math = Math;
  private readonly api = inject(SpendingAnalysisApi);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly categories = SPENDING_CATEGORIES;
  readonly statements = signal<SpendingStatementListItem[]>([]);
  readonly selected = signal<SpendingStatementDetail | null>(null);
  readonly loading = signal(true);
  readonly uploading = signal(false);
  readonly dragActive = signal(false);
  readonly categoryFilter = signal('all');
  readonly dashboard = signal<SpendingDashboard | null>(null);
  readonly patterns = signal<SpendingPatterns | null>(null);
  readonly budgetStatus = signal<SpendingBudgetStatus | null>(null);
  readonly budgetCategory = signal('Market');
  readonly budgetCurrency = signal('TRY');
  readonly budgetAmount = signal<number | null>(null);
  readonly savingBudget = signal(false);
  readonly coach = signal<SpendingCoachResponse | null>(null);
  readonly coachQuestion = signal('');
  readonly coachLoading = signal(false);
  readonly suggestedQuestions = [
    'En büyük 5 işlem hangisi?',
    'Hafta sonu ne kadar harcadım?',
    'Önceki ekstreye göre nasıl değişti?',
    'Tekrarlayan ödemelerim neler?',
  ];
  private readonly activeStatementRequest = signal<string | null>(null);

  readonly purchases = computed(() =>
    (this.selected()?.transactions ?? []).filter((transaction) => !transaction.isRefund)
  );
  readonly filteredTransactions = computed(() => {
    const category = this.categoryFilter();
    const rows = this.selected()?.transactions ?? [];
    return category === 'all' ? rows : rows.filter((row) => row.category === category);
  });
  readonly availableCurrencies = computed(() => {
    const currencies = this.selected()?.totals.map((total) => total.currency) ?? [];
    return currencies.length ? currencies : ['TRY'];
  });
  readonly categoryMetrics = computed(() => {
    const totals = new Map<string, { categoryCode: string; amount: number; currency: string; transactionCount: number }>();
    for (const row of this.purchases()) {
      const key = `${row.category}|${row.currency}`;
      const current = totals.get(key) ?? { categoryCode: row.category, amount: 0, currency: row.currency, transactionCount: 0 };
      current.amount += Number(row.amount);
      current.transactionCount += 1;
      totals.set(key, current);
    }
    return [...totals.values()].map((metric) => {
      const currencyTotal = this.selected()?.totals.find((total) => total.currency === metric.currency)?.spending ?? 0;
      return { ...metric, percentage: currencyTotal > 0 ? metric.amount / currencyTotal * 100 : 0 };
    }).sort((left, right) =>
      left.currency.localeCompare(right.currency) || right.percentage - left.percentage
    );
  });

  ngOnInit(): void {
    this.loadStatements();
  }

  loadStatements(selectId?: string, openSelection = true): void {
    this.loading.set(true);
    this.api.listStatements().subscribe({
      next: (statements) => {
        this.statements.set(statements);
        this.loading.set(false);
        if (openSelection) {
          const id = selectId ?? this.selected()?.id ?? statements[0]?.id;
          if (id) this.openStatement(id);
        }
      },
      error: (error) => {
        this.loading.set(false);
        this.toast.error(apiErrorMessage(error, 'Ekstre geçmişi yüklenemedi.'));
      },
    });
  }

  openStatement(statementId: string): void {
    this.activeStatementRequest.set(statementId);
    this.api.getStatement(statementId).subscribe({
      next: (statement) => {
        if (this.activeStatementRequest() !== statementId) return;
        this.selected.set(statement);
        this.budgetCurrency.set(statement.totals[0]?.currency ?? 'TRY');
        this.categoryFilter.set('all');
        this.loadAnalysis(statement.id);
      },
      error: (error) => {
        if (this.activeStatementRequest() === statementId) {
          this.toast.error(apiErrorMessage(error, 'Ekstre açılamadı.'));
        }
      },
    });
  }

  loadAnalysis(statementId: string): void {
    this.coach.set(null);
    this.dashboard.set(null);
    this.patterns.set(null);
    this.budgetStatus.set(null);
    this.api.getDashboard(statementId).subscribe({
      next: (dashboard) => {
        if (this.selected()?.id === statementId) this.dashboard.set(dashboard);
      },
      error: () => {
        if (this.selected()?.id === statementId) this.dashboard.set(null);
      },
    });
    this.api.getPatterns(statementId).subscribe({
      next: (patterns) => {
        if (this.selected()?.id === statementId) this.patterns.set(patterns);
      },
      error: () => {
        if (this.selected()?.id === statementId) this.patterns.set(null);
      },
    });
    this.api.getBudgetStatus(statementId).subscribe({
      next: (status) => {
        if (this.selected()?.id === statementId) this.budgetStatus.set(status);
      },
      error: () => {
        if (this.selected()?.id === statementId) this.budgetStatus.set(null);
      },
    });
  }

  loadCoach(statementId: string): void {
    this.coachLoading.set(true);
    this.api.getCoach(statementId).subscribe({
      next: (response) => {
        if (this.selected()?.id === statementId) this.coach.set(response);
        this.coachLoading.set(false);
      },
      error: () => {
        this.coach.set(null);
        this.coachLoading.set(false);
      },
    });
  }

  askCoach(question = this.coachQuestion()): void {
    const statementId = this.selected()?.id;
    const cleanQuestion = question.trim();
    if (!statementId || !cleanQuestion || this.coachLoading()) return;
    this.coachQuestion.set(cleanQuestion);
    this.coachLoading.set(true);
    this.api.askCoach(statementId, cleanQuestion).subscribe({
      next: (response) => {
        if (this.selected()?.id === statementId) this.coach.set(response);
        this.coachLoading.set(false);
      },
      error: (error) => {
        this.coachLoading.set(false);
        this.toast.error(apiErrorMessage(error, 'Finans koçu şu anda yanıt veremedi.'));
      },
    });
  }

  maxDailyAmount(currency: SpendingDashboard['currencies'][number]): number {
    return Math.max(1, ...currency.dailySeries.map((day) => Number(day.amount)));
  }

  saveBudget(): void {
    const statement = this.selected();
    const amount = Number(this.budgetAmount());
    if (!statement?.periodEnd || !(amount > 0)) {
      this.toast.error('Geçerli bir aylık bütçe tutarı girin.');
      return;
    }
    const month = statement.periodEnd.slice(0, 7);
    this.savingBudget.set(true);
    this.api.saveBudget(month, this.budgetCategory(), amount, this.budgetCurrency()).subscribe({
      next: () => {
        this.savingBudget.set(false);
        this.budgetAmount.set(null);
        this.toast.success('Aylık kategori bütçesi kaydedildi.');
        this.loadAnalysis(statement.id);
      },
      error: (error) => {
        this.savingBudget.set(false);
        this.toast.error(apiErrorMessage(error, 'Bütçe kaydedilemedi.'));
      },
    });
  }

  onFileInput(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.upload(file);
    (event.target as HTMLInputElement).value = '';
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.upload(file);
  }

  upload(file: File): void {
    const extension = file.name.split('.').pop()?.toLowerCase();
    if (!extension || !['csv', 'xlsx', 'pdf'].includes(extension)) {
      this.toast.error('PDF, XLSX veya CSV formatında bir ekstre seçin.');
      return;
    }
    if (file.size > 15 * 1024 * 1024) {
      this.toast.error('Ekstre dosyası en fazla 15 MB olabilir.');
      return;
    }
    this.uploading.set(true);
    this.api.uploadStatement(file).subscribe({
      next: (statement) => {
        this.uploading.set(false);
        this.selected.set(statement);
        this.activeStatementRequest.set(statement.id);
        this.budgetCurrency.set(statement.totals[0]?.currency ?? 'TRY');
        this.toast.success('Ekstre analiz edildi. İşlemleri kontrol edebilirsiniz.');
        this.loadAnalysis(statement.id);
        this.loadStatements(undefined, false);
      },
      error: (error) => {
        this.uploading.set(false);
        this.toast.error(apiErrorMessage(error, 'Ekstre analiz edilemedi.'));
      },
    });
  }

  changeCategory(transactionId: string, categoryCode: string): void {
    const statementId = this.selected()?.id;
    if (!statementId) return;
    this.api.updateCategory(statementId, transactionId, categoryCode).subscribe({
      next: () => {
        this.selected.update((statement) => statement ? {
          ...statement,
          transactions: statement.transactions.map((row) =>
            row.id === transactionId ? { ...row, category: categoryCode } : row
          ),
        } : null);
        this.loadAnalysis(statementId);
        this.toast.success('Kategori tercihin kaydedildi.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Kategori güncellenemedi.')),
    });
  }

  async removeStatement(statement: Pick<SpendingStatementListItem, 'id'>): Promise<void> {
    const approved = await this.confirm.ask({
      title: 'Ekstre analizini sil',
      message: 'Normalize edilmiş işlemler ve bu ekstreye ait analiz kalıcı olarak silinsin mi?',
      confirmLabel: 'Sil',
      danger: true,
    });
    if (!approved) return;
    this.api.deleteStatement(statement.id).subscribe({
      next: () => {
        this.selected.set(null);
        this.toast.success('Ekstre analizi silindi.');
        this.loadStatements();
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Ekstre silinemedi.')),
    });
  }

  money(value: number, currency = 'TRY'): string {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency }).format(value || 0);
  }

  statementTotal(statement: SpendingStatementListItem | SpendingStatementDetail): string {
    if (!statement.totals.length) return '—';
    return statement.totals.map((total) => this.money(total.spending, total.currency)).join(' · ');
  }

  largestPurchasesSummary(): string {
    const values = this.dashboard()?.currencies
      .filter((currency) => currency.largestPurchase)
      .map((currency) => `${this.money(currency.largestPurchase!.amount, currency.currency)} · ${currency.largestPurchase!.merchantName}`) ?? [];
    return values.length ? values.join(' | ') : '—';
  }

  topCategoriesSummary(): string {
    const values = this.dashboard()?.currencies
      .filter((currency) => currency.categories.length)
      .map((currency) => `${currency.categories[0]!.category} · ${this.money(currency.categories[0]!.amount, currency.currency)}`) ?? [];
    return values.length ? values.join(' | ') : '—';
  }
}

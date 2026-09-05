import { Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ExpenseCategoryDto } from '../../../core/models/api.models';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { FocusTrapDirective } from '../../../shared/directives/focus-trap.directive';
import { ExpensePartnerOption } from '../expense-partner-option';
import {
  DEFAULT_EXPENSE_FILTER_STATE,
  ExpenseFilterState,
  ExpenseQuickPeriod,
  ExpenseQuickTag,
  ExpenseSortOption,
  ExpenseStatusFilter,
  FilteredExpenseStats,
} from '../expense-filter.models';

@Component({
  selector: 'app-expense-list-controls',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe, FocusTrapDirective],
  templateUrl: './expense-list-controls.component.html',
  styleUrl: './expense-list-controls.component.scss',
})
export class ExpenseListControlsComponent {
  readonly isOwner = input<boolean>(false);
  readonly categories = input<ExpenseCategoryDto[]>([]);
  readonly partners = input<ExpensePartnerOption[]>([]);
  readonly filterState = input.required<ExpenseFilterState>();
  readonly filteredStats = input<FilteredExpenseStats | null>(null);

  readonly page = input<number>(1);
  readonly pageCount = input<number>(1);
  readonly totalCount = input<number>(0);
  readonly pageSize = input<number>(50);

  readonly filterChange = output<ExpenseFilterState>();
  readonly pageChange = output<number>();
  readonly resetFilters = output<void>();

  // Mobile Bottom Sheet state
  readonly mobileFilterOpen = signal(false);

  // Temporary filter state for mobile modal before applying
  readonly tempMobileFilter = signal<ExpenseFilterState>({ ...DEFAULT_EXPENSE_FILTER_STATE });

  readonly hasActiveFilters = computed(() => {
    const s = this.filterState();
    return (
      !!s.search.trim() ||
      s.status !== 'all' ||
      s.categoryId !== 'all' ||
      s.partnerId !== 'all' ||
      s.period !== 'all' ||
      s.quickTag !== 'all' ||
      s.sortBy !== 'date_desc'
    );
  });

  readonly activeFilterCount = computed(() => {
    let count = 0;
    const s = this.filterState();
    if (s.search.trim()) count++;
    if (s.status !== 'all') count++;
    if (s.categoryId !== 'all') count++;
    if (s.partnerId !== 'all') count++;
    if (s.period !== 'all') count++;
    if (s.quickTag !== 'all') count++;
    if (s.sortBy !== 'date_desc') count++;
    return count;
  });

  readonly activeCategoryName = computed(() => {
    const catId = this.filterState().categoryId;
    if (catId === 'all') return 'Tüm Kategoriler';
    if (catId === 'uncategorized') return 'Kategorisiz';
    return this.categories().find((c) => c.id === catId)?.name ?? 'Kategori';
  });

  readonly activePartnerName = computed(() => {
    const pId = this.filterState().partnerId;
    if (pId === 'all') return 'Tüm Ortaklar';
    if (pId === 'mine') return 'Benim İlgili Olduklarım';
    return this.partners().find((p) => p.id === pId)?.name ?? 'Ortak';
  });

  readonly activePeriodLabel = computed(() => {
    const p = this.filterState().period;
    switch (p) {
      case 'this_month':
        return 'Bu Ay';
      case 'last_month':
        return 'Geçen Ay';
      case 'last_30_days':
        return 'Son 30 Gün';
      case 'last_3_months':
        return 'Son 3 Ay';
      case 'this_year':
        return 'Bu Yıl';
      case 'custom':
        return 'Özel Aralık';
      default:
        return 'Tüm Zamanlar';
    }
  });

  // Updates a single field and emits
  updateFilter<K extends keyof ExpenseFilterState>(key: K, value: ExpenseFilterState[K]): void {
    const updated: ExpenseFilterState = {
      ...this.filterState(),
      [key]: value,
    };
    this.filterChange.emit(updated);
  }

  setSearch(search: string): void {
    this.updateFilter('search', search);
  }

  setStatus(status: ExpenseStatusFilter): void {
    this.updateFilter('status', status);
  }

  setCategory(categoryId: string): void {
    this.updateFilter('categoryId', categoryId);
  }

  setPartner(partnerId: string): void {
    this.updateFilter('partnerId', partnerId);
  }

  setPeriod(period: ExpenseQuickPeriod): void {
    this.updateFilter('period', period);
  }

  setSortBy(sortBy: ExpenseSortOption): void {
    this.updateFilter('sortBy', sortBy);
  }

  setQuickTag(quickTag: ExpenseQuickTag): void {
    this.updateFilter('quickTag', quickTag);
  }

  setCustomDates(start: string, end: string): void {
    const updated: ExpenseFilterState = {
      ...this.filterState(),
      period: 'custom',
      customStartDate: start,
      customEndDate: end,
    };
    this.filterChange.emit(updated);
  }

  onResetFilters(): void {
    this.resetFilters.emit();
  }

  // Mobile Bottom Sheet controls
  openMobileFilter(): void {
    this.tempMobileFilter.set({ ...this.filterState() });
    this.mobileFilterOpen.set(true);
  }

  closeMobileFilter(): void {
    this.mobileFilterOpen.set(false);
  }

  updateTempMobileFilter<K extends keyof ExpenseFilterState>(
    key: K,
    value: ExpenseFilterState[K]
  ): void {
    this.tempMobileFilter.set({
      ...this.tempMobileFilter(),
      [key]: value,
    });
  }

  applyMobileFilter(): void {
    this.filterChange.emit(this.tempMobileFilter());
    this.closeMobileFilter();
  }

  resetMobileFilter(): void {
    this.tempMobileFilter.set({ ...DEFAULT_EXPENSE_FILTER_STATE });
    this.filterChange.emit({ ...DEFAULT_EXPENSE_FILTER_STATE });
    this.closeMobileFilter();
  }
}

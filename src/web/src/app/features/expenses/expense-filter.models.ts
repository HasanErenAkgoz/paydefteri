import { ExpenseCategoryDto, ExpenseDto } from '../../core/models/api.models';

export type ExpenseStatusFilter = 'all' | 'paid' | 'planned';
export type ExpenseQuickPeriod =
  | 'all'
  | 'this_month'
  | 'last_month'
  | 'last_30_days'
  | 'last_3_months'
  | 'this_year'
  | 'custom';
export type ExpenseQuickTag = 'all' | 'recurring';
export type ExpenseSortOption =
  | 'date_desc'
  | 'date_asc'
  | 'amount_desc'
  | 'amount_asc'
  | 'name_asc';

export interface ExpenseFilterState {
  search: string;
  status: ExpenseStatusFilter;
  categoryId: string;
  partnerId: string;
  period: ExpenseQuickPeriod;
  quickTag: ExpenseQuickTag;
  sortBy: ExpenseSortOption;
  customStartDate: string;
  customEndDate: string;
}

export interface FilteredExpenseStats {
  totalCount: number;
  filteredCount: number;
  totalAmount: number;
  paidAmount: number;
  plannedAmount: number;
}

export const DEFAULT_EXPENSE_FILTER_STATE: ExpenseFilterState = {
  search: '',
  status: 'all',
  categoryId: 'all',
  partnerId: 'all',
  period: 'all',
  quickTag: 'all',
  sortBy: 'date_desc',
  customStartDate: '',
  customEndDate: '',
};

export function applyExpenseFilters(
  expenses: ExpenseDto[],
  state: ExpenseFilterState,
  _categories: ExpenseCategoryDto[]
): ExpenseDto[] {
  const search = state.search.trim().toLocaleLowerCase('tr-TR');
  const range = resolveDateRange(state);

  return expenses.filter((expense) => {
    if (search) {
      const haystack = [expense.name, expense.categoryName, expense.note]
        .filter(Boolean)
        .join(' ')
        .toLocaleLowerCase('tr-TR');
      if (!haystack.includes(search)) return false;
    }

    const paid = expense.status === 'Paid' || expense.status === 1;
    if (state.status === 'paid' && !paid) return false;
    if (state.status === 'planned' && paid) return false;

    if (state.categoryId === 'uncategorized' && expense.categoryId) return false;
    if (state.categoryId !== 'all' && state.categoryId !== 'uncategorized' && expense.categoryId !== state.categoryId) return false;

    if (state.partnerId !== 'all' && state.partnerId !== 'mine') {
      const isRelated = expense.paidByPartnerId === state.partnerId
        || expense.payments.some((payment) => payment.partnerId === state.partnerId)
        || expense.shareLines.some((share) => share.partnerId === state.partnerId);
      if (!isRelated) return false;
    }

    if (state.quickTag === 'recurring' && !expense.recurrenceId) return false;

    const occurredOn = String(expense.occurredOn).slice(0, 10);
    if (range.start && occurredOn < range.start) return false;
    if (range.end && occurredOn > range.end) return false;
    return true;
  });
}

export function sortExpenses(expenses: ExpenseDto[], sortBy: ExpenseSortOption): ExpenseDto[] {
  return [...expenses].sort((left, right) => {
    switch (sortBy) {
      case 'date_asc':
        return String(left.occurredOn).localeCompare(String(right.occurredOn));
      case 'amount_desc':
        return Number(right.totalAmount) - Number(left.totalAmount);
      case 'amount_asc':
        return Number(left.totalAmount) - Number(right.totalAmount);
      case 'name_asc':
        return left.name.localeCompare(right.name, 'tr-TR');
      case 'date_desc':
      default:
        return String(right.occurredOn).localeCompare(String(left.occurredOn));
    }
  });
}

function resolveDateRange(state: ExpenseFilterState): { start: string | null; end: string | null } {
  const today = new Date();
  const toIsoDate = (date: Date) => {
    const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
    return offsetDate.toISOString().slice(0, 10);
  };
  const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
  const lastOfMonth = new Date(today.getFullYear(), today.getMonth() + 1, 0);

  switch (state.period) {
    case 'this_month':
      return { start: toIsoDate(firstOfMonth), end: toIsoDate(lastOfMonth) };
    case 'last_month': {
      const start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
      return { start: toIsoDate(start), end: toIsoDate(new Date(today.getFullYear(), today.getMonth(), 0)) };
    }
    case 'last_30_days': {
      const start = new Date(today);
      start.setDate(today.getDate() - 29);
      return { start: toIsoDate(start), end: toIsoDate(today) };
    }
    case 'last_3_months': {
      const start = new Date(today);
      start.setMonth(today.getMonth() - 3);
      return { start: toIsoDate(start), end: toIsoDate(today) };
    }
    case 'this_year':
      return { start: `${today.getFullYear()}-01-01`, end: `${today.getFullYear()}-12-31` };
    case 'custom':
      return { start: state.customStartDate || null, end: state.customEndDate || null };
    default:
      return { start: null, end: null };
  }
}

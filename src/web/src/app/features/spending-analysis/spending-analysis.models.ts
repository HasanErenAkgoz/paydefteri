export interface SpendingCurrencyTotals {
  currency: string;
  spending: number;
  refunds: number;
  net: number;
}

export interface SpendingStatementListItem {
  id: string;
  periodStart: string;
  periodEnd: string;
  sourceFileName: string;
  sourceKind: string;
  transactionCount: number;
  totals: SpendingCurrencyTotals[];
  createdAtUtc: string;
}

export interface SpendingTransaction {
  id: string;
  occurredOn: string;
  description: string;
  merchantName: string;
  amount: number;
  currency: string;
  isRefund: boolean;
  installmentCurrent: number | null;
  installmentTotal: number | null;
  category: string;
}

export interface SpendingCategoryMetric {
  categoryCode: string;
  amount: number;
  percentage: number;
  currency: string;
  transactionCount: number;
}

export interface SpendingDashboardCurrency {
  currency: string;
  spending: number;
  transactionCount: number;
  topCategory: string | null;
  largestPurchase: { date: string; merchantName: string; amount: number } | null;
  dailyAverage: number;
  necessityRatio: number;
  categories: Array<{ category: string; amount: number; transactionCount: number; percentage: number }>;
  dailySeries: Array<{ date: string; amount: number }>;
}

export interface SpendingDashboard {
  statementId: string;
  currencies: SpendingDashboardCurrency[];
}

export interface MerchantPattern {
  currency: string;
  merchantName: string;
  transactionCount: number;
  totalAmount: number;
}

export interface SpendingPatterns {
  statementId: string;
  previousStatementComparison: {
    previousStatementId: string;
    currencies: Array<{ currency: string; currentSpending: number; previousSpending: number; changeAmount: number; changePercentage: number | null }>;
  } | null;
  recurringCandidates: MerchantPattern[];
  smallFrequentClusters: MerchantPattern[];
  activity: Array<{
    currency: string;
    weekdaySpending: number;
    weekendSpending: number;
    topDay: { date: string; amount: number } | null;
    topMerchants: MerchantPattern[];
  }>;
  positiveFindings: string[];
}

export interface SpendingBudgetStatus {
  statementId: string;
  categories: Array<{
    category: string;
    currency: string;
    budget: number;
    spent: number;
    projected: number;
    healthScore: number;
    explanation: string;
  }>;
}

export interface SpendingBudget {
  id: string;
  month: string;
  category: string;
  currency: string;
  amount: number;
}

export interface SpendingCoachResponse {
  answer: string;
  insights: string[];
  recommendations: string[];
  evidence: Record<string, number>;
  evidenceKeys: string[];
  disclaimer: string;
  dataAvailable: boolean;
  aiGenerated: boolean;
}

export interface SpendingStatementDetail {
  id: string;
  sourceFileName: string;
  sourceKind: string;
  periodStart: string | null;
  periodEnd: string | null;
  totals: SpendingCurrencyTotals[];
  transactions: SpendingTransaction[];
  warnings: string[];
  createdAtUtc: string;
}

export const SPENDING_CATEGORIES = [
  'Market',
  'Restoran / Yemek',
  'Yemek Siparişi',
  'Akaryakıt',
  'Ulaşım / Taksi',
  'Araç',
  'Giyim',
  'Elektronik',
  'Eğlence',
  'Oyun',
  'Dijital Abonelikler',
  'Telefon / İnternet',
  'Elektrik / Su / Doğalgaz',
  'Sağlık',
  'Eğitim',
  'Seyahat',
  'Otel',
  'Online Alışveriş',
  'Ev / Mobilya',
  'Kişisel Bakım',
  'Sigorta',
  'Vergi',
  'Diğer',
] as const;

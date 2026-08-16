/** Matches Application Common Models (camelCase JSON). */

export type ShareType = 'Default' | 'Equal' | 'Custom' | 0 | 1 | 2;
export type PlanType = 'Installment' | 'Expense' | 0 | 1;
export type ExpenseStatus = 'Planned' | 'Paid' | 0 | 1;
export type RecurrenceFrequency = 'Monthly' | 'Weekly' | 'Yearly' | 0 | 1 | 2;
export type InstallmentStatus = 'Pending' | 'Partial' | 'Full' | 0 | 1 | 2;
export type BulkIncreaseType = 'Percent' | 'Fixed' | 0 | 1;
export type IbanMode = 'None' | 'Plan' | 'Partner' | 0 | 1 | 2;
export type PaymentReviewStatus = 'None' | 'Pending' | 'Approved' | 'Rejected' | 0 | 1 | 2 | 3;

export interface LoginResult {
  accessToken: string;
  expiresAt: string;
}

/** Register now returns a session JWT (same shape as login). */
export type RegisterResult = LoginResult;

export interface UserProfileDto {
  userId: string;
  email: string;
  displayName: string;
}

export interface PlanDto {
  id: string;
  planType?: PlanType;
  title: string;
  description: string;
  deliveryInstallmentId: string | null;
  createdAtUtc: string;
  requireReceipt: boolean;
  ibanMode: IbanMode;
  settlementIban: string | null;
  remindersEnabled?: boolean;
  reminderDaysBefore?: number[];
  reminderDaysAfter?: number[];
  isArchived?: boolean;
}

export interface PartnerDto {
  id: string;
  planId: string;
  name: string;
  color: string;
  defaultPct: number;
  sortOrder: number;
  linkedUserId: string | null;
  iban: string | null;
  inviteEmail?: string | null;
}

export interface CustomShareDto {
  partnerId: string;
  amount: number;
}

export interface PaymentDto {
  partnerId: string;
  isPaid: boolean;
  paidAt: string | null;
  paidByPartnerId: string | null;
  note: string;
  hasReceipt: boolean;
  reviewStatus?: PaymentReviewStatus;
}

export interface InstallmentDto {
  id: string;
  planId: string;
  name: string;
  dueDate: string;
  totalAmount: number;
  shareType: ShareType;
  sortOrder: number;
  customShares: CustomShareDto[];
  payments: PaymentDto[];
}

export interface PartnerPaymentStatusDto {
  partnerId: string;
  partnerName: string;
  shareAmount: number;
  isPaid: boolean;
  paidAt: string | null;
  paidByPartnerId: string | null;
  note: string;
  hasReceipt: boolean;
  reviewStatus?: PaymentReviewStatus;
}

export interface DashboardInstallmentDto {
  id: string;
  name: string;
  dueDate: string;
  totalAmount: number;
  shareType: ShareType;
  status: InstallmentStatus;
  sortOrder: number;
  partnerPayments: PartnerPaymentStatusDto[];
}

export interface PartnerSummaryDto {
  partnerId: string;
  name: string;
  color: string;
  totalShare: number;
  paidAmount: number;
  remainingAmount: number;
  iban: string | null;
}

export interface SettlementBalanceDto {
  partnerId: string;
  partnerName: string;
  balance: number;
}

export interface DashboardMetricsDto {
  grandTotal: number;
  grandPaid: number;
  grandRemaining: number;
  paidPercent: number;
}

export interface MyShareMetricsDto {
  remainingAmount: number;
  paidAmount: number;
  totalShare: number;
  unpaidInstallmentCount: number;
  nextDueDate: string | null;
  nextInstallmentName: string | null;
}

export interface DashboardDto {
  planId: string;
  title: string;
  description: string;
  deliveryInstallmentId: string | null;
  daysUntilDelivery: number | null;
  myPartnerId: string | null;
  isOwner: boolean;
  requireReceipt: boolean;
  ibanMode: IbanMode;
  settlementIban: string | null;
  paymentTargetIban: string | null;
  metrics: DashboardMetricsDto;
  partners: PartnerSummaryDto[];
  settlements: SettlementBalanceDto[];
  installments: DashboardInstallmentDto[];
  myMetrics?: MyShareMetricsDto | null;
  pendingApprovalCount?: number;
}

export interface PlanMemberDto {
  id: string;
  userId: string;
  email: string | null;
  displayName: string | null;
  role: string;
  partnerId: string | null;
  partnerName: string | null;
}

export interface PlanInviteDto {
  id: string;
  email: string;
  partnerId: string;
  partnerName: string;
  status: string;
  token: string;
  expiresAtUtc: string;
  createdAtUtc: string;
  emailSent?: boolean;
}

export interface InvitePreviewDto {
  token: string;
  email: string;
  partnerName: string;
  planTitle: string;
  status: string;
  expiresAtUtc: string;
  isAcceptable: boolean;
  /** True when invite email already has a PayDefteri account. */
  accountExists?: boolean;
}

export interface PlanExportDto {
  title: string;
  description: string;
  deliveryInstallmentId: string | null;
  partners: PartnerExportDto[];
  installments: InstallmentExportDto[];
}

export interface PartnerExportDto {
  id: string;
  name: string;
  color: string;
  defaultPct: number;
  sortOrder: number;
}

export interface InstallmentExportDto {
  id: string;
  name: string;
  dueDate: string;
  totalAmount: number;
  shareType: string;
  sortOrder: number;
  customShares: CustomShareDto[];
  payments: PaymentDto[];
}

export interface CreatePlanRequest {
  title: string;
  description: string;
  planType?: PlanType;
}

export interface ExpenseCategoryDto {
  id: string;
  planId: string;
  name: string;
  color: string;
  sortOrder: number;
}

export interface ExpenseShareLineDto {
  partnerId: string;
  partnerName: string;
  shareAmount: number;
}

export interface ExpensePaymentDto {
  partnerId: string;
  amount: number;
}

export interface ExpenseDto {
  id: string;
  planId: string;
  categoryId: string | null;
  categoryName: string | null;
  recurrenceId: string | null;
  name: string;
  occurredOn: string;
  totalAmount: number;
  shareType: ShareType;
  status: ExpenseStatus;
  paidByPartnerId: string | null;
  note: string;
  customShares: CustomShareDto[];
  shareLines: ExpenseShareLineDto[];
  payments: ExpensePaymentDto[];
  canManage: boolean;
}

export interface ExpenseRecurrenceDto {
  id: string;
  planId: string;
  categoryId: string | null;
  name: string;
  totalAmount: number;
  shareType: ShareType;
  defaultPaidByPartnerId: string | null;
  frequency: RecurrenceFrequency;
  anchorDay: number;
  startDate: string;
  endDate: string | null;
  nextOccurrence: string;
  isActive: boolean;
  note: string;
  customShares: CustomShareDto[];
}

export interface SettlementTransferDto {
  id: string;
  planId: string;
  fromPartnerId: string;
  fromPartnerName: string;
  toPartnerId: string;
  toPartnerName: string;
  amount: number;
  transferredOn: string;
  note: string;
}

export interface ExpenseBalanceDto {
  partnerId: string;
  partnerName: string;
  color: string;
  balance: number;
}

export interface ExpenseBoardDto {
  plan: PlanDto;
  balances: ExpenseBalanceDto[];
  expenses: ExpenseDto[];
  categories: ExpenseCategoryDto[];
  recurrences: ExpenseRecurrenceDto[];
  transfers: SettlementTransferDto[];
  /** True when the current user owns the plan (structure edits allowed). */
  isOwner?: boolean;
}

export interface PagedExpenseDto {
  items: ExpenseDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ExpenseRequest {
  name: string;
  occurredOn: string;
  totalAmount: number;
  shareType: ShareType;
  status: ExpenseStatus;
  paidByPartnerId: string | null;
  categoryId: string | null;
  note?: string;
  customShares?: CustomShareDto[];
  payments?: ExpensePaymentDto[];
  installmentCount?: number;
}

export interface ExpenseReceiptDraftDto {
  name: string | null;
  totalAmount: number | null;
  occurredOn: string | null;
  categoryId: string | null;
  categoryName: string | null;
  installmentCount: number | null;
  documentNumber: string | null;
  note: string | null;
  confidence: number;
  lowConfidenceFields: string[];
  warnings: string[];
}

export interface ExpenseRecurrenceRequest {
  name: string;
  totalAmount: number;
  shareType: ShareType;
  categoryId: string | null;
  defaultPaidByPartnerId: string | null;
  frequency: RecurrenceFrequency;
  anchorDay: number;
  startDate: string;
  endDate: string | null;
  note?: string;
  customShares?: CustomShareDto[];
}

export interface SettlementTransferRequest {
  fromPartnerId: string;
  toPartnerId: string;
  amount: number;
  transferredOn: string;
  note?: string;
}

export interface UpdatePlanRequest {
  title: string;
  description: string;
  deliveryInstallmentId: string | null;
  requireReceipt: boolean;
  ibanMode: IbanMode;
  settlementIban: string | null;
  remindersEnabled: boolean;
  reminderDaysBefore: number[];
  reminderDaysAfter: number[];
}

export interface PartnerRequest {
  name: string;
  color: string;
  defaultPct: number;
  sortOrder: number;
  iban: string | null;
  inviteEmail?: string | null;
}

export interface InstallmentRequest {
  name: string;
  dueDate: string;
  totalAmount: number;
  shareType: number;
  sortOrder: number;
  customShares?: CustomShareDto[] | null;
}

export interface PaymentRequest {
  isPaid: boolean;
  paidAt: string | null;
  paidByPartnerId: string | null;
  note: string | null;
}

export interface BulkIncreaseRequest {
  fromInstallmentId: string;
  type: number;
  value: number;
}

export interface TemplateListItemDto {
  key: string;
  title: string;
  description: string;
}

export interface TemplateInstallmentPreviewDto {
  index: number;
  name: string;
  dueDate: string;
  totalAmount: number;
  perPartnerAmount: number;
}

export interface TemplatePartnerPreviewDto {
  name: string;
  color: string;
  defaultPct: number;
}

export interface PlanTemplatePreviewDto {
  key: string;
  title: string;
  description: string;
  grandTotal: number;
  installmentCount: number;
  deliveryName: string | null;
  deliveryIndex: number;
  partnerCount: number;
  partners: TemplatePartnerPreviewDto[];
  installments: TemplateInstallmentPreviewDto[];
}

export interface PlanDocumentPreviewDto {
  sourceFileName: string;
  sourceKind: string;
  title: string;
  description: string;
  grandTotal: number;
  installmentCount: number;
  deliveryName: string | null;
  deliveryIndex: number;
  warnings: string[];
  partners: TemplatePartnerPreviewDto[];
  installments: TemplateInstallmentPreviewDto[];
}

export interface ReminderHistoryItemDto {
  id: string;
  installmentId: string;
  installmentName: string;
  partnerId: string | null;
  partnerName: string | null;
  kind: string;
  offsetDays: number;
  sentOn: string;
  createdAtUtc: string;
}

export interface ReportPartnerBarDto {
  partnerId: string;
  name: string;
  color: string;
  paidAmount: number;
  remainingAmount: number;
  totalShare: number;
}

export interface ReportMonthDto {
  yearMonth: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  installmentCount: number;
}

export interface ReportSummaryDto {
  partners: ReportPartnerBarDto[];
  months: ReportMonthDto[];
  metrics: DashboardMetricsDto;
}

export interface PlanActivityItemDto {
  id: string;
  type: string;
  message: string;
  actorDisplayName: string;
  createdAtUtc: string;
}

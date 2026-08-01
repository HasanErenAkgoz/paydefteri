/** Matches Application Common Models (camelCase JSON). */

export type ShareType = 'Default' | 'Equal' | 'Custom' | 0 | 1 | 2;
export type InstallmentStatus = 'Pending' | 'Partial' | 'Full' | 0 | 1 | 2;
export type BulkIncreaseType = 'Percent' | 'Fixed' | 0 | 1;
export type IbanMode = 'None' | 'Plan' | 'Partner' | 0 | 1 | 2;
export type PaymentReviewStatus = 'None' | 'Pending' | 'Approved' | 'Rejected' | 0 | 1 | 2 | 3;

export interface LoginResult {
  accessToken: string;
  expiresAt: string;
}

export interface RegisterResult {
  userId: string;
  email: string;
  displayName: string;
}

export interface UserProfileDto {
  userId: string;
  email: string;
  displayName: string;
}

export interface PlanDto {
  id: string;
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

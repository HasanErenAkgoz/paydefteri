/** Matches Application Common Models (camelCase JSON). */

export type ShareType = 'Default' | 'Equal' | 'Custom' | 0 | 1 | 2;
export type InstallmentStatus = 'Pending' | 'Partial' | 'Full' | 0 | 1 | 2;
export type BulkIncreaseType = 'Percent' | 'Fixed' | 0 | 1;

export interface LoginResult {
  accessToken: string;
  expiresAt: string;
}

export interface RegisterResult {
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
}

export interface PartnerDto {
  id: string;
  planId: string;
  name: string;
  color: string;
  defaultPct: number;
  sortOrder: number;
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

export interface DashboardDto {
  planId: string;
  title: string;
  description: string;
  deliveryInstallmentId: string | null;
  daysUntilDelivery: number | null;
  metrics: DashboardMetricsDto;
  partners: PartnerSummaryDto[];
  settlements: SettlementBalanceDto[];
  installments: DashboardInstallmentDto[];
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
}

export interface PartnerRequest {
  name: string;
  color: string;
  defaultPct: number;
  sortOrder: number;
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

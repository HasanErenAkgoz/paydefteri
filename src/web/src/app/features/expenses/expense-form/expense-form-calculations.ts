export interface InstallmentPreview {
  count: number;
  baseAmount: number;
  finalAmount: number;
}

/** Returns a rounded installment preview while preserving the exact total in the final installment. */
export function getInstallmentPreview(amount: number, count: number): InstallmentPreview | null {
  if (!(amount > 0) || !Number.isInteger(count) || count < 2 || count > 120) {
    return null;
  }

  const baseAmount = Math.floor((amount / count) * 100) / 100;
  const finalAmount = Math.round((amount - baseAmount * (count - 1)) * 100) / 100;
  return { count, baseAmount, finalAmount };
}

export function amountsMatchTotal(amounts: Record<string, number>, expectedTotal: number): boolean {
  const total = Object.values(amounts).reduce((sum, amount) => sum + (Number(amount) || 0), 0);
  return Math.abs(total - expectedTotal) <= 0.01;
}

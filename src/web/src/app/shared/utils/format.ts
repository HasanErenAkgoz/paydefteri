const currency = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatTry(value: number): string {
  return currency.format(value);
}

export function formatDateTr(isoDate: string | null | undefined): string {
  if (!isoDate) {
    return '—';
  }
  const d = new Date(isoDate.includes('T') ? isoDate : `${isoDate}T00:00:00`);
  if (Number.isNaN(d.getTime())) {
    return isoDate;
  }
  return d.toLocaleDateString('tr-TR');
}

export function statusLabel(status: string | number): string {
  const key = String(status);
  switch (key) {
    case '0':
    case 'Pending':
      return 'Bekliyor';
    case '1':
    case 'Partial':
      return 'Kısmi';
    case '2':
    case 'Full':
      return 'Ödendi';
    default:
      return key;
  }
}

export function shareTypeLabel(shareType: string | number): string {
  const key = String(shareType);
  switch (key) {
    case '0':
    case 'Default':
      return 'Varsayılan %';
    case '1':
    case 'Equal':
      return 'Eşit';
    case '2':
    case 'Custom':
      return 'Özel';
    default:
      return key;
  }
}

export function shareTypeToNumber(shareType: string | number): number {
  if (typeof shareType === 'number') {
    return shareType;
  }
  switch (shareType) {
    case 'Equal':
      return 1;
    case 'Custom':
      return 2;
    default:
      return 0;
  }
}

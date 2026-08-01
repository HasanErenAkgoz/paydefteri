/** Turkish money helpers: 1.500.000,50 ↔ number */

export function formatMoneyTr(value: number | null | undefined, maxFractionDigits = 2): string {
  if (value == null || !Number.isFinite(value)) {
    return '';
  }
  return new Intl.NumberFormat('tr-TR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: maxFractionDigits,
  }).format(value);
}

/** Parse TR-formatted money text (dots thousands, comma decimal) into a number. */
export function parseMoneyTr(raw: string | null | undefined): number | null {
  if (raw == null) {
    return null;
  }
  const trimmed = String(raw).trim();
  if (!trimmed) {
    return null;
  }
  const normalized = trimmed.replace(/\s/g, '').replace(/\./g, '').replace(',', '.');
  if (!/^-?\d+(\.\d+)?$/.test(normalized)) {
    return null;
  }
  const n = Number(normalized);
  return Number.isFinite(n) ? n : null;
}

/**
 * Live-format user input as TR money while typing.
 * Keeps an optional decimal part after `,`.
 */
export function formatMoneyInputLive(raw: string): { display: string; value: number } {
  const negative = raw.trim().startsWith('-');
  const body = raw.replace(/[^\d,]/g, '');
  const commaIdx = body.indexOf(',');
  let intDigits: string;
  let fracDigits: string | undefined;

  if (commaIdx >= 0) {
    intDigits = body.slice(0, commaIdx).replace(/\D/g, '');
    fracDigits = body.slice(commaIdx + 1).replace(/\D/g, '').slice(0, 2);
  } else {
    intDigits = body.replace(/\D/g, '');
  }

  if (intDigits.length > 1) {
    intDigits = intDigits.replace(/^0+(?=\d)/, '');
  }

  const withDots = intDigits.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  let display = fracDigits !== undefined ? `${withDots},${fracDigits}` : withDots;
  if (negative && display) {
    display = `-${display}`;
  }

  const intNum = intDigits || '0';
  const frac = fracDigits !== undefined ? fracDigits.padEnd(1, '0') : '0';
  const value = Number(`${negative ? '-' : ''}${intNum}.${frac}`);

  return {
    display,
    value: Number.isFinite(value) ? value : 0,
  };
}

/** Client-side ICS / CSV helpers (HTML prototype parity). */

export interface IcsInstallmentRow {
  name: string;
  dueDate: string;
  totalAmount: number;
}

export function buildIcsCalendar(planTitle: string, installments: IcsInstallmentRow[]): string {
  let ics =
    'BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//PayDefteri//TR\r\nCALSCALE:GREGORIAN\r\nMETHOD:PUBLISH\r\n';

  for (const inst of installments) {
    const dt = toIcsDate(inst.dueDate);
    if (!dt) {
      continue;
    }
    const amount = formatTryPlain(inst.totalAmount);
    ics += 'BEGIN:VEVENT\r\n';
    ics += `SUMMARY:${escapeIcs(inst.name)} - ${amount}\r\n`;
    ics += `DESCRIPTION:${escapeIcs(planTitle)} kapsamında ödenmesi gereken son vade tarihi.\\nToplam Tutar: ${amount}\r\n`;
    ics += `DTSTART;VALUE=DATE:${dt}\r\n`;
    ics += `DTEND;VALUE=DATE:${dt}\r\n`;
    ics += 'STATUS:CONFIRMED\r\n';
    ics += 'END:VEVENT\r\n';
  }

  ics += 'END:VCALENDAR\r\n';
  return ics;
}

export function downloadTextFile(content: string, filename: string, mime: string): void {
  const blob = new Blob([content], { type: mime });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

export function downloadIcs(planTitle: string, installments: IcsInstallmentRow[], filename?: string): void {
  const ics = buildIcsCalendar(planTitle, installments);
  const name = filename ?? `taksit_takvimi_${new Date().toISOString().slice(0, 10)}.ics`;
  downloadTextFile(ics, name, 'text/calendar;charset=utf-8;');
}

export interface CsvExpenseRow {
  name: string;
  occurredOn: string;
  category: string;
  totalAmount: number;
  status: string;
  paidBy: string;
  shares: string;
  note: string;
}

export function downloadExpenseCsv(rows: CsvExpenseRow[], filename?: string): void {
  let csv = 'Ad,Tarih,Kategori,Tutar,Durum,Odeyen,Paylar,Not\n';
  for (const row of rows) {
    csv += [
      csvEscape(row.name),
      csvEscape(row.occurredOn),
      csvEscape(row.category),
      row.totalAmount,
      csvEscape(row.status),
      csvEscape(row.paidBy),
      csvEscape(row.shares),
      csvEscape(row.note),
    ].join(',');
    csv += '\n';
  }
  const name = filename ?? `giderler_${new Date().toISOString().slice(0, 10)}.csv`;
  downloadTextFile('\uFEFF' + csv, name, 'text/csv;charset=utf-8;');
}

export interface CsvPartnerColumn {
  name: string;
  share: number;
  isPaid: boolean;
  paidByName: string;
  note: string;
}

export interface CsvInstallmentRow {
  id: string;
  name: string;
  dueDate: string;
  totalAmount: number;
  partners: CsvPartnerColumn[];
}

export function buildCsv(partnerNames: string[], rows: CsvInstallmentRow[]): string {
  let csv = 'ID,Taksit Adi,Tarih,Toplam Tutar';
  for (const name of partnerNames) {
    csv += `,${csvEscape(name)} Payi,${csvEscape(name)} Odeme Durumu,${csvEscape(name)} Odeyen Kisi,${csvEscape(name)} Dekont Notu`;
  }
  csv += '\n';

  for (const row of rows) {
    csv += `${csvEscape(row.id)},${csvEscape(row.name)},${csvEscape(row.dueDate)},${row.totalAmount}`;
    for (const p of row.partners) {
      const status = p.isPaid ? 'ODENDI' : 'BEKLIYOR';
      csv += `,${p.share},${status},${csvEscape(p.paidByName)},${csvEscape(p.note)}`;
    }
    csv += '\n';
  }
  return csv;
}

export function downloadCsv(partnerNames: string[], rows: CsvInstallmentRow[], filename?: string): void {
  const csv = '\ufeff' + buildCsv(partnerNames, rows);
  const name = filename ?? `borc_taksit_takip_${new Date().toISOString().slice(0, 10)}.csv`;
  downloadTextFile(csv, name, 'text/csv;charset=utf-8;');
}

function toIcsDate(iso: string): string | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  if (!m) {
    return null;
  }
  return `${m[1]}${m[2]}${m[3]}`;
}

function escapeIcs(value: string): string {
  return value.replace(/\\/g, '\\\\').replace(/;/g, '\\;').replace(/,/g, '\\,').replace(/\n/g, '\\n');
}

function csvEscape(value: string): string {
  const s = String(value ?? '');
  if (/[",\n]/.test(s)) {
    return `"${s.replace(/"/g, '""')}"`;
  }
  return `"${s}"`;
}

function formatTryPlain(value: number): string {
  return (
    value.toLocaleString('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 }) + ' ₺'
  );
}

import { Injectable } from '@angular/core';
import { formatTry } from '../../shared/utils/format';

/** WhatsApp paylaşımı henüz yayında değil; arayüz "çok yakında" olarak işaretlenir. */
export const WHATSAPP_SHARE_ENABLED = false;

export interface InstallmentShareData {
  planTitle: string;
  installmentName: string;
  installmentNumber: number;
  dueDate: string;
  shareAmount: number;
  totalAmount: number;
  unpaidCount?: number;
}

@Injectable({ providedIn: 'root' })
export class ShareService {
  shareViaWhatsapp(data: InstallmentShareData): boolean {
    if (!WHATSAPP_SHARE_ENABLED) {
      return false;
    }

    const text = this.buildInstallmentMessage(data);
    const encoded = encodeURIComponent(text);
    const url = `https://wa.me/?text=${encoded}`;
    
    if (typeof window !== 'undefined') {
      window.open(url, '_blank');
    }

    return true;
  }

  async copyToClipboard(data: InstallmentShareData): Promise<boolean> {
    const text = this.buildInstallmentMessage(data);
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch {
        return false;
      }
    }
    return false;
  }

  private buildInstallmentMessage(data: InstallmentShareData): string {
    const formattedAmount = formatTry(data.shareAmount);
    return `🏠 *PayDefteri Hatırlatması: ${data.planTitle}*

📅 *Vade Tarihi:* ${data.dueDate}
🔢 *Taksit:* #${data.installmentNumber} - ${data.installmentName}
💰 *Ödenecek Pay:* ${formattedAmount}

Lütfen ödeme yaptıktan sonra deftere işlemeyi veya dekontu yüklemeyi unutma! 👍
🔗 https://paydefteri.com`;
  }
}

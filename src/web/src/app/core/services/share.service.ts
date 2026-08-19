import { Injectable } from '@angular/core';
import { formatTry } from '../../shared/utils/format';

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
  shareViaWhatsapp(data: InstallmentShareData): void {
    const text = this.buildInstallmentMessage(data);
    const encoded = encodeURIComponent(text);
    const url = `https://wa.me/?text=${encoded}`;
    
    if (typeof window !== 'undefined') {
      window.open(url, '_blank');
    }
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

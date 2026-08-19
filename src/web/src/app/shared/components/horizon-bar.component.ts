import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CurrencyTryPipe } from '../pipes/currency-try.pipe';

export interface HorizonInstallmentItem {
  id: string;
  name: string;
  installmentNumber: number;
  dueDate: string;
  amount: number;
  isPaid: boolean;
}

@Component({
  selector: 'app-horizon-bar',
  standalone: true,
  imports: [CommonModule, CurrencyTryPipe],
  template: `
    @if (upcomingItems().length > 0) {
      <div class="horizon-wrapper">
        <div class="horizon-header">
          <span class="horizon-title">📅 Yaklaşan Vade Ufku (3 Aylık)</span>
          <span class="horizon-sub">Tahmini Nakit Akışı</span>
        </div>
        <div class="horizon-cards">
          @for (item of upcomingItems(); track item.id; let idx = $index) {
            <div class="horizon-card" [class.urgent]="idx === 0">
              <div class="h-card-top">
                <span class="h-month-tag">{{ formatMonthYear(item.dueDate) }}</span>
                @if (idx === 0) {
                  <span class="urgent-pill">Sıradaki</span>
                }
              </div>
              <div class="h-amount">{{ item.amount | tryCurrency }}</div>
              <div class="h-meta">#{{ item.installmentNumber }} · {{ item.name }}</div>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .horizon-wrapper {
      background: var(--bg-card, #131b2e);
      border: 1px solid rgba(99, 102, 241, 0.2);
      border-radius: 16px;
      padding: 12px 14px;
      margin-bottom: 12px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
    }

    .horizon-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 10px;
    }

    .horizon-title {
      font-size: 0.76rem;
      font-weight: 800;
      color: #fff;
      text-transform: uppercase;
      letter-spacing: 0.4px;
    }

    .horizon-sub {
      font-size: 0.68rem;
      color: var(--text-muted, #94a3b8);
    }

    .horizon-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
      gap: 8px;
    }

    .horizon-card {
      background: rgba(15, 23, 42, 0.6);
      border: 1px solid rgba(148, 163, 184, 0.12);
      border-radius: 12px;
      padding: 8px 10px;
      display: flex;
      flex-direction: column;
      gap: 3px;
    }

    .horizon-card.urgent {
      background: linear-gradient(135deg, rgba(245, 158, 11, 0.12), rgba(15, 23, 42, 0.8));
      border-color: rgba(245, 158, 11, 0.35);
    }

    .h-card-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .h-month-tag {
      font-size: 0.68rem;
      font-weight: 700;
      color: #818cf8;
      text-transform: uppercase;
    }

    .urgent-pill {
      font-size: 0.6rem;
      font-weight: 800;
      padding: 1px 5px;
      border-radius: 8px;
      background: rgba(245, 158, 11, 0.25);
      color: #fbbf24;
    }

    .h-amount {
      font-family: var(--font-display, 'Outfit', sans-serif);
      font-size: 0.92rem;
      font-weight: 800;
      color: #fff;
      margin: 2px 0;
    }

    .h-meta {
      font-size: 0.65rem;
      color: var(--text-muted, #94a3b8);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
  `]
})
export class HorizonBarComponent {
  readonly items = input<HorizonInstallmentItem[]>([]);

  readonly upcomingItems = computed(() => {
    return (this.items() || [])
      .filter(i => !i.isPaid)
      .slice(0, 3);
  });

  formatMonthYear(isoString: string): string {
    if (!isoString) return '';
    try {
      const date = new Date(isoString);
      return date.toLocaleDateString('tr-TR', { month: 'short', year: 'numeric' });
    } catch {
      return isoString;
    }
  }
}

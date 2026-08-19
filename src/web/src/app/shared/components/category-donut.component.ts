import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CurrencyTryPipe } from '../pipes/currency-try.pipe';

export interface DonutCategory {
  label: string;
  amount: number;
  color: string;
}

@Component({
  selector: 'app-category-donut',
  standalone: true,
  imports: [CommonModule, CurrencyTryPipe],
  template: `
    @if (total() > 0) {
      <div class="donut-wrapper">
        <div class="donut-chart-container">
          <svg viewBox="0 0 100 100" class="donut-svg">
            <!-- Background circle -->
            <circle cx="50" cy="50" r="38" class="donut-track" />
            
            <!-- Category Slices -->
            @for (slice of slices(); track slice.label) {
              <circle
                cx="50"
                cy="50"
                r="38"
                class="donut-segment"
                [attr.stroke]="slice.color"
                [attr.stroke-dasharray]="slice.dashArray"
                [attr.stroke-dashoffset]="slice.dashOffset"
              />
            }
          </svg>
          <div class="donut-center-info">
            <span class="center-sub">Toplam</span>
            <span class="center-val">{{ total() | tryCurrency }}</span>
          </div>
        </div>

        <div class="donut-legend">
          @for (cat of categories(); track cat.label) {
            <div class="legend-item">
              <div class="legend-left">
                <span class="dot" [style.background]="cat.color"></span>
                <span class="label">{{ cat.label }}</span>
              </div>
              <div class="legend-right">
                <span class="amount">{{ cat.amount | tryCurrency }}</span>
                <span class="pct">%{{ ((cat.amount / total()) * 100).toFixed(0) }}</span>
              </div>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .donut-wrapper {
      background: var(--bg-card, #131b2e);
      border: 1px solid rgba(99, 102, 241, 0.2);
      border-radius: 16px;
      padding: 14px;
      margin-bottom: 12px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .donut-chart-container {
      position: relative;
      width: 140px;
      height: 140px;
      margin: 0 auto;
    }

    .donut-svg {
      width: 100%;
      height: 100%;
      transform: rotate(-90deg);
    }

    .donut-track {
      fill: transparent;
      stroke: rgba(148, 163, 184, 0.1);
      stroke-width: 14;
    }

    .donut-segment {
      fill: transparent;
      stroke-width: 14;
      stroke-linecap: round;
      transition: stroke-dasharray 0.5s ease, stroke-dashoffset 0.5s ease;
    }

    .donut-center-info {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
    }

    .center-sub {
      font-size: 0.65rem;
      font-weight: 700;
      color: var(--text-muted, #94a3b8);
      text-transform: uppercase;
    }

    .center-val {
      font-family: var(--font-display, 'Outfit', sans-serif);
      font-size: 0.88rem;
      font-weight: 800;
      color: #fff;
    }

    .donut-legend {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .legend-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.76rem;
      padding: 4px 6px;
      border-radius: 8px;
      background: rgba(15, 23, 42, 0.4);
    }

    .legend-left {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .label {
      color: #fff;
      font-weight: 600;
    }

    .legend-right {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .amount {
      font-weight: 700;
      color: #fff;
    }

    .pct {
      font-size: 0.68rem;
      color: var(--text-muted, #94a3b8);
      font-weight: 700;
      min-width: 28px;
      text-align: right;
    }
  `]
})
export class CategoryDonutComponent {
  readonly categories = input<DonutCategory[]>([]);

  readonly total = computed(() => {
    return (this.categories() || []).reduce((sum, c) => sum + (c.amount || 0), 0);
  });

  readonly slices = computed(() => {
    const circumference = 2 * Math.PI * 38; // ~238.76
    const totalAmount = this.total();
    if (totalAmount <= 0) return [];

    let accumulatedPct = 0;
    return (this.categories() || []).map(cat => {
      const pct = cat.amount / totalAmount;
      const strokeLength = pct * circumference;
      const dashArray = `${strokeLength} ${circumference - strokeLength}`;
      const dashOffset = -(accumulatedPct * circumference);
      accumulatedPct += pct;
      return {
        label: cat.label,
        color: cat.color,
        dashArray,
        dashOffset
      };
    });
  });
}

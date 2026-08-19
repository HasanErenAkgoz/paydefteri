import { Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-milestone-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="milestone-container" [class]="milestoneClass()">
      <div class="badge-icon">{{ milestoneIcon() }}</div>
      <div class="badge-content">
        <span class="badge-label">{{ milestoneTitle() }}</span>
        <span class="badge-desc">{{ milestoneDescription() }}</span>
      </div>
      <span class="pct-pill">%{{ progressPct().toFixed(0) }}</span>
    </div>
  `,
  styles: [`
    .milestone-container {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 12px;
      border-radius: 14px;
      background: rgba(19, 27, 46, 0.7);
      border: 1px solid rgba(99, 102, 241, 0.2);
      margin-top: 8px;
      transition: all 0.2s ease;
    }

    .milestone-container.level-1 {
      border-color: rgba(99, 102, 241, 0.35);
      background: linear-gradient(135deg, rgba(99, 102, 241, 0.1), rgba(19, 27, 46, 0.9));
    }

    .milestone-container.level-2 {
      border-color: rgba(16, 185, 129, 0.35);
      background: linear-gradient(135deg, rgba(16, 185, 129, 0.1), rgba(19, 27, 46, 0.9));
    }

    .milestone-container.level-3 {
      border-color: rgba(245, 158, 11, 0.35);
      background: linear-gradient(135deg, rgba(245, 158, 11, 0.12), rgba(19, 27, 46, 0.9));
    }

    .milestone-container.level-4 {
      border-color: rgba(234, 179, 8, 0.5);
      background: linear-gradient(135deg, rgba(234, 179, 8, 0.18), rgba(19, 27, 46, 0.95));
      box-shadow: 0 0 16px rgba(234, 179, 8, 0.25);
    }

    .badge-icon {
      font-size: 1.3rem;
      flex-shrink: 0;
    }

    .badge-content {
      display: flex;
      flex-direction: column;
      flex: 1;
    }

    .badge-label {
      font-size: 0.76rem;
      font-weight: 800;
      color: #fff;
      letter-spacing: 0.2px;
    }

    .badge-desc {
      font-size: 0.68rem;
      color: var(--text-muted, #94a3b8);
    }

    .pct-pill {
      font-size: 0.72rem;
      font-weight: 800;
      padding: 3px 8px;
      border-radius: 12px;
      background: rgba(255, 255, 255, 0.1);
      color: #fff;
    }
  `]
})
export class MilestoneBadgeComponent {
  readonly progressPct = input.required<number>();

  readonly milestoneClass = computed(() => {
    const pct = this.progressPct();
    if (pct >= 100) return 'level-4';
    if (pct >= 75) return 'level-3';
    if (pct >= 50) return 'level-2';
    if (pct >= 25) return 'level-1';
    return 'level-0';
  });

  readonly milestoneIcon = computed(() => {
    const pct = this.progressPct();
    if (pct >= 100) return '👑';
    if (pct >= 75) return '🥇';
    if (pct >= 50) return '🥈';
    if (pct >= 25) return '🥉';
    return '🌱';
  });

  readonly milestoneTitle = computed(() => {
    const pct = this.progressPct();
    if (pct >= 100) return 'Borçsuzluk Zaferi!';
    if (pct >= 75) return 'Son Düzlük!';
    if (pct >= 50) return 'Yolun Yarısı Tamam!';
    if (pct >= 25) return 'Çeyrek Yol Aşıldı!';
    return 'Tasarruf Yolculuğu Başladı';
  });

  readonly milestoneDescription = computed(() => {
    const pct = this.progressPct();
    if (pct >= 100) return 'Tüm taksitler başarıyla tamamlandı.';
    if (pct >= 75) return 'Özgürlüğe ve hedefe sadece son adımlar kaldı.';
    if (pct >= 50) return 'Zirve aşıldı, ev/araç teslimatına çok az kaldı.';
    if (pct >= 25) return 'İlk büyük eşik geride kaldı, harika gidiyorsunuz!';
    return 'Düzenli ödemelerle hedefinize adım adım yaklaşıyorsunuz.';
  });
}

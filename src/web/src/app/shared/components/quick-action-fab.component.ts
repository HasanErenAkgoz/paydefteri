import { Component, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-quick-action-fab',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Floating Action Button -->
    <button
      type="button"
      class="fab-btn"
      [class.open]="isOpen()"
      (click)="toggleOpen()"
      title="Hızlı İşlemler"
      aria-label="Hızlı İşlemler Menüsü"
    >
      <span class="fab-icon">{{ isOpen() ? '✕' : '⚡' }}</span>
    </button>

    <!-- Backdrop & Action Sheet -->
    @if (isOpen()) {
      <div class="drawer-backdrop" (click)="close()"></div>
      <div class="bottom-action-sheet">
        <div class="sheet-handle"></div>
        <div class="sheet-header">
          <h3>⚡ Hızlı İşlemler</h3>
          <p>Sık kullanılan finansal aksiyonlar</p>
        </div>

        <div class="sheet-actions">
          <button type="button" class="sheet-btn" (click)="onAction('add-expense')">
            <span class="action-icon">💸</span>
            <div class="action-text">
              <span class="title">Ekstra Gider / Fatura Ekle</span>
              <span class="sub">Ortak masraf veya fatura kaydet</span>
            </div>
          </button>

          <button type="button" class="sheet-btn" (click)="onAction('share-whatsapp')">
            <span class="action-icon">💬</span>
            <div class="action-text">
              <span class="title">WhatsApp ile Durum Paylaş</span>
              <span class="sub">Ortaklara hazır taksit özeti gönder</span>
            </div>
          </button>

          <button type="button" class="sheet-btn" (click)="onAction('settle-up')">
            <span class="action-icon">🔄</span>
            <div class="action-text">
              <span class="title">Mahsuplaşma Hesabı Kapat</span>
              <span class="sub">Ortaklar arası iç bakiyeyi sıfırla</span>
            </div>
          </button>

          <button type="button" class="sheet-btn" (click)="onAction('export-pdf')">
            <span class="action-icon">🖨️</span>
            <div class="action-text">
              <span class="title">Resmi Hesaplaşma PDF'i Al</span>
              <span class="sub">Tüm planın dökümünü indir</span>
            </div>
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .fab-btn {
      position: fixed;
      bottom: 84px;
      right: 18px;
      z-index: 9998;
      width: 52px;
      height: 52px;
      border-radius: 50%;
      border: 1px solid rgba(255, 255, 255, 0.25);
      background: linear-gradient(135deg, #6366f1, #8b5cf6);
      color: #fff;
      font-size: 1.35rem;
      display: grid;
      place-items: center;
      box-shadow: 0 8px 24px rgba(99, 102, 241, 0.45);
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
    }

    .fab-btn:active {
      transform: scale(0.92);
    }

    .fab-btn.open {
      background: #334155;
      transform: rotate(90deg);
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.4);
    }

    .drawer-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.65);
      backdrop-filter: blur(4px);
      -webkit-backdrop-filter: blur(4px);
      z-index: 10000;
      animation: fadeIn 0.2s ease-out;
    }

    .bottom-action-sheet {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      background: #111827;
      border-top: 1px solid rgba(99, 102, 241, 0.25);
      border-radius: 24px 24px 0 0;
      padding: 12px 18px calc(24px + env(safe-area-inset-bottom, 0px));
      z-index: 10001;
      display: flex;
      flex-direction: column;
      gap: 12px;
      box-shadow: 0 -12px 40px rgba(0, 0, 0, 0.8);
      animation: slideUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
    }

    .sheet-handle {
      width: 42px;
      height: 4px;
      border-radius: 4px;
      background: rgba(148, 163, 184, 0.4);
      margin: 0 auto 6px;
    }

    .sheet-header h3 {
      font-family: var(--font-display, 'Outfit', sans-serif);
      font-size: 1.05rem;
      font-weight: 800;
      color: #fff;
    }

    .sheet-header p {
      font-size: 0.72rem;
      color: var(--text-muted, #94a3b8);
    }

    .sheet-actions {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .sheet-btn {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 14px;
      border-radius: 14px;
      border: 1px solid rgba(99, 102, 241, 0.15);
      background: #131b2e;
      color: #fff;
      cursor: pointer;
      text-align: left;
      transition: all 0.15s ease;
    }

    .sheet-btn:active {
      background: rgba(99, 102, 241, 0.25);
      border-color: #6366f1;
    }

    .action-icon {
      font-size: 1.3rem;
      width: 36px;
      height: 36px;
      border-radius: 10px;
      background: rgba(15, 23, 42, 0.8);
      display: grid;
      place-items: center;
      flex-shrink: 0;
    }

    .action-text {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .action-text .title {
      font-size: 0.84rem;
      font-weight: 700;
      color: #fff;
    }

    .action-text .sub {
      font-size: 0.7rem;
      color: var(--text-muted, #94a3b8);
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    @keyframes slideUp {
      from { transform: translateY(100%); }
      to { transform: translateY(0); }
    }
  `]
})
export class QuickActionFabComponent {
  readonly isOpen = signal<boolean>(false);
  readonly actionSelected = output<string>();

  toggleOpen(): void {
    this.isOpen.set(!this.isOpen());
  }

  close(): void {
    this.isOpen.set(false);
  }

  onAction(actionName: string): void {
    this.close();
    this.actionSelected.emit(actionName);
  }
}

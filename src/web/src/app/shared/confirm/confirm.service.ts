import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
  /** Positive/info confirm (green) — e.g. template load. */
  success?: boolean;
}

interface ConfirmState extends ConfirmOptions {
  resolve: (value: boolean) => void;
}

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly state = signal<ConfirmState | null>(null);

  ask(options: ConfirmOptions): Promise<boolean> {
    return new Promise((resolve) => {
      // Defer open so the triggering click cannot land on the new backdrop and auto-cancel.
      setTimeout(() => {
        const current = this.state();
        if (current) {
          current.resolve(false);
        }
        this.state.set({
          title: options.title,
          message: options.message,
          confirmLabel: options.confirmLabel ?? 'Onayla',
          cancelLabel: options.cancelLabel ?? 'Vazgeç',
          danger: options.danger ?? false,
          success: options.success ?? false,
          resolve,
        });
      }, 0);
    });
  }

  respond(value: boolean): void {
    const current = this.state();
    if (!current) {
      return;
    }
    this.state.set(null);
    current.resolve(value);
  }
}

import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface ToastItem {
  id: number;
  kind: ToastKind;
  title: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  readonly items = signal<ToastItem[]>([]);

  success(message: string, title = 'Başarılı'): void {
    this.push('success', title, message);
  }

  error(message: string, title = 'Hata'): void {
    this.push('error', title, message);
  }

  info(message: string, title = 'Bilgi'): void {
    this.push('info', title, message);
  }

  dismiss(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
    this.items.update((list) => list.filter((t) => t.id !== id));
  }

  private push(kind: ToastKind, title: string, message: string): void {
    const text = (message || '').trim();
    if (!text) {
      return;
    }
    const id = this.nextId++;
    const item: ToastItem = { id, kind, title, message: text };
    this.items.update((list) => [...list, item].slice(-4));
    const ms = kind === 'error' ? 6500 : 4200;
    const timer = setTimeout(() => this.dismiss(id), ms);
    this.timers.set(id, timer);
  }
}

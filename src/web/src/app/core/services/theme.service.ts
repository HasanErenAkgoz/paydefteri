import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';

export type AppTheme = 'slate' | 'classic';

export interface ThemeOption {
  id: AppTheme;
  label: string;
  hint: string;
}

const STORAGE_KEY = 'paydefteri.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly options: ThemeOption[] = [
    { id: 'slate', label: 'Slate', hint: 'Login teması — çelik mavi & amber' },
    { id: 'classic', label: 'Klasik', hint: 'Önceki indigo görünüm' },
  ];

  readonly theme = signal<AppTheme>(this.readInitial());

  constructor() {
    this.apply(this.theme());
  }

  setTheme(theme: AppTheme): void {
    if (theme !== 'slate' && theme !== 'classic') {
      return;
    }
    this.theme.set(theme);
    if (this.isBrowser) {
      localStorage.setItem(STORAGE_KEY, theme);
    }
    this.apply(theme);
  }

  private readInitial(): AppTheme {
    if (!this.isBrowser) {
      return 'slate';
    }
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'slate' || stored === 'classic') {
        return stored;
      }
    } catch {
      // ignore
    }
    return 'slate';
  }

  private apply(theme: AppTheme): void {
    if (!this.isBrowser) {
      return;
    }
    document.documentElement.setAttribute('data-theme', theme);
  }
}

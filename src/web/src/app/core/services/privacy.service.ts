import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'paydefteri_privacy_mode';

@Injectable({ providedIn: 'root' })
export class PrivacyService {
  readonly isPrivate = signal<boolean>(this.loadInitialState());

  private loadInitialState(): boolean {
    try {
      return localStorage.getItem(STORAGE_KEY) === 'true';
    } catch {
      return false;
    }
  }

  toggle(): void {
    const next = !this.isPrivate();
    this.isPrivate.set(next);
    try {
      localStorage.setItem(STORAGE_KEY, String(next));
    } catch {
      // ignore localstorage errors
    }
  }

  setPrivacy(value: boolean): void {
    this.isPrivate.set(value);
    try {
      localStorage.setItem(STORAGE_KEY, String(value));
    } catch {
      // ignore
    }
  }
}

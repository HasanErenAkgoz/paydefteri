import { Injectable, signal } from '@angular/core';

/** Holds current plan title for the app shell nav. */
@Injectable({ providedIn: 'root' })
export class PlanContextService {
  readonly planTitle = signal<string | null>(null);
  readonly planId = signal<string | null>(null);

  setPlan(id: string | null, title: string | null): void {
    this.planId.set(id);
    this.planTitle.set(title);
  }

  clear(): void {
    this.planId.set(null);
    this.planTitle.set(null);
  }
}

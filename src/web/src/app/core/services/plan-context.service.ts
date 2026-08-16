import { Injectable, signal } from '@angular/core';
import { PlanDto, PlanType } from '../models/api.models';

/** Holds current / last-opened plan for the app shell nav. */
@Injectable({ providedIn: 'root' })
export class PlanContextService {
  readonly planTitle = signal<string | null>(null);
  readonly planDescription = signal<string | null>(null);
  readonly planId = signal<string | null>(null);
  readonly planType = signal<PlanType | null>(null);

  setPlan(
    id: string | null,
    title: string | null,
    description?: string | null,
    planType?: PlanType | null
  ): void {
    this.planId.set(id);
    if (title !== null) {
      this.planTitle.set(title);
    }
    if (description !== undefined) {
      this.planDescription.set(description);
    }
    if (planType !== undefined) {
      this.planType.set(planType ?? null);
    }
  }

  /** Drop stale last-plan if it is no longer in the user's accessible list. */
  syncWithPlans(plans: PlanDto[]): void {
    const currentId = this.planId();
    if (!currentId) {
      if (plans.length) {
        const plan = [...plans].sort((a, b) =>
          String(b.createdAtUtc).localeCompare(String(a.createdAtUtc))
        )[0];
        this.setPlan(plan.id, plan.title, plan.description, plan.planType);
      }
      return;
    }

    const match = plans.find((p) => p.id === currentId);
    if (match) {
      this.setPlan(match.id, match.title, match.description, match.planType);
      return;
    }

    if (plans.length) {
      const plan = [...plans].sort((a, b) =>
        String(b.createdAtUtc).localeCompare(String(a.createdAtUtc))
      )[0];
      this.setPlan(plan.id, plan.title, plan.description, plan.planType);
      return;
    }

    this.clear();
  }

  /** Clear only on logout or when no accessible plans remain. */
  clear(): void {
    this.planId.set(null);
    this.planTitle.set(null);
    this.planDescription.set(null);
    this.planType.set(null);
  }
}

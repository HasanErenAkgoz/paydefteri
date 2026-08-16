import { PlanDto, PlanType } from '../models/api.models';

export function isExpensePlan(plan: Pick<PlanDto, 'planType'> | PlanType | null | undefined): boolean {
  const t = typeof plan === 'object' && plan ? plan.planType : plan;
  return t === 'Expense' || t === 1;
}

export function planHomeCommands(planId: string, planType?: PlanType | null): string[] {
  return isExpensePlan(planType) ? ['/plans', planId, 'expenses'] : ['/plans', planId, 'dashboard'];
}

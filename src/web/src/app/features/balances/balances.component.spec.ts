import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { ExpensesApi } from '../../core/services/expenses.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { ToastService } from '../../shared/toast/toast.service';
import { BalancesComponent } from './balances.component';

describe('BalancesComponent', () => {
  let fixture: ComponentFixture<BalancesComponent>;
  let currentPlan = { id: 'plan-1', title: 'Ev Plani', description: '', planType: 'Installment' };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BalancesComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'plan-1' } } } },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } },
        {
          provide: PlansApi,
          useValue: {
            get: () => of(currentPlan),
            dashboard: () => of({
              planId: 'plan-1', title: 'Ev Plani', description: '', isOwner: true, partners: [], installments: [],
              metrics: { grandTotal: 0, grandPaid: 0, grandRemaining: 0, paidPercent: 0 },
              settlements: [{ partnerId: 'partner-1', partnerName: 'Yusuf', balance: -850 }],
            }),
            settleUp: () => of(void 0),
          },
        },
        {
          provide: ExpensesApi,
          useValue: {
            board: () => of({
              plan: { id: 'plan-1', title: 'Ev Giderleri', description: '', planType: 'Expense' },
              balances: [{ partnerId: 'partner-2', partnerName: 'Ayse', color: '#22c55e', balance: 450 }],
              expenses: [], categories: [], recurrences: [], transfers: [], isOwner: true,
            }),
            createTransfer: () => of(void 0),
            deleteTransfer: () => of(void 0),
          },
        },
        { provide: PlanContextService, useValue: { setPlan: () => undefined } },
        { provide: ToastService, useValue: { error: () => undefined, success: () => undefined } },
        { provide: ConfirmService, useValue: { ask: () => Promise.resolve(true) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(BalancesComponent);
    fixture.detectChanges();
  });

  it('shows installment plan balances in a dedicated settlement view', () => {
    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('h1')?.textContent).toContain('Bakiyeler & Mahsuplaşma');
    expect(host.textContent).toContain('Yusuf');
    expect(host.textContent).toContain('Hesabı kapat');
  });

  it('shows expense plan balances and the transfer workflow in the same view', () => {
    currentPlan = { id: 'plan-1', title: 'Ev Giderleri', description: '', planType: 'Expense' };
    fixture = TestBed.createComponent(BalancesComponent);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Ayse');
    expect(host.textContent).toContain('Transfer (mahsup)');
  });
});

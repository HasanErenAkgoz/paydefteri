import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { CelebrationService } from '../../core/services/celebration.service';
import { InstallmentsApi } from '../../core/services/installments.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { ShareService } from '../../core/services/share.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { ToastService } from '../../shared/toast/toast.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent accessibility', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  const dashboard = {
    planId: 'plan-1',
    title: 'Ev Plani',
    description: '',
    isOwner: true,
    partners: [],
    installments: [],
    settlements: [],
    metrics: { grandTotal: 0, grandPaid: 0, grandRemaining: 0, paidPercent: 0 },
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'plan-1' } } } },
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } },
        {
          provide: PlansApi,
          useValue: {
            get: () => of({ id: 'plan-1', title: 'Ev Plani', description: '', planType: 'Installment' }),
            dashboard: () => of(dashboard),
          },
        },
        { provide: InstallmentsApi, useValue: {} },
        { provide: PlanContextService, useValue: { setPlan: () => undefined } },
        { provide: ToastService, useValue: {} },
        { provide: ConfirmService, useValue: {} },
        { provide: CelebrationService, useValue: {} },
        { provide: ShareService, useValue: {} },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    fixture.componentInstance.loading.set(false);
    fixture.componentInstance.dashboard.set(dashboard as never);
    fixture.detectChanges();
  });

  it('gives the installment filters stable names and accessible labels', () => {
    const host = fixture.nativeElement as HTMLElement;
    const search = host.querySelector<HTMLInputElement>('[name="installmentSearch"]');
    const status = host.querySelector<HTMLSelectElement>('[name="installmentStatus"]');
    const partner = host.querySelector<HTMLSelectElement>('[name="installmentPartner"]');

    expect(search?.getAttribute('aria-label')).toBe('Taksit ara');
    expect(status?.getAttribute('aria-label')).toBe('Ödeme durumu filtresi');
    expect(partner?.getAttribute('aria-label')).toBe('Ortak filtresi');
  });

  it('puts the next financial action and plan progress in one focus panel', () => {
    const host = fixture.nativeElement as HTMLElement;
    const focusPanel = host.querySelector<HTMLElement>('[data-testid="dashboard-focus"]');

    expect(focusPanel?.querySelector('h2')?.textContent).toContain('Bugünün finans odağı');
    expect(focusPanel?.querySelector('[data-testid="focus-progress"]')).not.toBeNull();
    expect(focusPanel?.querySelector<HTMLButtonElement>('[data-testid="focus-calendar-action"]')?.textContent)
      .toContain('Takvime ekle');
  });
});

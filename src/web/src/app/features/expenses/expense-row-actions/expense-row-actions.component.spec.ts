import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExpenseRowActionsComponent } from './expense-row-actions.component';

describe('ExpenseRowActionsComponent', () => {
  let fixture: ComponentFixture<ExpenseRowActionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ExpenseRowActionsComponent] }).compileComponents();
    fixture = TestBed.createComponent(ExpenseRowActionsComponent);
    fixture.componentRef.setInput('expense', {
      id: 'expense-1',
      name: 'Market',
      totalAmount: 1200,
      status: 'Planned',
      paidByPartnerId: null,
      shareLines: [{ partnerId: 'partner-1', shareAmount: 1200 }],
    });
    fixture.componentRef.setInput('partners', [{ id: 'partner-1', name: 'Yusuf', color: '#38bdf8' }]);
    fixture.componentRef.setInput('canManage', true);
    fixture.componentRef.setInput('marking', false);
    fixture.detectChanges();
  });

  it('visually separates payment from edit and delete actions', () => {
    const host = fixture.nativeElement as HTMLElement;
    const paymentActions = host.querySelector<HTMLElement>('.expense-payment-actions');
    const managementActions = host.querySelector<HTMLElement>('.expense-management-actions');

    expect(paymentActions?.textContent).toContain('Ödendi yap');
    expect(managementActions?.textContent).toContain('Düzenle');
    expect(managementActions?.textContent).toContain('Sil');
  });
});

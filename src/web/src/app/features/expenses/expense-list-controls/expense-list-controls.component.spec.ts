import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DEFAULT_EXPENSE_FILTER_STATE } from '../expense-filter.models';
import { ExpenseListControlsComponent } from './expense-list-controls.component';

describe('ExpenseListControlsComponent', () => {
  let fixture: ComponentFixture<ExpenseListControlsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ExpenseListControlsComponent] }).compileComponents();

    fixture = TestBed.createComponent(ExpenseListControlsComponent);
    fixture.componentRef.setInput('filterState', { ...DEFAULT_EXPENSE_FILTER_STATE });
    fixture.detectChanges();
  });

  it('keeps mobile search and filter in the same toolbar', () => {
    const host = fixture.nativeElement as HTMLElement;
    const toolbar = host.querySelector('.mobile-search-toolbar');

    expect(toolbar).not.toBeNull();
    expect(toolbar!.querySelector('.expense-search-input')).not.toBeNull();
    expect(toolbar!.querySelector('.btn-filter-sheet')).not.toBeNull();
  });
});

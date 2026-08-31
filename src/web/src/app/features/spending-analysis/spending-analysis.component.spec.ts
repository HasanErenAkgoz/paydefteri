import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { SpendingAnalysisApi } from '../../core/services/spending-analysis.api';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { ToastService } from '../../shared/toast/toast.service';
import { SpendingAnalysisComponent } from './spending-analysis.component';

describe('SpendingAnalysisComponent accessibility', () => {
  let fixture: ComponentFixture<SpendingAnalysisComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpendingAnalysisComponent],
      providers: [
        { provide: SpendingAnalysisApi, useValue: { listStatements: () => of([]) } },
        { provide: ToastService, useValue: { error: () => undefined } },
        { provide: ConfirmService, useValue: {} },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SpendingAnalysisComponent);
    fixture.detectChanges();
  });

  it('describes the empty-state upload target and its supported file formats', () => {
    const host = fixture.nativeElement as HTMLElement;
    const dropZone = host.querySelector<HTMLElement>('.drop-zone')!;
    const hint = host.querySelector<HTMLElement>('#statement-upload-hint');
    const fileInput = dropZone.querySelector<HTMLInputElement>('input[type="file"]')!;

    expect(dropZone.getAttribute('aria-describedby')).toBe('statement-upload-hint');
    expect(fileInput.getAttribute('aria-label')).toBe('Ekstre dosyası seç');
    expect(hint?.textContent).toContain('15 MB');
  });
});

describe('SpendingAnalysisComponent statement history', () => {
  let fixture: ComponentFixture<SpendingAnalysisComponent>;
  let deleteStatement: jasmine.Spy;
  const statement = {
    id: 'statement-1',
    sourceFileName: 'haziran-ekstresi.pdf',
    sourceKind: 'PDF',
    periodStart: '2026-06-01',
    periodEnd: '2026-06-30',
    totals: [{ currency: 'TRY', spending: 1250, refunds: 0, net: 1250 }],
    transactions: [],
    warnings: [],
    createdAtUtc: '2026-06-30T00:00:00Z',
  };

  beforeEach(async () => {
    deleteStatement = jasmine.createSpy('deleteStatement').and.returnValue(of(void 0));
    await TestBed.configureTestingModule({
      imports: [SpendingAnalysisComponent],
      providers: [
        {
          provide: SpendingAnalysisApi,
          useValue: {
            listStatements: () => of([{ ...statement, transactionCount: 0 }]),
            getStatement: () => of(statement),
            getDashboard: () => of({ statementId: statement.id, currencies: [] }),
            getPatterns: () => of({ statementId: statement.id, previousStatementComparison: null, recurringCandidates: [], smallFrequentClusters: [], activity: [], positiveFindings: [] }),
            getBudgetStatus: () => of({ statementId: statement.id, categories: [] }),
            deleteStatement,
          },
        },
        { provide: ToastService, useValue: { error: () => undefined, success: () => undefined } },
        { provide: ConfirmService, useValue: { ask: () => Promise.resolve(true) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SpendingAnalysisComponent);
    fixture.detectChanges();
  });

  it('offers a separately labelled delete action for every statement in history', () => {
    const host = fixture.nativeElement as HTMLElement;
    const deleteButton = host.querySelector<HTMLButtonElement>('.history-delete');

    expect(deleteButton?.getAttribute('aria-label')).toContain('haziran-ekstresi.pdf');
  });

  it('deletes the selected statement after confirmation', async () => {
    await fixture.componentInstance.removeStatement(statement);

    expect(deleteStatement).toHaveBeenCalledWith(statement.id);
  });
});

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

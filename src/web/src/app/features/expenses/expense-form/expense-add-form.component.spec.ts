import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CameraService } from '../../../core/platform/camera.service';
import { ExpenseAddFormComponent } from './expense-add-form.component';

describe('ExpenseAddFormComponent', () => {
  let fixture: ComponentFixture<ExpenseAddFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpenseAddFormComponent],
      providers: [{ provide: CameraService, useValue: { isNative: false } }],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpenseAddFormComponent);
    fixture.componentRef.setInput('categories', []);
    fixture.componentRef.setInput('partners', []);
    fixture.componentRef.setInput('saving', false);
    fixture.detectChanges();
  });

  it('keeps receipt capture choices explicit for browser users', () => {
    const host = fixture.nativeElement as HTMLElement;
    const captureActions = host.querySelector('.receipt-capture-actions');

    expect(captureActions?.textContent).toContain('Kamera ile çek');
    expect(captureActions?.textContent).toContain('Fotoğraf seç');
  });
});

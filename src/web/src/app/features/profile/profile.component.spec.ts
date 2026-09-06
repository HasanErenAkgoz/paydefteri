import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { PlanContextService } from '../../core/services/plan-context.service';
import { ToastService } from '../../shared/toast/toast.service';
import { ProfileComponent } from './profile.component';

describe('ProfileComponent accessibility', () => {
  let fixture: ComponentFixture<ProfileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        {
          provide: AuthService,
          useValue: {
            isMobileApp: false,
            me: () =>
              of({
                userId: 'user-id',
                email: 'ayse@example.com',
                displayName: 'Ayse Yilmaz',
                emailConfirmed: true,
              }),
            updateProfile: () => of(void 0),
            changePassword: () => of(void 0),
            logout: () => undefined,
          },
        },
        {
          provide: ToastService,
          useValue: { error: () => undefined, success: () => undefined, info: () => undefined },
        },
        { provide: PlanContextService, useValue: { clear: () => undefined } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileComponent);
    fixture.detectChanges();
  });

  it('moves between profile tabs with arrow keys and connects the active tab to its panel', () => {
    const host = fixture.nativeElement as HTMLElement;
    const tabs = host.querySelectorAll<HTMLButtonElement>('.settings-nav-item');
    const accountTab = tabs[0]!;
    accountTab.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();

    const securityTab = tabs[1]!;
    const securityPanel = host.querySelector<HTMLElement>('[role="tabpanel"]');
    expect(securityTab.getAttribute('aria-selected')).toBe('true');
    expect(securityTab.getAttribute('aria-controls')).toBe('profile-panel-security');
    expect(securityPanel?.getAttribute('aria-labelledby')).toBe('profile-tab-security');
  });
});

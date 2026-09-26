import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GoogleAuthService } from '../../core/services/google-auth.service';
import { GoogleSignInButtonComponent } from './google-sign-in-button.component';

describe('GoogleSignInButtonComponent', () => {
  async function createFixture(
    google: Partial<GoogleAuthService>
  ): Promise<ComponentFixture<GoogleSignInButtonComponent>> {
    await TestBed.resetTestingModule()
      .configureTestingModule({
        imports: [GoogleSignInButtonComponent],
        providers: [{ provide: GoogleAuthService, useValue: google }],
      })
      .compileComponents();

    const fixture = TestBed.createComponent(GoogleSignInButtonComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('renders nothing when no client id is configured', async () => {
    const fixture = await createFixture({ available: false });

    // A button that cannot work is worse than no button at all.
    expect(fixture.componentInstance.failed()).toBeTrue();
    expect(fixture.nativeElement.querySelector('.google-signin')).toBeNull();
  });

  it('renders nothing when the Google script cannot be loaded', async () => {
    const fixture = await createFixture({
      available: true,
      renderButton: () => Promise.reject(new Error('blocked')),
    });

    expect(fixture.componentInstance.failed()).toBeTrue();
    expect(fixture.nativeElement.querySelector('.google-signin')).toBeNull();
  });

  it('emits the ID token Google hands back', async () => {
    const handOver: { call?: (idToken: string) => void } = {};
    const fixture = await createFixture({
      available: true,
      renderButton: (_host, _options, onCredential) => {
        handOver.call = onCredential;
        return Promise.resolve();
      },
    });

    const received: string[] = [];
    fixture.componentInstance.credential.subscribe((token) => received.push(token));
    handOver.call?.('id-token-from-google');

    expect(received).toEqual(['id-token-from-google']);
    expect(fixture.componentInstance.failed()).toBeFalse();
    expect(fixture.nativeElement.querySelector('.google-signin')).not.toBeNull();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  GoogleAuthService,
  GoogleSignInCancelledError,
} from '../../core/services/google-auth.service';
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

  describe('when Google cannot be offered', () => {
    it('renders nothing rather than a button that cannot work', async () => {
      const fixture = await createFixture({ mode: 'unavailable' });

      expect(fixture.componentInstance.failed()).toBeTrue();
      expect(fixture.nativeElement.querySelector('.google-signin')).toBeNull();
    });
  });

  describe('in the browser', () => {
    it('emits the ID token Google hands back', async () => {
      const handOver: { call?: (idToken: string) => void } = {};
      const fixture = await createFixture({
        mode: 'web',
        renderButton: (_host, _options, onCredential) => {
          handOver.call = onCredential;
          return Promise.resolve();
        },
      });

      const received: string[] = [];
      fixture.componentInstance.credential.subscribe((token) => received.push(token));
      handOver.call?.('id-token-from-google');

      expect(received).toEqual(['id-token-from-google']);
      expect(fixture.nativeElement.querySelector('.google-signin')).not.toBeNull();
    });

    it('removes itself when the Google script cannot be loaded', async () => {
      const fixture = await createFixture({
        mode: 'web',
        renderButton: () => Promise.reject(new Error('blocked')),
      });

      expect(fixture.componentInstance.failed()).toBeTrue();
      expect(fixture.nativeElement.querySelector('.google-signin')).toBeNull();
    });
  });

  describe('in the native shell', () => {
    function nativeButton(fixture: ComponentFixture<GoogleSignInButtonComponent>): HTMLButtonElement {
      return fixture.nativeElement.querySelector('.google-native') as HTMLButtonElement;
    }

    it('shows its own button, because Google will not render one here', async () => {
      const fixture = await createFixture({
        mode: 'native',
        signInNative: () => Promise.resolve('token'),
      });

      expect(fixture.componentInstance.native()).toBeTrue();
      expect(nativeButton(fixture)).not.toBeNull();
      expect(nativeButton(fixture).textContent).toContain('Google ile devam et');
      // Google's web button must not be rendered alongside it.
      expect(fixture.nativeElement.querySelector('.google-signin__host')).toBeNull();
    });

    it('emits the ID token the native picker returns', async () => {
      const fixture = await createFixture({
        mode: 'native',
        signInNative: () => Promise.resolve('id-token-from-plugin'),
      });

      const received: string[] = [];
      fixture.componentInstance.credential.subscribe((token) => received.push(token));
      await fixture.componentInstance.signInNatively();

      expect(received).toEqual(['id-token-from-plugin']);
    });

    it('stays silent when the account picker is dismissed', async () => {
      const fixture = await createFixture({
        mode: 'native',
        signInNative: () => Promise.reject(new GoogleSignInCancelledError()),
      });

      const failures: unknown[] = [];
      fixture.componentInstance.nativeError.subscribe((e) => failures.push(e));
      await fixture.componentInstance.signInNatively();

      // Backing out is a choice, not an error to report at the person.
      expect(failures).toEqual([]);
      expect(nativeButton(fixture)).not.toBeNull();
    });

    it('reports a real sign-in failure', async () => {
      const boom = new Error('play services missing');
      const fixture = await createFixture({
        mode: 'native',
        signInNative: () => Promise.reject(boom),
      });

      const failures: unknown[] = [];
      fixture.componentInstance.nativeError.subscribe((e) => failures.push(e));
      await fixture.componentInstance.signInNatively();

      expect(failures).toEqual([boom]);
    });
  });
});

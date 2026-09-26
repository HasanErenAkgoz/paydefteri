import { isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  NgZone,
  OnDestroy,
  Output,
  PLATFORM_ID,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import {
  GoogleAuthService,
  GoogleButtonText,
  GoogleSignInCancelledError,
} from '../../core/services/google-auth.service';

/**
 * Google sign-in entry point, in whichever form the platform allows.
 *
 * In the browser this is Google's own rendered button, because Google will not
 * hand an ID token to a button we draw ourselves. In the Capacitor shell the
 * token comes from the native plugin instead, so there the button is ours and
 * follows Google's branding spec for a dark background. When neither is
 * possible — no client id, or the script is blocked — the whole block is
 * removed rather than left as a control that cannot work.
 */
@Component({
  selector: 'app-google-sign-in-button',
  standalone: true,
  template: `
    @if (!failed()) {
      <div class="google-signin" [class.google-signin--busy]="busy">
        @if (native()) {
          <button class="google-native" type="button" [disabled]="busy" (click)="signInNatively()">
            <svg class="google-native__mark" viewBox="0 0 24 24" aria-hidden="true">
              <path
                fill="#EA4335"
                d="M12 10.2v3.9h5.5c-.2 1.3-1.6 3.8-5.5 3.8A6.4 6.4 0 1 1 12 5.6c1.8 0 3 .8 3.7 1.4l2.5-2.4A10.2 10.2 0 1 0 12 22.2c5.9 0 9.8-4.1 9.8-9.9 0-.7-.1-1.2-.2-1.7H12Z"
              />
            </svg>
            <span>Google ile devam et</span>
          </button>
        } @else {
          <div class="google-signin__host" #host></div>
        }

        @if (busy) {
          <div class="google-signin__veil" aria-live="polite">Google ile giriş yapılıyor…</div>
        }
      </div>
    }
  `,
  styles: [
    `
      .google-signin {
        position: relative;
        display: flex;
        justify-content: center;
        min-height: 44px;
      }

      .google-signin__host {
        display: flex;
        justify-content: center;
        width: 100%;
      }

      .google-signin--busy .google-signin__host,
      .google-signin--busy .google-native {
        opacity: 0.35;
        pointer-events: none;
      }

      .google-signin__veil {
        position: absolute;
        inset: 0;
        display: grid;
        place-items: center;
        font-size: 0.85rem;
        font-weight: 600;
        color: inherit;
      }

      /* Google's dark button spec, sized like the other buttons on the card. */
      .google-native {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 10px;
        width: 100%;
        min-height: 44px;
        padding: 0 18px;
        border-radius: 999px;
        border: 1px solid #8e918f;
        background: #131314;
        color: #e3e3e3;
        font: inherit;
        font-size: 0.95rem;
        font-weight: 600;
        cursor: pointer;
      }

      .google-native:disabled {
        cursor: default;
      }

      .google-native__mark {
        width: 20px;
        height: 20px;
        flex: none;
      }
    `,
  ],
})
export class GoogleSignInButtonComponent implements AfterViewInit, OnDestroy {
  private readonly google = inject(GoogleAuthService);
  private readonly zone = inject(NgZone);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  /** Wording Google puts on the web button — "Google ile devam et" for `continue_with`. */
  @Input() text: GoogleButtonText = 'continue_with';
  /** Dims the button while the token is being exchanged for a session. */
  @Input() busy = false;

  @Output() readonly credential = new EventEmitter<string>();
  /** Raised when the button cannot be shown, so the host can drop its divider. */
  @Output() readonly unavailable = new EventEmitter<void>();
  /** Raised when the native picker fails or is dismissed without a token. */
  @Output() readonly nativeError = new EventEmitter<unknown>();

  @ViewChild('host') private hostRef?: ElementRef<HTMLDivElement>;

  readonly failed = signal(false);
  readonly native = signal(false);

  private resizeObserver?: ResizeObserver;
  private renderedWidth = 0;

  ngAfterViewInit(): void {
    const mode = this.isBrowser ? this.google.mode : 'unavailable';

    if (mode === 'unavailable') {
      this.markUnavailable();
      return;
    }

    if (mode === 'native') {
      // Our own button; nothing to render into or to measure.
      this.native.set(true);
      return;
    }

    void this.render();

    const host = this.hostRef?.nativeElement;
    if (host && typeof ResizeObserver !== 'undefined') {
      // Google bakes the width into the rendered markup, so a layout change
      // (rotation, responsive breakpoint) needs a fresh render.
      this.resizeObserver = new ResizeObserver(() => {
        if (Math.abs(this.measure() - this.renderedWidth) > 8) {
          void this.render();
        }
      });
      this.resizeObserver.observe(host.parentElement ?? host);
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  async signInNatively(): Promise<void> {
    if (this.busy) {
      return;
    }

    try {
      this.credential.emit(await this.google.signInNative());
    } catch (error) {
      // Backing out of the picker is a choice, not a failure worth a message.
      if (!(error instanceof GoogleSignInCancelledError)) {
        this.nativeError.emit(error);
      }
    }
  }

  private async render(): Promise<void> {
    const host = this.hostRef?.nativeElement;
    if (!host) {
      return;
    }

    const width = this.measure();
    try {
      await this.google.renderButton(host, { text: this.text, width }, (idToken) =>
        // Google calls back outside Angular's zone.
        this.zone.run(() => this.credential.emit(idToken))
      );
      this.renderedWidth = width;
    } catch {
      this.markUnavailable();
    }
  }

  private measure(): number {
    const host = this.hostRef?.nativeElement;
    const width = host?.parentElement?.clientWidth || host?.clientWidth || 0;
    return width > 0 ? width : 320;
  }

  private markUnavailable(): void {
    this.failed.set(true);
    this.resizeObserver?.disconnect();
    this.unavailable.emit();
  }
}

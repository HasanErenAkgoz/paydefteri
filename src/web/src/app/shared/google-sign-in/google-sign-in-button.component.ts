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
import { GoogleAuthService, GoogleButtonText } from '../../core/services/google-auth.service';

/**
 * Google's own rendered sign-in button. Google will not hand an ID token to a
 * button we draw ourselves, so this wraps theirs and only sizes it to the card.
 * When the script is blocked or no client id is configured the whole block is
 * removed rather than left as a button that does nothing.
 */
@Component({
  selector: 'app-google-sign-in-button',
  standalone: true,
  template: `
    @if (!failed()) {
      <div class="google-signin" [class.google-signin--busy]="busy">
        <div class="google-signin__host" #host></div>
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

      .google-signin--busy .google-signin__host {
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
    `,
  ],
})
export class GoogleSignInButtonComponent implements AfterViewInit, OnDestroy {
  private readonly google = inject(GoogleAuthService);
  private readonly zone = inject(NgZone);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  /** Wording Google puts on the button — "Google ile devam et" for `continue_with`. */
  @Input() text: GoogleButtonText = 'continue_with';
  /** Dims the button while the token is being exchanged for a session. */
  @Input() busy = false;

  @Output() readonly credential = new EventEmitter<string>();
  /** Raised when the button cannot be shown, so the host can drop its divider. */
  @Output() readonly unavailable = new EventEmitter<void>();

  @ViewChild('host') private hostRef?: ElementRef<HTMLDivElement>;

  readonly failed = signal(false);

  private resizeObserver?: ResizeObserver;
  private renderedWidth = 0;

  ngAfterViewInit(): void {
    if (!this.isBrowser || !this.google.available) {
      this.markUnavailable();
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

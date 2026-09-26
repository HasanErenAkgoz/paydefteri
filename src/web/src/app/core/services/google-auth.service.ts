import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { environment } from '../../../environments/environment';

/** Google Identity Services — loaded on demand, only on the pages that show the button. */
const GIS_SCRIPT_URL = 'https://accounts.google.com/gsi/client';

export type GoogleButtonText = 'signin_with' | 'signup_with' | 'continue_with';

/** The slice of the `google.accounts.id` API this app uses. */
interface GoogleIdentityApi {
  initialize(options: {
    client_id: string;
    callback: (response: { credential?: string }) => void;
    cancel_on_tap_outside?: boolean;
    auto_select?: boolean;
    ux_mode?: 'popup' | 'redirect';
    itp_support?: boolean;
  }): void;
  renderButton(
    parent: HTMLElement,
    options: {
      type?: 'standard' | 'icon';
      theme?: 'outline' | 'filled_blue' | 'filled_black';
      size?: 'small' | 'medium' | 'large';
      text?: GoogleButtonText;
      shape?: 'rectangular' | 'pill' | 'circle' | 'square';
      logo_alignment?: 'left' | 'center';
      width?: number;
      locale?: string;
    }
  ): void;
  disableAutoSelect(): void;
}

type GoogleGlobal = { accounts?: { id?: GoogleIdentityApi } };

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private scriptLoad: Promise<GoogleIdentityApi> | null = null;

  /**
   * The Capacitor shell runs on an origin Google cannot authorise, so the web
   * button is a browser-only affordance; native sign-in needs a native plugin.
   */
  get available(): boolean {
    return this.isBrowser && !environment.mobile && !!environment.googleClientId;
  }

  /**
   * Draws Google's own button inside `host` and calls back with the ID token.
   * Rejects when the script cannot be reached, so callers can fall back.
   */
  async renderButton(
    host: HTMLElement,
    options: { text: GoogleButtonText; width: number },
    onCredential: (idToken: string) => void
  ): Promise<void> {
    if (!this.available) {
      throw new Error('Google sign-in is not available on this build.');
    }

    const api = await this.load();
    api.initialize({
      client_id: environment.googleClientId,
      callback: (response) => {
        if (response?.credential) {
          onCredential(response.credential);
        }
      },
      cancel_on_tap_outside: true,
      auto_select: false,
      ux_mode: 'popup',
      itp_support: true,
    });

    host.replaceChildren();
    api.renderButton(host, {
      type: 'standard',
      // The auth cards are dark-only, so Google's dark variant sits with the
      // other buttons instead of glaring white between them.
      theme: 'filled_black',
      size: 'large',
      text: options.text,
      shape: 'pill',
      logo_alignment: 'left',
      // Google caps the rendered button at 400px and refuses 0.
      width: Math.min(400, Math.max(200, Math.round(options.width))),
      locale: 'tr',
    });
  }

  /** Stops Google from silently re-selecting the account after a sign-out. */
  disableAutoSelect(): void {
    const api = (globalThis as { google?: GoogleGlobal }).google?.accounts?.id;
    api?.disableAutoSelect();
  }

  private load(): Promise<GoogleIdentityApi> {
    this.scriptLoad ??= new Promise<GoogleIdentityApi>((resolve, reject) => {
      const ready = () => {
        const api = (globalThis as { google?: GoogleGlobal }).google?.accounts?.id;
        if (api) {
          resolve(api);
        } else {
          this.scriptLoad = null;
          reject(new Error('Google Identity Services did not initialise.'));
        }
      };

      const existing = document.querySelector<HTMLScriptElement>(`script[src="${GIS_SCRIPT_URL}"]`);
      if (existing) {
        existing.addEventListener('load', ready, { once: true });
        existing.addEventListener('error', () => {
          this.scriptLoad = null;
          reject(new Error('Google Identity Services could not be loaded.'));
        }, { once: true });
        // A script added by an earlier visit to this route is already done.
        if ((globalThis as { google?: GoogleGlobal }).google?.accounts?.id) {
          ready();
        }
        return;
      }

      const script = document.createElement('script');
      script.src = GIS_SCRIPT_URL;
      script.async = true;
      script.defer = true;
      script.addEventListener('load', ready, { once: true });
      script.addEventListener('error', () => {
        script.remove();
        this.scriptLoad = null;
        reject(new Error('Google Identity Services could not be loaded.'));
      }, { once: true });
      document.head.appendChild(script);
    });

    return this.scriptLoad;
  }
}

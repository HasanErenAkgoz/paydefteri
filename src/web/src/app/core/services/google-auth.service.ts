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

/**
 * How Google sign-in can be offered here. The Capacitor shell runs on an origin
 * Google will not authorise, so it cannot use the browser button and goes
 * through the native plugin instead.
 */
export type GoogleSignInMode = 'web' | 'native' | 'unavailable';

/**
 * The person backed out of the native account picker. Distinct from a failure
 * because dismissing a sheet should not raise an error at them.
 */
export class GoogleSignInCancelledError extends Error {
  constructor() {
    super('Google sign-in was dismissed.');
    this.name = 'GoogleSignInCancelledError';
  }
}

/** Dismissals surface differently per platform, so match on what they all carry. */
function isDismissal(error: unknown): boolean {
  const parts = [
    (error as { message?: unknown } | null)?.message,
    (error as { code?: unknown } | null)?.code,
    (error as { errorMessage?: unknown } | null)?.errorMessage,
  ]
    .filter((v) => typeof v === 'string' || typeof v === 'number')
    .map((v) => String(v).toLowerCase());

  return parts.some(
    (p) =>
      p.includes('cancel') ||
      p.includes('dismiss') ||
      p.includes('closed') ||
      // Android's SIGN_IN_CANCELLED status code.
      p === '12501'
  );
}

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private scriptLoad: Promise<GoogleIdentityApi> | null = null;
  private nativeInit: Promise<void> | null = null;

  get mode(): GoogleSignInMode {
    if (!this.isBrowser || !environment.googleClientId) {
      return 'unavailable';
    }
    return environment.mobile ? 'native' : 'web';
  }

  get available(): boolean {
    return this.mode !== 'unavailable';
  }

  /**
   * Opens the native account picker and returns the ID token. Google mints it
   * for the web client id even on a phone, so the API validates it exactly the
   * way it validates one from the browser button.
   */
  async signInNative(): Promise<string> {
    if (this.mode !== 'native') {
      throw new Error('Native Google sign-in is not available on this build.');
    }

    const { SocialLogin } = await this.nativePlugin();
    await this.ensureNativeInitialized();

    let response;
    try {
      response = await SocialLogin.login({
        provider: 'google',
        options: { scopes: ['email', 'profile'] },
      });
    } catch (error) {
      throw isDismissal(error) ? new GoogleSignInCancelledError() : error;
    }

    const result = response.result as { idToken?: string | null } | undefined;
    const idToken = result?.idToken;
    if (!idToken) {
      throw new Error('Google did not return an ID token.');
    }
    return idToken;
  }

  /**
   * Loaded on demand so the plugin's web stub stays out of the browser bundle,
   * where it is never used.
   */
  private nativePlugin() {
    return import('@capgo/capacitor-social-login');
  }

  private ensureNativeInitialized(): Promise<void> {
    this.nativeInit ??= (async () => {
      const { SocialLogin } = await this.nativePlugin();
      await SocialLogin.initialize({
        google: {
          // Android and the token audience both key off the web client id.
          webClientId: environment.googleClientId,
          iOSClientId: environment.googleIosClientId || undefined,
          iOSServerClientId: environment.googleClientId,
        },
      });
    })().catch((error) => {
      // A failed init must not poison every later attempt.
      this.nativeInit = null;
      throw error;
    });
    return this.nativeInit;
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
    if (this.mode !== 'web') {
      throw new Error('The Google button is only available in the browser.');
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

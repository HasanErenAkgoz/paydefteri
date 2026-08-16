import { Injectable, inject } from '@angular/core';
import { App, URLOpenListenerEvent } from '@capacitor/app';
import { Keyboard } from '@capacitor/keyboard';
import { SplashScreen } from '@capacitor/splash-screen';
import { StatusBar, Style } from '@capacitor/status-bar';
import { Router } from '@angular/router';
import { PlatformService } from './platform.service';
import { ToastService } from '../../shared/toast/toast.service';

const EXIT_CONFIRMATION_WINDOW_MS = 2_000;

@Injectable({ providedIn: 'root' })
export class AppLifecycleService {
  private readonly platform = inject(PlatformService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private initialized = false;
  private lastExitRequestAt = 0;

  async initialize(): Promise<void> {
    if (!this.platform.isNative || this.initialized) {
      return;
    }

    this.initialized = true;
    document.body.classList.add('native-app', `native-${this.platform.platform}`);

    await StatusBar.setOverlaysWebView({ overlay: true });
    await StatusBar.setBackgroundColor({ color: '#00000000' });
    await StatusBar.setStyle({ style: Style.Dark });

    await App.addListener('appUrlOpen', (event) => this.openDeepLink(event));
    await App.addListener('backButton', ({ canGoBack }) => this.handleBackButton(canGoBack));
    await Keyboard.addListener('keyboardWillShow', () => document.body.classList.add('keyboard-open'));
    await Keyboard.addListener('keyboardWillHide', () => document.body.classList.remove('keyboard-open'));

    const launch = await App.getLaunchUrl();
    if (launch?.url) {
      await this.navigateToDeepLink(launch.url);
    }

    await SplashScreen.hide();
  }

  private openDeepLink(event: URLOpenListenerEvent): void {
    void this.navigateToDeepLink(event.url);
  }

  private async navigateToDeepLink(rawUrl: string): Promise<void> {
    try {
      const url = new URL(rawUrl);
      if (url.hostname !== 'paydefteri.com' || !url.pathname.startsWith('/invite/')) {
        return;
      }
      await this.router.navigateByUrl(`${url.pathname}${url.search}`);
    } catch {
      // Invalid external URLs are ignored deliberately and never logged with their token.
    }
  }

  private handleBackButton(canGoBack: boolean): void {
    const path = this.router.url.split('?')[0];

    if (path === '/') {
      this.requestAppExit();
      return;
    }

    if (canGoBack && path !== '/plans') {
      history.back();
      return;
    }

    if (path === '/login' || path === '/register' || path.startsWith('/invite/')) {
      void this.router.navigateByUrl('/');
      return;
    }

    if (path !== '/plans') {
      void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
      return;
    }

    this.requestAppExit();
  }

  private requestAppExit(): void {
    const now = Date.now();
    if (now - this.lastExitRequestAt <= EXIT_CONFIRMATION_WINDOW_MS) {
      void App.exitApp();
      return;
    }

    this.lastExitRequestAt = now;
    this.toast.info('Çıkmak için geri tuşuna tekrar basın.');
  }
}

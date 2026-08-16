import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import { Network } from '@capacitor/network';
import { PluginListenerHandle } from '@capacitor/core';
import { PlatformService } from './platform.service';

@Injectable({ providedIn: 'root' })
export class ConnectivityService implements OnDestroy {
  private readonly platform = inject(PlatformService);
  private listener: PluginListenerHandle | null = null;

  readonly online = signal(true);
  readonly initialized = signal(false);

  async initialize(): Promise<void> {
    if (this.initialized()) {
      return;
    }

    if (this.platform.isNative) {
      const status = await Network.getStatus();
      this.online.set(status.connected);
      this.listener = await Network.addListener('networkStatusChange', (next) => {
        this.online.set(next.connected);
      });
    } else if (typeof navigator !== 'undefined' && typeof window !== 'undefined') {
      this.online.set(navigator.onLine);
      window.addEventListener('online', this.handleBrowserOnline);
      window.addEventListener('offline', this.handleBrowserOffline);
    }

    this.initialized.set(true);
  }

  ngOnDestroy(): void {
    void this.listener?.remove();
    if (!this.platform.isNative && typeof window !== 'undefined') {
      window.removeEventListener('online', this.handleBrowserOnline);
      window.removeEventListener('offline', this.handleBrowserOffline);
    }
  }

  private readonly handleBrowserOnline = (): void => this.online.set(true);
  private readonly handleBrowserOffline = (): void => this.online.set(false);
}

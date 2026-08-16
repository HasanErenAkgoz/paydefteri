import { Injectable } from '@angular/core';
import { Capacitor } from '@capacitor/core';

export type PayDefteriPlatform = 'web' | 'ios' | 'android';

@Injectable({ providedIn: 'root' })
export class PlatformService {
  readonly isNative = Capacitor.isNativePlatform();
  readonly platform: PayDefteriPlatform = this.isNative
    ? (Capacitor.getPlatform() as 'ios' | 'android')
    : 'web';
}

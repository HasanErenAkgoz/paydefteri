import { Injectable, inject } from '@angular/core';
import { Share } from '@capacitor/share';
import { PlatformService } from './platform.service';

export interface ShareContent {
  title: string;
  text?: string;
  url?: string;
}

@Injectable({ providedIn: 'root' })
export class ShareService {
  private readonly platform = inject(PlatformService);

  async share(content: ShareContent): Promise<boolean> {
    if (this.platform.isNative) {
      await Share.share({
        title: content.title,
        text: content.text,
        url: content.url,
        dialogTitle: content.title,
      });
      return true;
    }

    if (typeof navigator !== 'undefined' && navigator.share) {
      await navigator.share(content);
      return true;
    }

    if (content.url && typeof navigator !== 'undefined' && navigator.clipboard) {
      await navigator.clipboard.writeText(content.url);
      return true;
    }

    return false;
  }
}

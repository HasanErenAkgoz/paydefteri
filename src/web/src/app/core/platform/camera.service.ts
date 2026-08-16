import { Injectable, inject } from '@angular/core';
import {
  Camera,
  CameraResultType,
  CameraSource,
  ImageOptions,
  Photo,
} from '@capacitor/camera';
import { PlatformService } from './platform.service';

@Injectable({ providedIn: 'root' })
export class CameraService {
  private readonly platform = inject(PlatformService);

  readonly isNative = this.platform.isNative;

  captureReceipt(): Promise<File | null> {
    return this.pickReceipt(CameraSource.Camera);
  }

  selectReceipt(): Promise<File | null> {
    return this.pickReceipt(CameraSource.Photos);
  }

  private async pickReceipt(source: CameraSource): Promise<File | null> {
    if (!this.isNative) {
      return null;
    }

    const options: ImageOptions = {
      source,
      resultType: CameraResultType.Uri,
      quality: 82,
      width: 2200,
      height: 2200,
      correctOrientation: true,
      saveToGallery: false,
      promptLabelHeader: 'Fiş veya fatura',
      promptLabelPhoto: 'Fotoğraflardan seç',
      promptLabelPicture: 'Kamera ile çek',
      promptLabelCancel: 'Vazgeç',
    };

    try {
      const photo = await Camera.getPhoto(options);
      return await this.toFile(photo);
    } catch (error) {
      // Capacitor rejects the promise when the native picker is cancelled.
      if (this.isCancellation(error)) {
        return null;
      }
      throw error;
    }
  }

  private async toFile(photo: Photo): Promise<File> {
    if (!photo.webPath) {
      throw new Error('Seçilen görsel okunamadı. Lütfen tekrar deneyin.');
    }

    const response = await fetch(photo.webPath);
    if (!response.ok) {
      throw new Error('Seçilen görsel okunamadı. Lütfen tekrar deneyin.');
    }

    const blob = await response.blob();
    const format = photo.format.toLowerCase();
    const extension = format === 'jpg' ? 'jpg' : format;
    const contentType = format === 'jpg' ? 'image/jpeg' : `image/${format}`;
    return new File([blob], `fis-${Date.now()}.${extension}`, {
      type: blob.type || contentType,
    });
  }

  private isCancellation(error: unknown): boolean {
    const message = error instanceof Error ? error.message : String(error ?? '');
    return /cancel|cancelled|canceled|user cancelled/i.test(message);
  }
}

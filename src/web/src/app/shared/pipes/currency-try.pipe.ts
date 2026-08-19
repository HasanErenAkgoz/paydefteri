import { Pipe, PipeTransform, inject } from '@angular/core';
import { formatTry } from '../utils/format';
import { PrivacyService } from '../../core/services/privacy.service';

@Pipe({
  name: 'tryCurrency',
  standalone: true,
  pure: false
})
export class CurrencyTryPipe implements PipeTransform {
  private readonly privacyService = inject(PrivacyService);

  transform(value: number | null | undefined, forceShow = false): string {
    if (this.privacyService.isPrivate() && !forceShow) {
      return '₺ ••••••';
    }
    if (value == null || Number.isNaN(value)) {
      return formatTry(0);
    }
    return formatTry(value);
  }
}

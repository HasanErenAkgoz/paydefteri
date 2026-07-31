import { Pipe, PipeTransform } from '@angular/core';
import { formatTry } from '../utils/format';

@Pipe({ name: 'tryCurrency', standalone: true })
export class CurrencyTryPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null || Number.isNaN(value)) {
      return formatTry(0);
    }
    return formatTry(value);
  }
}

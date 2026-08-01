import { Directive, ElementRef, HostListener, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { formatMoneyInputLive, formatMoneyTr } from '../utils/money';

/** Text input that shows TR thousands (1.500.000,50) and binds a number to ngModel. */
@Directive({
  selector: 'input[appMoneyInput]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MoneyInputDirective),
      multi: true,
    },
  ],
  host: {
    type: 'text',
    inputmode: 'decimal',
    autocomplete: 'off',
  },
})
export class MoneyInputDirective implements ControlValueAccessor {
  private readonly el = inject(ElementRef<HTMLInputElement>);
  private onChange: (value: number | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;
  private disabled = false;

  writeValue(value: number | null | undefined): void {
    const el = this.el.nativeElement;
    if (value == null || !Number.isFinite(Number(value))) {
      el.value = '';
      return;
    }
    el.value = formatMoneyTr(Number(value));
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.el.nativeElement.disabled = isDisabled;
  }

  @HostListener('input')
  onInput(): void {
    if (this.disabled) {
      return;
    }
    const el = this.el.nativeElement;
    const caretFromEnd = el.value.length - (el.selectionStart ?? el.value.length);
    const { display, value } = formatMoneyInputLive(el.value);
    el.value = display;
    const nextCaret = Math.max(0, display.length - caretFromEnd);
    el.setSelectionRange(nextCaret, nextCaret);
    this.onChange(display ? value : null);
  }

  @HostListener('blur')
  onBlur(): void {
    if (this.disabled) {
      return;
    }
    const el = this.el.nativeElement;
    const { display, value } = formatMoneyInputLive(el.value);
    if (!display) {
      el.value = '';
      this.onChange(null);
    } else {
      el.value = formatMoneyTr(value);
      this.onChange(value);
    }
    this.onTouched();
  }
}

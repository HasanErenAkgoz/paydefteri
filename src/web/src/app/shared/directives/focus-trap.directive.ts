import {
  AfterViewInit,
  Directive,
  ElementRef,
  EventEmitter,
  HostListener,
  OnDestroy,
  Output,
  inject,
} from '@angular/core';

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not([disabled])',
  'textarea:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

/**
 * Modal/dialog odak yönetimi: açılışta odağı içeri alır, Tab'ı içeride döndürür,
 * Escape'te kapanma sinyali verir ve kapanınca odağı modalı açan öğeye geri verir.
 *
 * Kullanım: dialog kutusuna `appFocusTrap` ekleyin ve `(dismissed)` çıktısını
 * mevcut kapatma metoduna bağlayın.
 */
@Directive({
  selector: '[appFocusTrap]',
  standalone: true,
})
export class FocusTrapDirective implements AfterViewInit, OnDestroy {
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);

  /** Escape'e basıldığında tetiklenir; kapatma kararını çağıran bileşen verir. */
  @Output() readonly dismissed = new EventEmitter<void>();

  private previouslyFocused: HTMLElement | null = null;

  ngAfterViewInit(): void {
    const active = document.activeElement;
    this.previouslyFocused = active instanceof HTMLElement ? active : null;

    const target = this.focusableElements()[0];
    if (target) {
      target.focus();
      return;
    }

    // İçeride odaklanacak bir şey yoksa kutunun kendisine odaklan ki
    // klavye kullanıcısı modalın arkasında kalmasın.
    const element = this.host.nativeElement;
    if (!element.hasAttribute('tabindex')) {
      element.setAttribute('tabindex', '-1');
    }
    element.focus();
  }

  ngOnDestroy(): void {
    // Modal kapanırken odağı tetikleyen öğeye geri ver; öğe DOM'dan kalktıysa dokunma.
    const previous = this.previouslyFocused;
    if (previous && document.contains(previous)) {
      previous.focus();
    }
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.dismissed.emit();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    const focusable = this.focusableElements();
    if (focusable.length === 0) {
      event.preventDefault();
      return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const active = document.activeElement;

    if (event.shiftKey && (active === first || !this.host.nativeElement.contains(active))) {
      event.preventDefault();
      last.focus();
      return;
    }

    if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private focusableElements(): HTMLElement[] {
    return Array.from(
      this.host.nativeElement.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)
    ).filter((element) => element.getClientRects().length > 0);
  }
}

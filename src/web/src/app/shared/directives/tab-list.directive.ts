import { Directive, ElementRef, HostListener, inject } from '@angular/core';

/**
 * `role="tablist"` kapsayıcısına klavye davranışı kazandırır: ok tuşlarıyla
 * geçiş, Home/End ve roving tabindex (yalnızca seçili sekme Tab sırasında).
 *
 * Neden directive: sekme düğmelerinin görünümü ekrandan ekrana farklı (ikon,
 * emoji, rozet, özel sınıflar). Ortak bir bileşen bu görsel farkları ezerdi;
 * directive ise markup'a dokunmadan yalnızca erişilebilirlik davranışını ekler.
 *
 * Kullanım: <div role="tablist" appTabList> ... <button role="tab"> ... </div>
 */
@Directive({
  selector: '[appTabList]',
  standalone: true,
})
export class TabListDirective {
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    const keys = ['ArrowRight', 'ArrowLeft', 'ArrowDown', 'ArrowUp', 'Home', 'End'];
    if (!keys.includes(event.key)) {
      return;
    }

    const tabs = this.tabs();
    if (tabs.length === 0) {
      return;
    }

    const current = tabs.indexOf(document.activeElement as HTMLElement);
    if (current === -1) {
      return;
    }

    event.preventDefault();
    const next = this.nextIndex(event.key, current, tabs.length);
    tabs[next].focus();
    // Sekme listelerinde odak değişimi seçimi de değiştirir (WAI-ARIA
    // "automatic activation"); mevcut ekranlar da tıklamayla böyle çalışıyor.
    tabs[next].click();
  }

  /** Yalnızca seçili sekme Tab sırasında kalır; diğerlerine ok tuşuyla gidilir. */
  @HostListener('focusin')
  syncRovingTabIndex(): void {
    for (const tab of this.tabs()) {
      tab.tabIndex = tab.getAttribute('aria-selected') === 'true' ? 0 : -1;
    }
  }

  private nextIndex(key: string, current: number, length: number): number {
    switch (key) {
      case 'Home':
        return 0;
      case 'End':
        return length - 1;
      case 'ArrowLeft':
      case 'ArrowUp':
        return (current - 1 + length) % length;
      default:
        return (current + 1) % length;
    }
  }

  private tabs(): HTMLElement[] {
    return Array.from(
      this.host.nativeElement.querySelectorAll<HTMLElement>('[role="tab"]:not([disabled])')
    ).filter((tab) => tab.getClientRects().length > 0);
  }
}

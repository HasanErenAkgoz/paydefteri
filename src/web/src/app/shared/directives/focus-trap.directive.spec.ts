import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FocusTrapDirective } from './focus-trap.directive';

@Component({
  standalone: true,
  imports: [FocusTrapDirective],
  template: `
    <button type="button" id="opener">Aç</button>
    @if (open()) {
      <div appFocusTrap (dismissed)="dismissedCount = dismissedCount + 1">
        <button type="button" id="first">İlk</button>
        <button type="button" id="last">Son</button>
      </div>
    }
  `,
})
class FocusTrapHostComponent {
  readonly open = signal(true);
  dismissedCount = 0;
}

describe('FocusTrapDirective', () => {
  let fixture: ComponentFixture<FocusTrapHostComponent>;
  let host: HTMLElement;

  const query = <T extends HTMLElement>(selector: string): T =>
    host.querySelector<T>(selector)!;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FocusTrapHostComponent] }).compileComponents();
    fixture = TestBed.createComponent(FocusTrapHostComponent);
    host = fixture.nativeElement as HTMLElement;
    document.body.appendChild(host);
  });

  afterEach(() => host.remove());

  it('moves focus into the dialog when it opens', () => {
    fixture.detectChanges();

    expect(document.activeElement).toBe(query('#first'));
  });

  it('emits dismissed on Escape so the component can close the dialog', () => {
    fixture.detectChanges();

    query('#first').dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
    );

    expect(fixture.componentInstance.dismissedCount).toBe(1);
  });

  it('cycles Tab from the last focusable back to the first', () => {
    fixture.detectChanges();
    query('#last').focus();

    const event = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    query('#last').dispatchEvent(event);

    expect(event.defaultPrevented).toBeTrue();
    expect(document.activeElement).toBe(query('#first'));
  });

  it('cycles Shift+Tab from the first focusable back to the last', () => {
    fixture.detectChanges();
    query('#first').focus();

    const event = new KeyboardEvent('keydown', {
      key: 'Tab',
      shiftKey: true,
      bubbles: true,
      cancelable: true,
    });
    query('#first').dispatchEvent(event);

    expect(event.defaultPrevented).toBeTrue();
    expect(document.activeElement).toBe(query('#last'));
  });

  it('restores focus to the element that was focused before opening', () => {
    const opener = query<HTMLButtonElement>('#opener');
    opener.focus();
    fixture.detectChanges();

    fixture.componentInstance.open.set(false);
    fixture.detectChanges();

    expect(document.activeElement).toBe(opener);
  });
});

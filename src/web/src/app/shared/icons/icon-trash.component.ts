import { Component, Input } from '@angular/core';

/** Shared trash / delete icon for destructive actions. */
@Component({
  selector: 'app-icon-trash',
  standalone: true,
  template: `
    <svg
      class="icon-trash"
      [attr.width]="size"
      [attr.height]="size"
      viewBox="0 0 24 24"
      aria-hidden="true"
      focusable="false"
    >
      <path
        d="M4 7h16M9 7V5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2m-9 0 1 12a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2l1-12M10 11v6M14 11v6"
        fill="none"
        stroke="currentColor"
        stroke-width="1.85"
        stroke-linecap="round"
        stroke-linejoin="round"
      />
    </svg>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        line-height: 0;
      }
      .icon-trash {
        display: block;
      }
    `,
  ],
})
export class IconTrashComponent {
  @Input() size = 16;
}

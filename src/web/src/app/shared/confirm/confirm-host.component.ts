import { Component, inject } from '@angular/core';
import { FocusTrapDirective } from '../directives/focus-trap.directive';
import { ConfirmService } from './confirm.service';

@Component({
  selector: 'app-confirm-host',
  standalone: true,
  imports: [FocusTrapDirective],
  templateUrl: './confirm-host.component.html',
  styleUrl: './confirm-host.component.scss',
})
export class ConfirmHostComponent {
  readonly confirm = inject(ConfirmService);

  cancel(): void {
    this.confirm.respond(false);
  }

  ok(): void {
    this.confirm.respond(true);
  }
}

import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  templateUrl: './toast-host.component.html',
  styleUrl: './toast-host.component.scss',
})
export class ToastHostComponent {
  readonly toasts = inject(ToastService);

  dismiss(id: number): void {
    this.toasts.dismiss(id);
  }
}

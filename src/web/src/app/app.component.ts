import { Component, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { PlanContextService } from './core/services/plan-context.service';
import { ToastHostComponent } from './shared/toast/toast-host.component';
import { ConfirmHostComponent } from './shared/confirm/confirm-host.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastHostComponent, ConfirmHostComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  readonly planContext = inject(PlanContextService);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );

  /** URL plan id, otherwise last opened plan (so Planlar page keeps the same navbar). */
  readonly planId = computed(() => {
    const match = this.url()?.match(/\/plans\/([0-9a-fA-F-]{36})\//);
    return match?.[1] ?? this.planContext.planId();
  });

  readonly isPlansManage = computed(() => {
    const path = (this.url() ?? '').split('?')[0];
    return path === '/plans';
  });

  readonly isProfile = computed(() => {
    const path = (this.url() ?? '').split('?')[0];
    return path === '/profile';
  });

  readonly showPlanTabs = computed(() => !!this.planId() && this.auth.isAuthenticated());

  readonly isAuthRoute = computed(() => {
    const path = this.url() ?? '';
    return (
      path.startsWith('/login') ||
      path.startsWith('/register') ||
      path.startsWith('/invite')
    );
  });

  logout(): void {
    this.planContext.clear();
    this.auth.logout();
  }
}

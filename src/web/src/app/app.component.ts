import { Component, OnInit, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { PlanContextService } from './core/services/plan-context.service';
import { SeoService } from './core/services/seo.service';
import { isExpensePlan, planHomeCommands } from './core/utils/plan-routes';
import { ToastHostComponent } from './shared/toast/toast-host.component';
import { ConfirmHostComponent } from './shared/confirm/confirm-host.component';
import { AppLifecycleService } from './core/platform/app-lifecycle.service';
import { ConnectivityService } from './core/platform/connectivity.service';

import { PrivacyService } from './core/services/privacy.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastHostComponent, ConfirmHostComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  private readonly lifecycle = inject(AppLifecycleService);
  readonly auth = inject(AuthService);
  readonly planContext = inject(PlanContextService);
  readonly connectivity = inject(ConnectivityService);
  readonly privacy = inject(PrivacyService);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url)
    ),
    { initialValue: this.router.url }
  );

  ngOnInit(): void {
    this.seo.init();
    void this.connectivity.initialize();
    void this.lifecycle.initialize();
  }

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

  readonly isExpensePlan = computed(() => isExpensePlan(this.planContext.planType()));

  readonly planHomeLink = computed(() => {
    const id = this.planId();
    if (!id) {
      return ['/plans'] as string[];
    }
    return planHomeCommands(id, this.planContext.planType());
  });

  readonly isAuthRoute = computed(() => {
    const path = (this.url() ?? '').split('?')[0];
    return (
      path === '/' ||
      path.startsWith('/login') ||
      path.startsWith('/register') ||
      path.startsWith('/invite')
    );
  });

  logout(): void {
    this.triggerHaptic();
    this.planContext.clear();
    this.auth.logout();
  }

  triggerHaptic(): void {
    if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
      try {
        navigator.vibrate(8);
      } catch {
        // Silently ignore if unsupported
      }
    }
  }
}

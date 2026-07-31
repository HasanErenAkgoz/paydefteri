import { Component, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { PlanContextService } from './core/services/plan-context.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
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

  readonly planId = computed(() => {
    const match = this.url()?.match(/\/plans\/([^/]+)\//);
    return match?.[1] ?? null;
  });

  readonly showPlanTabs = computed(() => !!this.planId() && this.auth.isAuthenticated());

  logout(): void {
    this.planContext.clear();
    this.auth.logout();
  }
}

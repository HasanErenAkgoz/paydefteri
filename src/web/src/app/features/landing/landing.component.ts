import { isPlatformBrowser } from '@angular/common';
import { Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
})
export class LandingComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly year = new Date().getFullYear();
  readonly mobileSlide = signal(0);

  ngOnInit(): void {
    if (this.isBrowser && this.auth.isAuthenticated()) {
      void this.router.navigateByUrl('/home');
    }
  }

  onMobileOnboardingScroll(event: Event): void {
    const track = event.currentTarget as HTMLElement;
    const slide = Math.round(track.scrollLeft / Math.max(track.clientWidth, 1));
    this.mobileSlide.set(Math.min(Math.max(slide, 0), 2));
  }

  showMobileSlide(track: HTMLElement, slide: number): void {
    const target = Math.min(Math.max(slide, 0), 2);
    track.scrollTo({ left: target * track.clientWidth, behavior: 'smooth' });
    this.mobileSlide.set(target);
  }
}

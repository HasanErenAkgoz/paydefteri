
import { Injectable, inject, DOCUMENT } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export interface SeoConfig {
  title: string;
  description: string;
  canonicalPath?: string;
  robots?: string;
  ogImage?: string;
  jsonLd?: Record<string, unknown> | Record<string, unknown>[];
}

const SITE = 'https://paydefteri.com';
const DEFAULT_OG = `${SITE}/og-image.png`;

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly router = inject(Router);
  private readonly doc = inject(DOCUMENT);
  private jsonLdEl: HTMLScriptElement | null = null;

  constructor() {
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.applyFromRoute());
  }

  /** Call once after app bootstrap so first paint route is covered. */
  init(): void {
    this.applyFromRoute();
  }

  apply(config: SeoConfig): void {
    const canonical = `${SITE}${config.canonicalPath ?? '/'}`;
    const robots = config.robots ?? 'index,follow';
    const image = config.ogImage ?? DEFAULT_OG;

    this.title.setTitle(config.title);
    this.meta.updateTag({ name: 'description', content: config.description });
    this.meta.updateTag({ name: 'robots', content: robots });
    this.meta.updateTag({ property: 'og:title', content: config.title });
    this.meta.updateTag({ property: 'og:description', content: config.description });
    this.meta.updateTag({ property: 'og:url', content: canonical });
    this.meta.updateTag({ property: 'og:image', content: image });
    this.meta.updateTag({ name: 'twitter:title', content: config.title });
    this.meta.updateTag({ name: 'twitter:description', content: config.description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
    this.setCanonical(canonical);

    if (config.jsonLd) {
      this.setJsonLd(config.jsonLd);
    } else {
      this.clearJsonLd();
    }
  }

  private applyFromRoute(): void {
    let route = this.router.routerState.root;
    while (route.firstChild) {
      route = route.firstChild;
    }
    const seo = route.snapshot.data['seo'] as SeoConfig | undefined;
    if (seo) {
      this.apply(seo);
    }
  }

  private setCanonical(url: string): void {
    let link = this.doc.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.doc.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.doc.head.appendChild(link);
    }
    link.setAttribute('href', url);
  }

  private setJsonLd(data: Record<string, unknown> | Record<string, unknown>[]): void {
    this.clearJsonLd();
    const el = this.doc.createElement('script');
    el.type = 'application/ld+json';
    el.text = JSON.stringify(data);
    this.doc.head.appendChild(el);
    this.jsonLdEl = el;
  }

  private clearJsonLd(): void {
    this.jsonLdEl?.remove();
    this.jsonLdEl = null;
    this.doc.querySelectorAll('script[type="application/ld+json"][data-seo]').forEach((n) => n.remove());
  }
}

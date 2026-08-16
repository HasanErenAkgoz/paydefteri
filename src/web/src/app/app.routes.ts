import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { SeoConfig } from './core/services/seo.service';

const landingSeo: SeoConfig = {
  title: 'PayDefteri | Ortak taksit ve ödeme takibi',
  description:
    'Ortak taksit planını Excel yerine tek ekranda takip edin. Kim ödedi, kalan pay, vade ve mahsuplaşma — PayDefteri ile şeffaf ortak ödeme takibi.',
  canonicalPath: '/',
  robots: 'index,follow',
  jsonLd: [
    {
      '@context': 'https://schema.org',
      '@type': 'WebApplication',
      name: 'PayDefteri',
      url: 'https://paydefteri.com/',
      applicationCategory: 'FinanceApplication',
      operatingSystem: 'Web',
      inLanguage: 'tr',
      description:
        'Ortak taksit ve ödeme planlarını tek ekranda takip eden web uygulaması. Mahsuplaşma, vade uyarısı ve PDF hesaplaşma raporu.',
      offers: {
        '@type': 'Offer',
        price: '0',
        priceCurrency: 'TRY',
      },
    },
    {
      '@context': 'https://schema.org',
      '@type': 'Organization',
      name: 'PayDefteri',
      url: 'https://paydefteri.com/',
    },
  ],
};

const publicSeo = (title: string, description: string, path: string): SeoConfig => ({
  title,
  description,
  canonicalPath: path,
  robots: 'index,follow',
});

const noindexSeo = (title: string): SeoConfig => ({
  title,
  description: 'PayDefteri hesap alanı.',
  robots: 'noindex,nofollow',
});

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () =>
      import('./features/landing/landing.component').then((m) => m.LandingComponent),
    data: { seo: landingSeo },
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
    data: {
      seo: publicSeo(
        'Giriş Yap | PayDefteri',
        'PayDefteri hesabınıza giriş yapın. Ortak taksit ve ödeme planlarınıza devam edin.',
        '/login'
      ),
    },
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
    data: {
      seo: publicSeo(
        'Kayıt Ol | PayDefteri',
        'PayDefteri’ye ücretsiz kayıt olun. Ortak ödeme planınızı dakikalar içinde kurun.',
        '/register'
      ),
    },
  },
  {
    path: 'invite/:token',
    loadComponent: () =>
      import('./features/invite/invite.component').then((m) => m.InviteComponent),
    data: { seo: noindexSeo('Davet | PayDefteri') },
  },
  {
    path: 'home',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Yönlendiriliyor | PayDefteri') },
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('./features/profile/profile.component').then((m) => m.ProfileComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Profil | PayDefteri') },
  },
  {
    path: 'plans',
    loadComponent: () =>
      import('./features/plans/plan-list.component').then((m) => m.PlanListComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Planlar | PayDefteri') },
  },
  {
    path: 'plans/:id/dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Takip Tablosu | PayDefteri') },
  },
  {
    path: 'plans/:id/expenses',
    loadComponent: () =>
      import('./features/expenses/expenses.component').then((m) => m.ExpensesComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Gider Takip Tablosu | PayDefteri') },
  },
  {
    path: 'plans/:id/setup',
    loadComponent: () =>
      import('./features/setup/setup.component').then((m) => m.SetupComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Kurulum | PayDefteri') },
  },
  {
    path: 'plans/:id/data',
    loadComponent: () => import('./features/data/data.component').then((m) => m.DataComponent),
    canActivate: [authGuard],
    data: { seo: noindexSeo('Yedek ve Rapor | PayDefteri') },
  },
  { path: '**', redirectTo: '' },
];

import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { DataComponent } from './features/data/data.component';
import { PlanListComponent } from './features/plans/plan-list.component';
import { SetupComponent } from './features/setup/setup.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'plans' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'plans', component: PlanListComponent, canActivate: [authGuard] },
  { path: 'plans/:id/dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'plans/:id/setup', component: SetupComponent, canActivate: [authGuard] },
  { path: 'plans/:id/data', component: DataComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'plans' },
];

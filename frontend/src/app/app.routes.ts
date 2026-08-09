import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { guestGuard } from './core/guards/guest-guard';
import { AppLayout } from './pages/app-layout/app-layout';
import { Home } from './pages/home/home';
import { Login } from './pages/login/login';
import { NotFound } from './pages/not-found/not-found';

export const routes: Routes = [
  {
    path: 'auth/login',
    canActivate: [guestGuard],
    component: Login,
  },
  {
    path: '',
    canActivate: [authGuard],
    component: AppLayout,
    children: [
      {
        path: '',
        redirectTo: 'home',
        pathMatch: 'full',
      },
      {
        path: 'home',
        component: Home,
      },
      {
        path: '**',
        component: NotFound,
      },
    ],
  },
];

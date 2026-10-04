import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { authorizationGuard } from './core/guards/authorization-guard';
import { guestGuard } from './core/guards/guest-guard';
import { Role } from './enums/role';
import { AppLayout } from './pages/app-layout/app-layout';
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
        loadComponent: () => import('./pages/home/home').then((m) => m.Home),
      },
      {
        path: 'admin-settings',
        children: [
          {
            path: 'users-management',
            canActivate: [authorizationGuard],
            data: {
              roles: [Role.SuperAdmin, Role.Admin],
            },
            loadComponent: () =>
              import('./pages/admin-settings/users-management/users-management').then(
                (m) => m.UsersManagement,
              ),
          },
        ],
      },
      {
        path: '**',
        component: NotFound,
      },
    ],
  },
];

import { Routes } from '@angular/router';

import { RoleName } from './core/enums/uam';
import { authGuard } from './core/guards/auth-guard';
import { authorizationGuard } from './core/guards/authorization-guard';
import { guestGuard } from './core/guards/guest-guard';

import { Login } from './pages/login/login';
import { NotFound } from './pages/not-found/not-found';

export const routes: Routes = [
  // Public routes
  {
    path: 'auth/login',
    component: Login,
    canActivate: [guestGuard],
  },

  // Protected routes
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'home',
      },
      {
        path: 'home',
        data: {
          breadcrumb: 'Home',
        },
        loadComponent: () => import('./pages/home/home').then((m) => m.Home),
      },
      {
        path: 'admin-settings',
        data: {
          breadcrumb: 'Admin Settings',
        },
        children: [
          {
            path: 'users-management',
            canActivate: [authorizationGuard],
            data: {
              roles: [RoleName.SuperAdmin, RoleName.Admin],
              breadcrumb: 'Users Management',
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

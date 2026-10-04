import { Routes } from '@angular/router';
import { Role } from './core/enums/role';
import { authGuard } from './core/guards/auth-guard';
import { authorizationGuard } from './core/guards/authorization-guard';
import { guestGuard } from './core/guards/guest-guard';
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
    data: {
      breadcrumb: 'Home',
      breadcrumbUrl: '/home',
    },
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
        data: { breadcrumb: 'Admin Settings' },
        children: [
          {
            path: 'users-management',
            canActivate: [authorizationGuard],
            data: {
              roles: [Role.SuperAdmin, Role.Admin],
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
        data: { breadcrumb: 'Not Found' },
      },
    ],
  },
];

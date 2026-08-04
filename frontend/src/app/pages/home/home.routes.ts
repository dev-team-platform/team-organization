import { Routes } from '@angular/router';
import { Home } from './home';

export const homeRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: Home,
  },
  {
    path: 'home',
    pathMatch: 'full',
    redirectTo: '',
  },
];

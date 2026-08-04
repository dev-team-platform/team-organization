import { Routes } from '@angular/router';
import { homeRoutes } from './pages/home/home.routes';
import { NotFound } from './pages/not-found/not-found';

export const routes: Routes = [
  ...homeRoutes,
  {
    path: '**',
    component: NotFound,
  },
];

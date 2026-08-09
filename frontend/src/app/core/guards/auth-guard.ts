import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { AuthService } from '../services/auth-service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.checkAuthentication().pipe(
    switchMap((isAuthenticated) => {
      if (!isAuthenticated) {
        return [
          router.createUrlTree(['/auth/login'], {
            queryParams: {
              returnUrl: state.url,
            },
          }),
        ];
      }

      return authService.initializeCsrf().pipe(map(() => true));
    }),
  );
};

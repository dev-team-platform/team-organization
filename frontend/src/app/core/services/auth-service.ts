import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { CurrentUser } from '../models/auth/current-user';

export const AUTH_ENDPOINTS = {
  LOGIN: '/api/v1/auth/login',
  LOGOUT: '/api/v1/auth/logout',
  CSRF: '/auth/csrf',
};

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly httpClient = inject(HttpClient);

  private readonly _currentUser = signal<CurrentUser | null>(null);

  readonly currentUser = this._currentUser.asReadonly();

  checkAuthentication(): Observable<boolean> {
    return this.httpClient
      .get<CurrentUser>('users/me', {
        withCredentials: true,
      })
      .pipe(
        tap((user) => {
          this._currentUser.set(user);
        }),
        map(() => true),
        catchError(() => {
          this._currentUser.set(null);
          return of(false);
        }),
      );
  }

  initializeCsrf(): Observable<void> {
    return this.httpClient.get<void>(AUTH_ENDPOINTS.CSRF, {
      withCredentials: true,
    });
  }

  login(returnUrl: string = '/'): void {
    window.location.href = AUTH_ENDPOINTS.LOGIN + '?returnUrl=' + encodeURIComponent(returnUrl);
  }

  logout(): void {
    window.location.href = AUTH_ENDPOINTS.LOGOUT;
  }
}

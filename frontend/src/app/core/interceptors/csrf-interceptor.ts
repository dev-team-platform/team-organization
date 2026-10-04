import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { getCookie } from '../utils/cookie';

const CSRF_COOKIE_NAME = 'XSRF-TOKEN';
const CSRF_HEADER_NAME = 'X-XSRF-TOKEN';

const CSRF_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

export const csrfInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  const isApiRequest = !/^https?:\/\//i.test(req.url) || req.url.startsWith(baseUrl);

  const requiresCsrf = CSRF_METHODS.has(req.method.toUpperCase());

  if (!isApiRequest || !requiresCsrf) {
    return next(req);
  }

  const csrfToken = getCookie(CSRF_COOKIE_NAME);

  if (!csrfToken) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        [CSRF_HEADER_NAME]: csrfToken,
      },
    }),
  );
};

import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (/^https?:\/\//i.test(req.url)) {
    return next(req);
  }

  let baseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  if (req.url.startsWith('auth')) {
    baseUrl = baseUrl.replace(/\/organizations(?=\/|$)/, '');
  }

  const endpoint = req.url.replace(/^\/+/, '');

  return next(
    req.clone({
      url: `${baseUrl}/${endpoint}`,
    }),
  );
};

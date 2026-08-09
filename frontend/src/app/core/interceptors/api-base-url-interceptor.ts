import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (/^https?:\/\//i.test(req.url)) {
    return next(req);
  }

  const baseUrl = environment.apiBaseUrl.replace(/\/+$/, '');
  const endpoint = req.url.replace(/^\/+/, '');

  return next(
    req.clone({
      url: `${baseUrl}/${endpoint}`,
    }),
  );
};

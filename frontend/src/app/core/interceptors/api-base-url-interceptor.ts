import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AUTH_ENDPOINTS } from '../services/auth-service';
import { ClientCorrelationService } from '../services/client-correlation-service';

export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationService = inject(ClientCorrelationService);

  let baseUrl = environment.apiBaseUrl.replace(/\/+$/, '');

  for (const authEndpoint of Object.values(AUTH_ENDPOINTS)) {
    if (req.url.startsWith(authEndpoint)) {
      baseUrl = baseUrl.replace(/\/organizations(?=\/|$)/, '');
      break;
    }
  }

  const endpoint = req.url.replace(/^\/+/, '');

  const clientActionId = correlationService.getClientActionId();
  const clientRequestId = correlationService.createRequestId();
  let headers = req.headers.set('X-Client-Request-Id', clientRequestId);

  if (clientActionId) {
    headers = headers.set('X-Client-Action-Id', clientActionId);
  }

  return next(
    req.clone({
      url: `${baseUrl}/${endpoint}`,
      headers,
      withCredentials: true,
    }),
  );
};

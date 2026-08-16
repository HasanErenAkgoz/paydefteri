import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

function readCookie(name: string): string | null {
  const prefix = `${encodeURIComponent(name)}=`;
  const value = document.cookie.split('; ').find((cookie) => cookie.startsWith(prefix));
  return value ? decodeURIComponent(value.slice(prefix.length)) : null;
}

export const xsrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (environment.mobile) {
    return next(req);
  }
  if (!UNSAFE_METHODS.has(req.method) || typeof document === 'undefined') {
    return next(req);
  }

  const token = readCookie('paydefteri_xsrf');
  return next(token ? req.clone({ setHeaders: { 'X-XSRF-TOKEN': token } }) : req);
};

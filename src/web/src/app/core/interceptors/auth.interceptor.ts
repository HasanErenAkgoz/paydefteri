import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MobileSessionService } from '../services/mobile-session.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const mobileSession = inject(MobileSessionService);
  if (!environment.mobile) {
    return next(req.clone({ withCredentials: true }));
  }

  const isMobileAuthRequest = req.url.includes('/mobile/v1/auth/');
  const accessToken = mobileSession.accessToken;
  const headers: Record<string, string> = {};
  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }
  if (mobileSession.sessionId) {
    headers['X-Mobile-Session-Id'] = mobileSession.sessionId;
  }

  return next(req.clone({ setHeaders: headers })).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isMobileAuthRequest) {
        return throwError(() => error);
      }

      return mobileSession.refreshAccessToken().pipe(
        switchMap((token) =>
          next(req.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
              ...(mobileSession.sessionId
                ? { 'X-Mobile-Session-Id': mobileSession.sessionId }
                : {}),
            },
          }))
        )
      );
    })
  );
};

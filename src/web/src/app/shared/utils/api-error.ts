/** Extract a user-facing message from ASP.NET ProblemDetails / HttpErrorResponse. */
export function apiErrorMessage(err: unknown, fallback: string): string {
  const error = (err as { error?: unknown } | null | undefined)?.error;
  if (error == null) {
    return fallback;
  }

  if (typeof error === 'string' && error.trim()) {
    return error;
  }

  if (typeof error !== 'object') {
    return fallback;
  }

  const body = error as {
    detail?: string;
    title?: string;
    errors?: Record<string, string[] | string> | string[];
  };

  const fromErrors = flattenProblemErrors(body.errors);
  if (fromErrors) {
    return fromErrors;
  }

  if (body.detail?.trim() && !isGenericValidationDetail(body.detail)) {
    return body.detail.trim();
  }

  return fallback;
}

function isGenericValidationDetail(detail: string): boolean {
  return /one or more validation errors occurred/i.test(detail);
}

function flattenProblemErrors(
  errors: Record<string, string[] | string> | string[] | undefined
): string | null {
  if (!errors) {
    return null;
  }

  const messages: string[] = [];
  if (Array.isArray(errors)) {
    for (const item of errors) {
      if (typeof item === 'string' && item.trim()) {
        messages.push(item.trim());
      }
    }
  } else {
    for (const value of Object.values(errors)) {
      if (typeof value === 'string' && value.trim()) {
        messages.push(value.trim());
      } else if (Array.isArray(value)) {
        for (const item of value) {
          if (typeof item === 'string' && item.trim()) {
            messages.push(item.trim());
          }
        }
      }
    }
  }

  return messages.length ? messages.join(' ') : null;
}

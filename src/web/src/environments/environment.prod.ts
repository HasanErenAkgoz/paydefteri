export const environment = {
  production: true,
  mobile: false,
  // Same-origin via nginx reverse proxy (/api → API container).
  apiUrl: '/api',
  // Google Cloud OAuth "Web application" client id. Empty hides the
  // Google button instead of rendering one that cannot work.
  googleClientId: '',
  // Native only — the browser never uses it.
  googleIosClientId: '',
};

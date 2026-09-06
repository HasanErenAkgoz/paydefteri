import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.paydefteri.app',
  appName: 'PayDefteri',
  webDir: 'dist/web/browser',
  server: {
    androidScheme: 'https',
    // No cleartext and no wildcard: the app reaches the API over TLS only.
    // Both were Play Store review blockers as well as MITM exposure.
    cleartext: false,
    allowNavigation: ['paydefteri.com'],
  },
  plugins: {
    CapacitorHttp: {
      enabled: true,
    },
    SplashScreen: {
      launchAutoHide: false,
      backgroundColor: '#0f172a',
      showSpinner: false,
    },
    Keyboard: {
      resize: 'body',
      resizeOnFullScreen: true,
    },
    StatusBar: {
      style: 'DARK',
      backgroundColor: '#00000000',
      overlaysWebView: true,
    },
  },
};

export default config;

export const environment = {
  production: true,
  mobile: true,
  apiUrl: 'https://paydefteri.com/api',
  // Native Google sign-in. The ID token the plugin returns is minted for the
  // WEB client id, which is what the API validates against — the iOS id only
  // identifies the app to Google. Both are public values.
  googleClientId: '623515520878-igetq8uafv653fofpsv1vodu4is6ievp.apps.googleusercontent.com',
  googleIosClientId: '623515520878-or9gj6l238th8dv0fiv8see321c08f80.apps.googleusercontent.com',
};

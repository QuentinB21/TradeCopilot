export const authConfig = {
  authority: import.meta.env.VITE_AUTH_AUTHORITY as string | undefined,
  clientId: import.meta.env.VITE_AUTH_CLIENT_ID as string | undefined,
  scope: (import.meta.env.VITE_AUTH_SCOPE as string | undefined) ?? "openid profile email"
};

export const isAuthEnabled = Boolean(authConfig.authority && authConfig.clientId);

export function authRedirectUri() {
  return `${window.location.origin}/auth/callback`;
}

export function authPostLogoutRedirectUri() {
  return window.location.origin;
}

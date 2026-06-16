export const authConfig = {
  authority: import.meta.env.VITE_AUTH_AUTHORITY as string | undefined,
  clientId: import.meta.env.VITE_AUTH_CLIENT_ID as string | undefined,
  scope: (import.meta.env.VITE_AUTH_SCOPE as string | undefined) ?? "openid profile email"
};

export const isAuthEnabled = Boolean(authConfig.authority && authConfig.clientId);

export function appBasePath() {
  return normalizeBasePath((import.meta.env.VITE_APP_BASE_PATH as string | undefined) ?? import.meta.env.BASE_URL ?? "/");
}

export function authRedirectUri() {
  return buildAppUrl("/auth/callback");
}

export function authPostLogoutRedirectUri() {
  return buildAppUrl("/");
}

export function authHomePath() {
  return appBasePath();
}

export function isAuthCallbackPath(pathname = window.location.pathname) {
  return normalizePath(pathname) === normalizePath(new URL(authRedirectUri()).pathname);
}

function buildAppUrl(path: string) {
  const basePath = appBasePath();
  const trimmedBasePath = basePath === "/" ? "" : basePath.replace(/\/$/, "");
  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${window.location.origin}${trimmedBasePath}${normalizedPath}`;
}

function normalizeBasePath(path: string) {
  const withLeadingSlash = path.startsWith("/") ? path : `/${path}`;
  return withLeadingSlash.endsWith("/") ? withLeadingSlash : `${withLeadingSlash}/`;
}

function normalizePath(path: string) {
  return path.length > 1 ? path.replace(/\/$/, "") : path;
}

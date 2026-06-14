let accessTokenProvider: (() => Promise<string | null>) | null = null;

export function setAccessTokenProvider(provider: (() => Promise<string | null>) | null) {
  accessTokenProvider = provider;
}

export async function readAccessToken() {
  return accessTokenProvider ? accessTokenProvider() : null;
}

export function notifyUnauthorized() {
  window.dispatchEvent(new CustomEvent("tradecopilot:unauthorized"));
}

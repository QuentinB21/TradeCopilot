let accessTokenProvider: (() => Promise<string | null>) | null = null;
let guestModeProvider: (() => boolean) | null = null;

export function setAccessTokenProvider(provider: (() => Promise<string | null>) | null) {
  accessTokenProvider = provider;
}

export function setGuestModeProvider(provider: (() => boolean) | null) {
  guestModeProvider = provider;
}

export async function readAccessToken() {
  return accessTokenProvider ? accessTokenProvider() : null;
}

export function readIsGuestMode() {
  return guestModeProvider ? guestModeProvider() : false;
}

export function notifyUnauthorized() {
  window.dispatchEvent(new CustomEvent("tradecopilot:unauthorized"));
}

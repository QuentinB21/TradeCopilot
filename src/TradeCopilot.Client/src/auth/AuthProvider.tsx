import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { UserManager, WebStorageStateStore, type User } from "oidc-client-ts";
import { authConfig, authHomePath, authPostLogoutRedirectUri, authRedirectUri, isAuthCallbackPath, isAuthEnabled } from "./authConfig";
import { setAccessTokenProvider, setGuestModeProvider } from "./tokenStore";

type AuthContextValue = {
  isAuthenticated: boolean;
  isLoading: boolean;
  loadingReason: "callback" | "startup" | null;
  isGuest: boolean;
  user: User | null;
  error: string | null;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
  enterGuestMode: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);
const GuestSessionKey = "tradecopilot.guest";

export function AuthProvider({ children }: { children: ReactNode }) {
  const manager = useMemo(() => {
    if (!isAuthEnabled) {
      return null;
    }

    return new UserManager({
      authority: authConfig.authority!,
      client_id: authConfig.clientId!,
      redirect_uri: authRedirectUri(),
      post_logout_redirect_uri: authPostLogoutRedirectUri(),
      response_type: "code",
      scope: authConfig.scope,
      automaticSilentRenew: true,
      userStore: new WebStorageStateStore({ store: window.sessionStorage })
    });
  }, []);
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setLoading] = useState(isAuthEnabled && isAuthCallbackPath());
  const [loadingReason, setLoadingReason] = useState<AuthContextValue["loadingReason"]>(
    isAuthEnabled && isAuthCallbackPath() ? "callback" : null
  );
  const [isGuest, setIsGuest] = useState(() => window.sessionStorage.getItem(GuestSessionKey) === "true" && !isAuthCallbackPath());
  const [error, setError] = useState<string | null>(null);

  const readValidUser = useCallback(async () => {
    if (!manager) {
      return null;
    }

    const currentUser = await manager.getUser();
    if (currentUser && !currentUser.expired) {
      return currentUser;
    }

    return null;
  }, [manager]);

  useEffect(() => {
    if (!manager) {
      setAccessTokenProvider(null);
      setGuestModeProvider(() => isGuest);
      setLoading(false);
      setLoadingReason(null);
    } else {
      setGuestModeProvider(() => isGuest);
    }

    if (!manager) {
      return () => {
        setGuestModeProvider(null);
      };
    }

    const authManager = manager;

    setAccessTokenProvider(async () => {
      if (isGuest) {
        return null;
      }

      const validUser = await readValidUser();
      return validUser?.access_token ?? null;
    });

    const handleUserLoaded = (loadedUser: User) => setUser(loadedUser);
    const handleUserUnloaded = () => setUser(null);
    const handleUnauthorized = () => {
      void authManager.removeUser().finally(() => setUser(null));
    };
    authManager.events.addUserLoaded(handleUserLoaded);
    authManager.events.addUserUnloaded(handleUserUnloaded);
    window.addEventListener("tradecopilot:unauthorized", handleUnauthorized);

    async function initialize() {
      try {
        if (isAuthCallbackPath()) {
          setIsGuest(false);
          window.sessionStorage.removeItem(GuestSessionKey);
          setLoading(true);
          setLoadingReason("callback");
          try {
            const callbackUser = await authManager.signinRedirectCallback();
            setUser(callbackUser);
          } catch (callbackException) {
            const existingUser = await authManager.getUser();
            if (existingUser && !existingUser.expired) {
              setUser(existingUser);
            } else {
              throw callbackException;
            }
          } finally {
            window.history.replaceState({}, document.title, authHomePath());
          }

          return;
        }

        setLoadingReason("startup");
        setUser(isGuest ? null : await readValidUser());
      } catch (exception) {
        setError(exception instanceof Error ? exception.message : "Authentification impossible.");
      } finally {
        setLoading(false);
        setLoadingReason(null);
      }
    }

    void initialize();

    return () => {
      authManager.events.removeUserLoaded(handleUserLoaded);
      authManager.events.removeUserUnloaded(handleUserUnloaded);
      window.removeEventListener("tradecopilot:unauthorized", handleUnauthorized);
      setAccessTokenProvider(null);
      setGuestModeProvider(null);
    };
  }, [isGuest, manager, readValidUser]);

  const signIn = useCallback(async () => {
    setIsGuest(false);
    window.sessionStorage.removeItem(GuestSessionKey);
    if (manager) {
      setError(null);
      await manager.signinRedirect();
    }
  }, [manager]);

  const signOut = useCallback(async () => {
    if (isGuest) {
      setIsGuest(false);
      window.sessionStorage.removeItem(GuestSessionKey);
      setUser(null);
      return;
    }

    if (manager) {
      setError(null);
      setUser(null);
      await manager.signoutRedirect();
    }
  }, [isGuest, manager]);

  const enterGuestMode = useCallback(() => {
    setError(null);
    setUser(null);
    setIsGuest(true);
    window.sessionStorage.setItem(GuestSessionKey, "true");
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    isAuthenticated: isGuest || !isAuthEnabled || Boolean(user && !user.expired),
    isLoading,
    loadingReason,
    isGuest,
    user,
    error,
    signIn,
    signOut,
    enterGuestMode
  }), [enterGuestMode, error, isGuest, isLoading, loadingReason, signIn, signOut, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}

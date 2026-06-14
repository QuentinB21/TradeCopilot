import { createContext, type ReactNode, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { UserManager, WebStorageStateStore, type User } from "oidc-client-ts";
import { authConfig, authPostLogoutRedirectUri, authRedirectUri, isAuthEnabled } from "./authConfig";
import { setAccessTokenProvider } from "./tokenStore";

type AuthContextValue = {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: User | null;
  error: string | null;
  signIn: () => Promise<void>;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

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
  const [isLoading, setLoading] = useState(isAuthEnabled);
  const [error, setError] = useState<string | null>(null);

  const readValidUser = useCallback(async () => {
    if (!manager) {
      return null;
    }

    const currentUser = await manager.getUser();
    if (currentUser && !currentUser.expired) {
      return currentUser;
    }

    try {
      return await manager.signinSilent();
    } catch {
      return null;
    }
  }, [manager]);

  useEffect(() => {
    if (!manager) {
      setAccessTokenProvider(null);
      setLoading(false);
      return;
    }
    const authManager = manager;

    setAccessTokenProvider(async () => {
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
        if (window.location.pathname === "/auth/callback") {
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
            window.history.replaceState({}, document.title, "/");
          }

          return;
        }

        setUser(await readValidUser());
      } catch (exception) {
        setError(exception instanceof Error ? exception.message : "Authentification impossible.");
      } finally {
        setLoading(false);
      }
    }

    void initialize();

    return () => {
      authManager.events.removeUserLoaded(handleUserLoaded);
      authManager.events.removeUserUnloaded(handleUserUnloaded);
      window.removeEventListener("tradecopilot:unauthorized", handleUnauthorized);
      setAccessTokenProvider(null);
    };
  }, [manager, readValidUser]);

  const signIn = useCallback(async () => {
    if (manager) {
      await manager.signinRedirect();
    }
  }, [manager]);

  const signOut = useCallback(async () => {
    if (manager) {
      await manager.signoutRedirect();
    }
  }, [manager]);

  const value = useMemo<AuthContextValue>(() => ({
    isAuthenticated: !isAuthEnabled || Boolean(user && !user.expired),
    isLoading,
    user,
    error,
    signIn,
    signOut
  }), [error, isLoading, signIn, signOut, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}

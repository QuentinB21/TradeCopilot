import { notifyUnauthorized, readAccessToken } from "../auth/tokenStore";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await readAccessToken();
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers
    }
  });

  if (!response.ok) {
    if (response.status === 401) {
      notifyUnauthorized();
    }

    const message = await response.text();
    throw new Error(message || `API ${path} returned ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export function getJson<T>(path: string) {
  return requestJson<T>(path);
}

export function postJson<T>(path: string, body: unknown) {
  return requestJson<T>(path, {
    method: "POST",
    body: JSON.stringify(body)
  });
}

export async function postForm<T>(path: string, body: FormData): Promise<T> {
  const token = await readAccessToken();
  const response = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    body,
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    }
  });

  if (!response.ok) {
    if (response.status === 401) {
      notifyUnauthorized();
    }

    const message = await response.text();
    throw new Error(message || `API ${path} returned ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function putJson<T>(path: string, body: unknown) {
  return requestJson<T>(path, {
    method: "PUT",
    body: JSON.stringify(body)
  });
}

export function deleteJson(path: string) {
  return requestJson<void>(path, { method: "DELETE" });
}

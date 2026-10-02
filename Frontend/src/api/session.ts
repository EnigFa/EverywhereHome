const TOKEN_KEY = "eh_token";
const listeners = new Set<() => void>();

let token: string | null = localStorage.getItem(TOKEN_KEY);

export function getToken(): string | null {
  return token;
}

export function currentUserId(): string | null {
  if (!token) {
    return null;
  }
  try {
    const payload = token.split(".")[1]?.replace(/-/g, "+").replace(/_/g, "/");
    if (!payload) {
      return null;
    }
    const json = JSON.parse(atob(payload)) as { sub?: string };
    return json.sub ?? null;
  } catch {
    return null;
  }
}

export function subscribeAuth(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function setSession(next: string | null): void {
  token = next;
  if (next) {
    localStorage.setItem(TOKEN_KEY, next);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
  for (const listener of listeners) {
    listener();
  }
  window.dispatchEvent(new Event("eh-auth"));
}

const TOKEN_KEY = "eh_token";
const listeners = new Set<() => void>();

let token: string | null = localStorage.getItem(TOKEN_KEY);

export function getToken(): string | null {
  return token;
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

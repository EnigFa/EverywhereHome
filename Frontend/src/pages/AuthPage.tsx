import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type AuthResponse } from "../api/client";
import { setSession } from "../api/session";

type Mode = "login" | "register";

type Providers = {
  google: boolean;
  facebook: boolean;
  apple: boolean;
};

const apiOrigin = import.meta.env.DEV
  ? (import.meta.env.VITE_API_ORIGIN || "http://localhost:5176")
  : "";

export function AuthPage({ mode }: { mode: Mode }) {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [providers, setProviders] = useState<Providers>({ google: false, facebook: false, apple: false });

  useEffect(() => {
    api<Providers>("/api/auth/providers")
      .then(setProviders)
      .catch(() => undefined);
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    if (mode === "register" && password !== confirmPassword) {
      setError("Паролі не збігаються.");
      return;
    }
    try {
      const path = mode === "login" ? "/api/auth/login" : "/api/auth/register";
      const body =
        mode === "login"
          ? { email, password }
          : { email, password, confirmPassword, displayName };
      const result = await api<AuthResponse>(path, {
        method: "POST",
        body: JSON.stringify(body)
      });
      setSession(result.accessToken);
      navigate("/", { replace: true });
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    }
  }

  function startExternal(provider: "google" | "apple" | "facebook") {
    const base = apiOrigin.replace(/\/$/, "");
    window.location.assign(`${base}/api/auth/external/${provider}`);
  }

  return (
    <div className="auth-overlay">
      <form className="auth-card" onSubmit={onSubmit}>
        <h1>{mode === "login" ? "Вхід" : "Реєстрація"}</h1>
        <p className="auth-lead">
          {mode === "login"
            ? "Увійдіть через пошту або акаунт Google, Apple чи Facebook."
            : "Створіть акаунт через пошту або швидко через Google, Apple чи Facebook."}
        </p>

        <div className="social-stack">
          <button type="button" className="social-btn" onClick={() => startExternal("google")}>
            Продовжити з Google
            {!providers.google && <span className="social-hint">потрібні ключі</span>}
          </button>
          <button type="button" className="social-btn" onClick={() => startExternal("apple")}>
            Продовжити з Apple
            {!providers.apple && <span className="social-hint">потрібні ключі</span>}
          </button>
          <button type="button" className="social-btn" onClick={() => startExternal("facebook")}>
            Продовжити з Facebook
            {!providers.facebook && <span className="social-hint">потрібні ключі</span>}
          </button>
        </div>

        <p className="auth-or">або</p>

        {mode === "register" && (
          <label>
            Імʼя
            <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
          </label>
        )}
        <label>
          Email
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </label>
        <label>
          Пароль
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
        </label>
        {mode === "register" && (
          <label>
            Підтвердження пароля
            <input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              minLength={6}
            />
          </label>
        )}
        {error && <p className="error">{error}</p>}
        <button type="submit" className="auth-primary">
          {mode === "login" ? "Увійти" : "Створити акаунт"}
        </button>
        <p className="auth-switch">
          {mode === "login" ? (
            <>
              Немає акаунта? <Link to="/register">Зареєструватися</Link>
            </>
          ) : (
            <>
              Вже є акаунт? <Link to="/login">Увійти</Link>
            </>
          )}
        </p>
      </form>
    </div>
  );
}

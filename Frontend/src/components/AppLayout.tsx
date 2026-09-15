import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useEffect, useLayoutEffect, useRef, useState } from "react";

import { getToken, setSession, subscribeAuth } from "../api/session";

export function AppLayout() {
  const navigate = useNavigate();
  const [signedIn, setSignedIn] = useState(Boolean(getToken()));
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    const sync = () => setSignedIn(Boolean(getToken()));
    sync();
    const unsubscribe = subscribeAuth(sync);
    window.addEventListener("storage", sync);
    return () => {
      unsubscribe();
      window.removeEventListener("storage", sync);
    };
  }, []);

  useEffect(() => {
    function onClick(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  function logout() {
    setSession(null);
    setSignedIn(false);
    setMenuOpen(false);
    navigate("/");
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <NavLink to="/" className="brand">
          EverywhereHome
        </NavLink>
        <nav className="app-nav">
          {signedIn ? (
            <div className="account-menu" ref={menuRef}>
              <button type="button" className="account-toggle" onClick={() => setMenuOpen((open) => !open)}>
                Профіль
              </button>
              {menuOpen && (
                <div className="account-panel">
                  <NavLink to="/profile" onClick={() => setMenuOpen(false)}>
                    Профіль
                  </NavLink>
                  <NavLink to="/host/listings" onClick={() => setMenuOpen(false)}>
                    Мої оголошення
                  </NavLink>
                  <NavLink to="/bookings" onClick={() => setMenuOpen(false)}>
                    Бронювання
                  </NavLink>
                  <NavLink to="/payments" onClick={() => setMenuOpen(false)}>
                    Мої способи оплати
                  </NavLink>
                  <button type="button" className="text-btn" onClick={logout}>
                    Вийти
                  </button>
                </div>
              )}
            </div>
          ) : (
            <>
              <NavLink to="/login">Увійти</NavLink>
              <NavLink to="/register">Реєстрація</NavLink>
            </>
          )}
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
      <footer className="app-footer">© 2026 EverywhereHome</footer>
    </div>
  );
}

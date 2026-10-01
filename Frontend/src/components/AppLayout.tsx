import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { getToken, setSession, subscribeAuth } from "../api/session";
import { useLanguage } from "../i18n";
import { LanguageSwitcher } from "./LanguageSwitcher";

export function AppLayout() {
  const navigate = useNavigate();
  const { t } = useLanguage();
  const [signedIn, setSignedIn] = useState(Boolean(getToken()));
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useLayoutEffect(() => {
    const sync = () => setSignedIn(Boolean(getToken()));
    sync();
    const unsubscribe = subscribeAuth(sync);
    window.addEventListener("storage", sync);
    return () => { unsubscribe(); window.removeEventListener("storage", sync); };
  }, []);

  useEffect(() => {
    const onClick = (event: MouseEvent) => { if (menuRef.current && !menuRef.current.contains(event.target as Node)) setMenuOpen(false); };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  function logout() { setSession(null); setSignedIn(false); setMenuOpen(false); navigate("/"); }

  return <div className="app-shell">
    <header className="app-header">
      <NavLink to="/" className="brand">EverywhereHome</NavLink>
      <div className="header-right">
        <nav className="app-nav">
          {signedIn ? <div className="account-menu" ref={menuRef}>
            <button type="button" className="account-toggle" onClick={() => setMenuOpen((open) => !open)}>{t("profile")}</button>
            {menuOpen && <div className="account-panel">
              <NavLink to="/profile" onClick={() => setMenuOpen(false)}>{t("profile")}</NavLink>
              <NavLink to="/host/listings" onClick={() => setMenuOpen(false)}>{t("myListings")}</NavLink>
              <NavLink to="/bookings" onClick={() => setMenuOpen(false)}>{t("bookings")}</NavLink>
              <NavLink to="/payments" onClick={() => setMenuOpen(false)}>{t("payments")}</NavLink>
              <NavLink to="/favorites" onClick={() => setMenuOpen(false)}>{t("favorites")}</NavLink>
              <NavLink to="/messages" onClick={() => setMenuOpen(false)}>{t("messages")}</NavLink>
              <NavLink to="/host/application" onClick={() => setMenuOpen(false)}>{t("hostApplication")}</NavLink>
              <NavLink to="/admin" onClick={() => setMenuOpen(false)}>{t("admin")}</NavLink>
              <button type="button" className="text-btn" onClick={logout}>{t("logout")}</button>
            </div>}
          </div> : <><NavLink to="/login">{t("login")}</NavLink><NavLink to="/register">{t("register")}</NavLink></>}
        </nav>
        <LanguageSwitcher />
      </div>
    </header>
    <main><Outlet /></main>
    <footer className="app-footer">{t("footer")}</footer>
  </div>;
}

import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { api, type Profile } from "../api/client";
import { getToken, setSession, subscribeAuth } from "../api/session";
import { useLanguage } from "../i18n";
import { Icon } from "./Icon";
import { LanguageSwitcher } from "./LanguageSwitcher";

export function AppLayout() {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { t, language } = useLanguage();
  const [signedIn, setSignedIn] = useState(Boolean(getToken()));
  const [isAdmin, setIsAdmin] = useState(false);
  const [isHost, setIsHost] = useState(false);
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
    if (!signedIn) {
      setIsAdmin(false);
      setIsHost(false);
      return;
    }
    api<Profile>("/api/profile").then((profile) => {
      setIsHost(profile.isHost);
      setIsAdmin(false);
      const token = getToken();
      return fetch("/api/admin/users?pageSize=1", { headers: token ? { Authorization: `Bearer ${token}` } : {} });
    }).then((response) => {
      if (response) setIsAdmin(response.ok);
    }).catch(() => setIsAdmin(false));
  }, [signedIn]);

  useEffect(() => {
    const onClick = (event: MouseEvent) => { if (menuRef.current && !menuRef.current.contains(event.target as Node)) setMenuOpen(false); };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  function logout() { setSession(null); setSignedIn(false); setMenuOpen(false); navigate("/"); }

  useEffect(() => {
    if (pathname.startsWith("/listings/") || pathname.startsWith("/users/")) return;
    const page = titleForPath(pathname, t);
    document.title = page ? `${page} · EverywhereHome` : "EverywhereHome";
  }, [pathname, language, t]);

  return <div className="app-shell">
    <header className="app-header">
      <NavLink to="/" className="brand" aria-label="EverywhereHome"><img className="brand-mark" src="/brand-logo.svg" alt="" /><span className="brand-name">EverywhereHome</span></NavLink>
      <div className="header-right">
        <nav className="app-nav">
          {isAdmin && <NavLink to="/admin" aria-label={t("admin")}><Icon name="shield" /><span className="nav-label">{t("admin")}</span></NavLink>}
          {isHost && <NavLink to="/host/listings/new" aria-label={t("newListing")}><Icon name="plus" /><span className="nav-label">{t("newListing")}</span></NavLink>}
          {signedIn ? <div className="account-menu" ref={menuRef}>
            <button type="button" className="account-toggle" aria-label={t("profile")} onClick={() => setMenuOpen((open) => !open)}><Icon name="user" /><span className="nav-label">{t("profile")}</span></button>
            {menuOpen && <div className="account-panel">
              <NavLink to="/profile" onClick={() => setMenuOpen(false)}><Icon name="user" />{t("profile")}</NavLink>
              <NavLink to="/host/listings" onClick={() => setMenuOpen(false)}><Icon name="grid" />{t("myListings")}</NavLink>
              <NavLink to="/bookings" onClick={() => setMenuOpen(false)}><Icon name="calendar" />{t("bookings")}</NavLink>
              <NavLink to="/payments" onClick={() => setMenuOpen(false)}><Icon name="card" />{t("payments")}</NavLink>
              <NavLink to="/favorites" onClick={() => setMenuOpen(false)}><Icon name="heart" />{t("favorites")}</NavLink>
              <NavLink to="/messages" onClick={() => setMenuOpen(false)}><Icon name="chat" />{t("messages")}</NavLink>
              <button type="button" className="text-btn" onClick={logout}><Icon name="logout" />{t("logout")}</button>
            </div>}
          </div> : <><NavLink to="/login" aria-label={t("login")}><Icon name="login" /><span className="nav-label">{t("login")}</span></NavLink><NavLink to="/register" aria-label={t("register")}><Icon name="register" /><span className="nav-label">{t("register")}</span></NavLink></>}
        </nav>
        <LanguageSwitcher />
      </div>
    </header>
    <main><Outlet /></main>
    <footer className="app-footer">
      <span>{t("footer")}</span>
      <nav className="footer-links">
        <Link to="/about"><Icon name="info" />{t("aboutUs")}</Link>
        <Link to="/jobs"><Icon name="briefcase" />{t("jobs")}</Link>
        <Link to="/rules"><Icon name="scroll" />{t("rulesPage")}</Link>
        <Link to="/contacts"><Icon name="mail" />{t("contacts")}</Link>
        <Link to="/privacy"><Icon name="lock" />{t("privacy")}</Link>
      </nav>
    </footer>
  </div>;
}

function titleForPath(pathname: string, t: (key: string) => string) {
  const exact: Record<string, string> = {
    "/bookings": "myBookings",
    "/profile": "profile",
    "/payments": "payments",
    "/favorites": "favorites",
    "/messages": "messages",
    "/host/application": "hostApplication",
    "/admin": "admin",
    "/admin/reports": "reports",
    "/admin/support": "support",
    "/admin/users": "users",
    "/admin/applications": "hostApplications",
    "/admin/listings": "editById",
    "/host/listings": "myListingsTitle",
    "/host/listings/new": "newListing",
    "/about": "aboutUs",
    "/jobs": "jobs",
    "/rules": "rulesPage",
    "/contacts": "contacts",
    "/privacy": "privacy",
    "/login": "loginTitle",
    "/register": "registerTitle",
    "/auth/callback": "finishingLogin"
  };
  if (exact[pathname]) return t(exact[pathname]);
  if (pathname.startsWith("/bookings/") && pathname.endsWith("/pay")) return t("paymentConfirmation");
  if (pathname.startsWith("/host/listings/")) return t("editListing");
  return null;
}

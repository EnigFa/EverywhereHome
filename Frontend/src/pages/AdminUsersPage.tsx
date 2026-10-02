import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type Profile } from "../api/client";
import { SideMenu } from "../components/SideMenu";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { UserLink } from "../components/UserLink";
import { currentUserId, getToken } from "../api/session";
import { useLanguage } from "../i18n";

type UserRow = { id: string; email: string; displayName: string; isHost: boolean; isAdmin: boolean; isBlocked: boolean; trustLevel: number; isChiefAdmin: boolean };
type Page = { items: UserRow[]; total: number; page: number; pageSize: number };

export function AdminUsersPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [blocked, setBlocked] = useState<"" | "true" | "false">("");
  const [trust, setTrust] = useState("");
  const [role, setRole] = useState("");
  const [page, setPage] = useState(1);
  const [data, setData] = useState<Page | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [sideOpen, setSideOpen] = useState(true);
  const [chief, setChief] = useState(false);
  const [pending, setPending] = useState<null | { title: string; text: string; run: () => Promise<void> }>(null);
  const [busy, setBusy] = useState(false);
  const me = currentUserId();

  function load(nextPage = page) {
    const params = new URLSearchParams({ page: String(nextPage), pageSize: "25" });
    if (search.trim()) params.set("q", search.trim());
    if (blocked) params.set("blocked", blocked);
    if (trust) params.set("trust", trust);
    if (role) params.set("role", role);
    api<Page>(`/api/admin/users?${params}`).then(setData).catch((e: Error) => setError(e.message));
  }

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    load(1);
    api<Profile>("/api/profile").then((profile) => setChief(profile.isChiefAdmin)).catch(() => setChief(false));
  }, [navigate]);

  function submit(event: FormEvent) {
    event.preventDefault();
    setPage(1);
    load(1);
  }

  async function setBlockedState(id: string, value: boolean) {
    setError(null);
    try {
      await api(`/api/admin/users/${id}/${value ? "block" : "unblock"}`, { method: "POST" });
      load(page);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function changeTrust(id: string, trustLevel: number) {
    setError(null);
    try {
      await api(`/api/admin/users/${id}/trust`, { method: "POST", body: JSON.stringify({ trustLevel }) });
      load(page);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function changeRole(id: string, role: string) {
    setError(null);
    try {
      await api(`/api/admin/users/${id}/role`, { method: "POST", body: JSON.stringify({ role }) });
      load(page);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  const trustName = (value: number) => value === 1 ? t("reliable") : value === 2 ? t("suspicious") : t("ordinary");
  const pages = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1;

  return (
    <section className="admin-section">
      <Link className="back-link" to="/admin">{t("backToAdmin")}</Link>
      <div className={sideOpen ? "admin-split" : "admin-split is-collapsed"}>
        <SideMenu open={sideOpen} onToggle={() => setSideOpen((open) => !open)} title={t("filters")} hideLabel={t("hideFilters")} showLabel={t("showFilters")}>
          <form className="side-form" onSubmit={submit}>
            <label>{t("search")}<input value={search} onChange={(e) => setSearch(e.target.value)} placeholder={t("userSearch")} /></label>
            <label>{t("accountState")}
              <select value={blocked} onChange={(e) => setBlocked(e.target.value as "" | "true" | "false")}>
                <option value="">{t("all")}</option>
                <option value="false">{t("active")}</option>
                <option value="true">{t("blocked")}</option>
              </select>
            </label>
            <label>{t("trust")}
              <select value={trust} onChange={(e) => setTrust(e.target.value)}>
                <option value="">{t("all")}</option>
                <option value="0">{t("ordinary")}</option>
                <option value="1">{t("reliable")}</option>
                <option value="2">{t("suspicious")}</option>
              </select>
            </label>
            <label>{t("role")}
              <select value={role} onChange={(e) => setRole(e.target.value)}>
                <option value="">{t("all")}</option>
                <option value="guest">{t("guestBadge")}</option>
                <option value="host">{t("hostBadge")}</option>
                <option value="admin">{t("adminBadge")}</option>
              </select>
            </label>
            <button className="auth-primary" type="submit">{t("apply")}</button>
          </form>
        </SideMenu>
        <div className="admin-work">
          <div className="section-heading">
            <h1>{t("users")}</h1>
            <p className="count-pill">{t("usersFound")}: {data?.total ?? 0}</p>
          </div>
          {error && <p className="error">{error}</p>}
      <ul className="bookings-list">
        {data?.items.map((user) => (
          <li key={user.id} className="booking-row">
            <div>
              <strong><UserLink id={user.id} name={user.displayName} /></strong>
              <p className="muted">{user.email}</p>
              <p className="muted">{user.isChiefAdmin ? t("chiefBadge") : user.isAdmin ? t("adminBadge") : user.isHost ? t("hostBadge") : t("guestBadge")} · {user.isBlocked ? t("blocked") : t("active")} · {trustName(user.trustLevel)}</p>
            </div>
            <div className="row-actions">
              <select
                value={user.trustLevel}
                onChange={(e) => {
                  const trustLevel = Number(e.target.value);
                  setPending({ title: t("trust"), text: t("confirmTrust"), run: () => changeTrust(user.id, trustLevel) });
                }}
              >
                <option value={0}>{t("ordinary")}</option>
                <option value={1}>{t("reliable")}</option>
                <option value={2}>{t("suspicious")}</option>
              </select>
              {!user.isChiefAdmin && user.id !== me && (chief || !user.isAdmin) && (
                <select
                  value={user.isAdmin ? "admin" : user.isHost ? "host" : "guest"}
                  onChange={(e) => {
                    const role = e.target.value;
                    setPending({ title: t("role"), text: t("confirmRole"), run: () => changeRole(user.id, role) });
                  }}
                >
                  <option value="guest">{t("guestBadge")}</option>
                  <option value="host">{t("hostBadge")}</option>
                  {chief && <option value="admin">{t("adminBadge")}</option>}
                </select>
              )}
              {!user.isAdmin && !user.isChiefAdmin && user.id !== me && (
                <button type="button" className="text-btn" onClick={() => setPending({
                  title: user.isBlocked ? t("unblock") : t("block"),
                  text: user.isBlocked ? t("confirmUnblock") : t("confirmBlock"),
                  run: () => setBlockedState(user.id, !user.isBlocked)
                })}>{user.isBlocked ? t("unblock") : t("block")}</button>
              )}
            </div>
          </li>
        ))}
      </ul>
      {pages > 1 && (
        <div className="pager">
          <button type="button" className="dropdown-toggle" disabled={page <= 1} onClick={() => { setPage(page - 1); load(page - 1); }}>{t("previous")}</button>
          <span>{page} / {pages}</span>
          <button type="button" className="dropdown-toggle" disabled={page >= pages} onClick={() => { setPage(page + 1); load(page + 1); }}>{t("next")}</button>
        </div>
      )}
        </div>
      </div>
      {pending && (
        <ConfirmDialog
          title={pending.title}
          text={pending.text}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          busy={busy}
          onClose={() => setPending(null)}
          onConfirm={() => {
            setBusy(true);
            pending.run().finally(() => {
              setBusy(false);
              setPending(null);
            });
          }}
        />
      )}
    </section>
  );
}

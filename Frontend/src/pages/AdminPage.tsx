import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { useLanguage } from "../i18n";

type Report = { id: string; target: number; status: number; text: string; listingId: string | null; listingTitle: string | null; reportedUserId: string | null; reportedUserName: string | null; reporterName: string; createdAtUtc: string };
type HostApplication = { id: string; fullName: string; status: number; adminNote: string | null; createdAtUtc: string; applicantName: string | null; applicantEmail: string | null };
type User = { id: string; email: string; displayName: string; isBlocked: boolean; isAdmin: boolean; isHost: boolean };
type Support = { id: string; kind: number; listingId: string | null; listingTitle: string | null; userId: string; userName: string; hostId: string | null };

export function AdminPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [reports, setReports] = useState<Report[]>([]);
  const [applications, setApplications] = useState<HostApplication[]>([]);
  const [users, setUsers] = useState<User[]>([]);
  const [support, setSupport] = useState<Support[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const [r, a, u, s] = await Promise.all([
        api<Report[]>("/api/admin/reports"),
        api<HostApplication[]>("/api/admin/host-applications"),
        api<User[]>("/api/admin/users"),
        api<Support[]>("/api/admin/support")
      ]);
      setReports(r); setApplications(a); setUsers(u); setSupport(s); setError(null);
    } catch (e) { setError(e instanceof Error ? e.message : t("adminAccess")); }
  }

  useEffect(() => { if (!localStorage.getItem("eh_token")) { navigate("/login"); return; } void load(); }, [navigate]);

  async function action(path: string) { try { await api(path, { method: "POST", body: JSON.stringify({}) }); await load(); } catch (e) { setError(e instanceof Error ? e.message : t("error")); } }

  return <section className="admin-page"><div className="section-heading"><div><h1>{t("admin")}</h1><p className="muted">{t("adminLead")}</p></div><button className="dropdown-toggle" type="button" onClick={() => void load()}>{t("refresh")}</button></div>{error && <p className="error">{error}</p>}
    <div className="admin-grid">
      <div className="profile-card"><h2>{t("hostApplications")}</h2>{applications.map(a => <div className="admin-row" key={a.id}><div><strong>{a.fullName}</strong><small>{a.applicantEmail ?? ""}</small></div><div>{a.status === 0 && <><button className="text-btn" onClick={() => void action(`/api/admin/host-applications/${a.id}/approve`)}>{t("approve")}</button><button className="text-btn" onClick={() => void action(`/api/admin/host-applications/${a.id}/reject`)}>{t("reject")}</button></>}</div></div>)}{applications.length===0&&<p className="muted">{t("none")}</p>}</div>
      <div className="profile-card"><h2>{t("reports")}</h2>{reports.map(r => <div className="admin-row" key={r.id}><div><strong>{r.listingTitle ?? r.reportedUserName ?? t("report")}</strong><small>{r.text}</small></div><div><button className="text-btn" onClick={() => void action(`/api/admin/reports/${r.id}/review`)}>{t("review")}</button>{r.listingId&&<button className="text-btn" onClick={() => void action(`/api/admin/reports/${r.id}/unpublish`)}>{t("unpublish")}</button>}{r.reportedUserId&&<button className="text-btn" onClick={() => void action(`/api/admin/reports/${r.id}/block`)}>{t("block")}</button>}</div></div>)}{reports.length===0&&<p className="muted">{t("none")}</p>}</div>
      <div className="profile-card"><h2>{t("users")}</h2>{users.map(u => <div className="admin-row" key={u.id}><div><strong>{u.displayName}</strong><small>{u.email}</small></div><button className="text-btn" onClick={() => void action(`/api/admin/users/${u.id}/${u.isBlocked ? "unblock" : "block"}`)}>{u.isBlocked ? t("unblock") : t("block")}</button></div>)}</div>
      <div className="profile-card"><h2>{t("support")}</h2>{support.map(s => <div className="admin-row" key={s.id}><div><strong>{s.userName}</strong><small>{s.listingTitle ?? t("support")}</small></div><a className="text-btn" href={`/messages?adminUserId=${s.userId}`}>{t("open")}</a></div>)}{support.length===0&&<p className="muted">{t("none")}</p>}</div>
    </div>
  </section>;
}

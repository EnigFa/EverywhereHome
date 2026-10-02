import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../api/client";
import { getToken } from "../api/session";
import { useLanguage } from "../i18n";

type Summary = {
  reportQueue: number;
  reportInProgress: number;
  supportQueue: number;
  supportInProgress: number;
  pendingApplications: number;
  blockedUsers: number;
};

export function AdminHomePage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [summary, setSummary] = useState<Summary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    api<Summary>("/api/admin/summary").then(setSummary).catch((e: Error) => setError(e.message));
  }, [navigate]);

  const cards = [
    { to: "/admin/reports", title: t("reports"), count: summary ? String(summary.reportQueue) : "—", text: summary ? `${t("queue")} · ${t("inProgress")}: ${summary.reportInProgress}` : t("loading") },
    { to: "/admin/support", title: t("support"), count: summary ? String(summary.supportQueue) : "—", text: summary ? `${t("queue")} · ${t("inProgress")}: ${summary.supportInProgress}` : t("loading") },
    { to: "/admin/users", title: t("users"), count: summary ? String(summary.blockedUsers) : "—", text: t("blocked") },
    { to: "/admin/applications", title: t("hostApplications"), count: summary ? String(summary.pendingApplications) : "—", text: t("queue") },
    { to: "/admin/listings", title: t("editById"), count: "ID", text: t("editByIdLead") }
  ];

  return (
    <section className="admin-home">
      <h1>{t("admin")}</h1>
      <p className="muted">{t("adminLead")}</p>
      {error && <p className="error">{error}</p>}
      <div className="admin-home-grid">
        {cards.map((card) => (
          <Link key={card.to} to={card.to} className="admin-home-card">
            <h2>{card.title}</h2>
            <strong>{card.count}</strong>
            <p>{card.text}</p>
            <span className="card-go">{t("open")}</span>
          </Link>
        ))}
      </div>
    </section>
  );
}

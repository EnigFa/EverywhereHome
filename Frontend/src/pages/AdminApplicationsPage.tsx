import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, mediaUrl } from "../api/client";
import { SideMenu } from "../components/SideMenu";
import { UserLink } from "../components/UserLink";
import { getToken } from "../api/session";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useLanguage } from "../i18n";

type Application = {
  id: string;
  fullName: string;
  documentUrl: string;
  status: number;
  adminNote: string | null;
  createdAtUtc: string;
  applicantName: string | null;
  applicantEmail: string | null;
  userId: string;
};

export function AdminApplicationsPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [items, setItems] = useState<Application[]>([]);
  const [sort, setSort] = useState("newest");
  const [openId, setOpenId] = useState<string | null>(null);
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [sideOpen, setSideOpen] = useState(true);
  const [ask, setAsk] = useState<null | { text: string; run: () => Promise<void> }>(null);

  function load() {
    api<Application[]>("/api/admin/host-applications").then(setItems).catch((e: Error) => setError(e.message));
  }

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    load();
  }, [navigate]);

  const sorted = useMemo(() => {
    const copy = [...items];
    if (sort === "oldest") copy.sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc));
    else if (sort === "pending") copy.sort((a, b) => a.status - b.status || b.createdAtUtc.localeCompare(a.createdAtUtc));
    else copy.sort((a, b) => b.createdAtUtc.localeCompare(a.createdAtUtc));
    return copy;
  }, [items, sort]);

  async function approve(id: string) {
    setError(null);
    try {
      await api(`/api/admin/host-applications/${id}/approve`, { method: "POST" });
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function reject(id: string) {
    setError(null);
    try {
      await api(`/api/admin/host-applications/${id}/reject`, { method: "POST", body: JSON.stringify({ note }) });
      setNote("");
      load();
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  const statusText = (status: number) => status === 1 ? t("approved") : status === 2 ? t("rejected") : t("pending");

  return (
    <section className="admin-section">
      <Link className="back-link" to="/admin">{t("backToAdmin")}</Link>
      <div className={sideOpen ? "admin-split" : "admin-split is-collapsed"}>
        <SideMenu open={sideOpen} onToggle={() => setSideOpen((open) => !open)} title={t("sort")} hideLabel={t("hideMenu")} showLabel={t("showMenu")}>
          <label className="side-form">{t("sort")}
            <select value={sort} onChange={(e) => setSort(e.target.value)}>
              <option value="newest">{t("newest")}</option>
              <option value="oldest">{t("oldest")}</option>
              <option value="pending">{t("pendingFirst")}</option>
            </select>
          </label>
        </SideMenu>
        <div className="admin-work">
          <h1>{t("hostApplications")}</h1>
          {error && <p className="error">{error}</p>}
          <ul className="bookings-list">
        {sorted.map((item) => (
          <li key={item.id} className="case-card">
            <button type="button" className="case-toggle" onClick={() => setOpenId(openId === item.id ? null : item.id)}>
              <strong>{item.fullName}</strong>
              <span>{statusText(item.status)} · {new Date(item.createdAtUtc).toLocaleString()}</span>
            </button>
            {openId === item.id && (
              <div className="case-body">
                <p><UserLink id={item.userId} name={item.applicantName || item.fullName} /> · {item.applicantEmail}</p>
                <p><a className="user-link" href={mediaUrl(item.documentUrl)} target="_blank" rel="noreferrer">{t("openDocument")}</a></p>
                {item.adminNote && <p>{t("note")}: {item.adminNote}</p>}
                {item.status === 0 && (
                  <>
                    <button type="button" className="auth-primary" onClick={() => setAsk({ text: t("confirmApprove"), run: () => approve(item.id) })}>{t("approve")}</button>
                    <label>{t("rejectReason")}<textarea value={note} onChange={(e) => setNote(e.target.value)} rows={3} /></label>
                    <button type="button" className="text-btn" onClick={() => setAsk({ text: t("confirmReject"), run: () => reject(item.id) })}>{t("reject")}</button>
                  </>
                )}
              </div>
            )}
          </li>
        ))}
      </ul>
        </div>
      </div>
      {ask && (
        <ConfirmDialog
          title={t("confirmAction")}
          text={ask.text}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          onClose={() => setAsk(null)}
          onConfirm={() => {
            const action = ask.run;
            setAsk(null);
            void action();
          }}
        />
      )}
    </section>
  );
}

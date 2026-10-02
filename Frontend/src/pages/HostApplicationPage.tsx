import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, apiForm } from "../api/client";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useLanguage } from "../i18n";

type Application = { id: string; fullName: string; documentUrl: string; status: number; adminNote: string | null; createdAtUtc: string; applicantName?: string; applicantEmail?: string };

export function HostApplicationPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [application, setApplication] = useState<Application | null>(null);
  const [fullName, setFullName] = useState("");
  const [document, setDocument] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [ask, setAsk] = useState(false);

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) { navigate("/login"); return; }
    api<Application | null>("/api/host/application").then((value) => { if (value) { setApplication(value); setFullName(value.fullName); } }).catch((e: Error) => setError(e.message));
  }, [navigate]);

  function request(event: FormEvent) {
    event.preventDefault();
    if (!document || !fullName.trim()) { setError(t("applicationRequired")); return; }
    setAsk(true);
  }

  async function submit() {
    if (!document) return;
    setSaving(true); setError(null);
    try {
      const form = new FormData(); form.append("fullName", fullName.trim()); form.append("document", document);
      setApplication(await apiForm<Application>("/api/host/application", form));
      setDocument(null);
    } catch (e) { setError(e instanceof Error ? e.message : t("error")); }
    finally { setSaving(false); setAsk(false); }
  }

  const status = application?.status === 1 ? t("approved") : application?.status === 2 ? t("rejected") : t("pending");
  return <section className="host-page"><h1>{t("hostApplication")}</h1><p className="muted">{t("hostApplicationLead")}</p>{application && <div className="profile-card"><strong>{t("status")}: {status}</strong>{application.adminNote && <p>{application.adminNote}</p>}<p className="muted">{new Date(application.createdAtUtc).toLocaleString()}</p></div>}{(!application || application.status === 2) && <form className="profile-card" onSubmit={request}><label>{t("fullName")}<input value={fullName} onChange={(e) => setFullName(e.target.value)} required /></label><label>{t("document")}<input type="file" accept="image/*,.pdf" onChange={(e) => setDocument(e.target.files?.[0] ?? null)} required /></label>{error && <p className="error">{error}</p>}<button className="auth-primary full" disabled={saving}>{saving ? t("sending") : t("submitApplication")}</button></form>}{ask && <ConfirmDialog title={t("confirmAction")} text={t("confirmSave")} confirmLabel={t("confirmAction")} closeLabel={t("close")} busy={saving} onClose={() => setAsk(false)} onConfirm={() => void submit()} />}</section>;
}

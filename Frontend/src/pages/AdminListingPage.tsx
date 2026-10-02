import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type HostListingEdit, type Profile } from "../api/client";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { ListingEditor } from "../components/ListingEditor";
import { getToken } from "../api/session";
import { useLanguage } from "../i18n";

export function AdminListingPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [rawId, setRawId] = useState("");
  const [loaded, setLoaded] = useState<HostListingEdit | null>(null);
  const [chief, setChief] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [askOff, setAskOff] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    api<Profile>("/api/profile").then((profile) => setChief(profile.isChiefAdmin)).catch(() => setChief(false));
  }, [navigate]);

  async function openListing(event: FormEvent) {
    event.preventDefault();
    const id = rawId.trim();
    if (!id) return;
    setError(null);
    setLoaded(null);
    try {
      setLoaded(await api<HostListingEdit>(`/api/admin/listings/${id}`));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function unpublish() {
    if (!loaded) return;
    setBusy(true);
    setError(null);
    try {
      await api(`/api/admin/listings/${loaded.id}/unpublish`, { method: "POST" });
      setLoaded({ ...loaded, isPublished: false });
      setAskOff(false);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
      setAskOff(false);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="admin-section">
      <Link className="back-link" to="/admin">{t("backToAdmin")}</Link>
      <h1>{t("editById")}</h1>
      <p className="muted">{t("editByIdLead")}</p>
      <form className="profile-card host-form" onSubmit={openListing}>
        <label className="full">
          {t("listingId")}
          <input value={rawId} onChange={(e) => setRawId(e.target.value)} placeholder={t("listingId")} required />
        </label>
        {error && <p className="error full">{error}</p>}
        <button className="auth-primary" type="submit">{t("open")}</button>
      </form>
      {loaded && (
        <div className="admin-work">
          <p className="staff-meta">{t("listingId")}: {loaded.id}</p>
          <p className="muted">{loaded.isPublished ? t("published") : t("draft")}</p>
          {loaded.isPublished && (
            <button type="button" className="text-btn" onClick={() => setAskOff(true)}>{t("deactivate")}</button>
          )}
          {chief && (
            <ListingEditor
              loadUrl={`/api/admin/listings/${loaded.id}`}
              saveUrl={`/api/admin/listings/${loaded.id}`}
              photoUrl={`/api/admin/listings/${loaded.id}`}
              onSaved={() => {
                api<HostListingEdit>(`/api/admin/listings/${loaded.id}`).then(setLoaded).catch(() => undefined);
              }}
            />
          )}
        </div>
      )}
      {askOff && (
        <ConfirmDialog
          title={t("deactivate")}
          text={t("confirmUnpublish")}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          busy={busy}
          onConfirm={() => void unpublish()}
          onClose={() => setAskOff(false)}
        />
      )}
    </section>
  );
}

import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type HostListing } from "../api/client";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useLanguage } from "../i18n";

export function HostListingsPage() {
  const { t, language } = useLanguage();
  const navigate = useNavigate();
  const [listings, setListings] = useState<HostListing[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const names = {
    uk: ["Гарні краєвиди", "Невеликі квартири", "Великі квартири", "Хостели", "Luxe", "У центрі міста", "Сільська місцевість", "Від дизайнера"],
    en: ["Great views", "Small apartments", "Large apartments", "Hostels", "Luxe", "City center", "Countryside", "Designer"]
  };

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    api<HostListing[]>("/api/host/listings").then(setListings).catch((e: Error) => setError(e.message));
  }, [navigate]);

  async function remove(id: string) {
    setError(null);
    setBusy(true);
    try {
      await api(`/api/host/listings/${id}`, { method: "DELETE" });
      setListings((current) => current.filter((item) => item.id !== id));
      setPendingId(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
      setPendingId(null);
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="host-page">
      <div className="host-head">
        <h1>{t("myListingsTitle")}</h1>
        <Link to="/host/listings/new" className="auth-primary">{t("newListing")}</Link>
      </div>
      {error && <p className="error">{error}</p>}
      {listings.length === 0 && !error && <p className="muted">{t("noListings")}</p>}
      <ul className="bookings-list">
        {listings.map((listing) => (
          <li key={listing.id} className="booking-row">
            <div>
              <strong>{listing.title}</strong>
              <p className="muted">
                {listing.city} · ${listing.pricePerNight} / {t("night")} · {names[language][listing.category] ?? t("category")} · {listing.isPublished ? t("published") : t("draft")}
              </p>
            </div>
            <div className="host-actions">
              <Link to={`/host/listings/${listing.id}`}>{t("edit")}</Link>
              <button type="button" className="text-btn" onClick={() => setPendingId(listing.id)}>{t("remove")}</button>
            </div>
          </li>
        ))}
      </ul>
      {pendingId && (
        <ConfirmDialog
          title={t("remove")}
          text={t("deleteConfirm")}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          busy={busy}
          onClose={() => setPendingId(null)}
          onConfirm={() => void remove(pendingId)}
        />
      )}
    </section>
  );
}

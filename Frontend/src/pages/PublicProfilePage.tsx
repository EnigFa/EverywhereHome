import { FormEvent, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, mediaUrl, type Profile } from "../api/client";
import { getToken, currentUserId } from "../api/session";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useLanguage } from "../i18n";

type PublicListing = { id: string; title: string; city: string; pricePerNight: number; rating: number; reviewCount: number; coverPhotoUrl: string | null };
type PublicProfile = {
  id: string;
  displayName: string;
  isHost: boolean;
  isAdmin: boolean;
  hometown: string | null;
  profession: string | null;
  languages: string | null;
  intro: string | null;
  listingCount: number;
  reviewCount: number;
  listings: PublicListing[];
  isBlocked: boolean;
  trustLevel: number;
  isChiefAdmin: boolean;
};

export function PublicProfilePage() {
  const { id } = useParams();
  const { t } = useLanguage();
  const [profile, setProfile] = useState<PublicProfile | null>(null);
  const [text, setText] = useState("");
  const [note, setNote] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [staff, setStaff] = useState(false);
  const [askBlock, setAskBlock] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    document.title = `${profile?.displayName || t("profile")} · EverywhereHome`;
  }, [profile?.displayName, t]);

  useEffect(() => {
    if (!getToken()) return;
    api<Profile>("/api/profile").then((mine) => {
      setStaff(mine.isAdmin);
    }).catch(() => undefined);
  }, []);

  useEffect(() => {
    if (!id) return;
    api<PublicProfile>(`/api/users/${id}`).then(setProfile).catch((e: Error) => setError(e.message));
  }, [id]);

  async function report(event: FormEvent) {
    event.preventDefault();
    if (!profile) return;
    setNote(null);
    try {
      await api("/api/reports", { method: "POST", body: JSON.stringify({ target: 1, reportedUserId: profile.id, listingId: null, text }) });
      setText("");
      setNote(t("reportSent"));
    } catch (e) {
      setNote(e instanceof Error ? e.message : t("error"));
    }
  }

  if (error) return <p className="error">{error}</p>;
  if (!profile) return <p>{t("loading")}</p>;

  const role = profile.isAdmin ? (profile.isChiefAdmin ? t("chiefBadge") : t("adminBadge")) : profile.isHost ? t("hostBadge") : t("guestBadge");
  const initial = (profile.displayName || "?").slice(0, 1).toUpperCase();

  return (
    <section className="public-profile">
      <div className="profile-hero">
        <div className="avatar" aria-hidden>{initial}</div>
        <div>
          <h1>
            {profile.displayName}
            {profile.isBlocked && <span className="blocked-badge">{t("blocked")}</span>}
          </h1>
          <p className="role-badge">{role}</p>
          {staff && (
            <p className="staff-meta">
              ID: {profile.id}
              <br />
              {t("trustStatus")}: {profile.trustLevel === 1 ? t("reliable") : profile.trustLevel === 2 ? t("suspicious") : t("ordinary")}
            </p>
          )}
          <p className="muted">{[profile.hometown, profile.profession, profile.languages].filter(Boolean).join(" · ")}</p>
        </div>
      </div>
      {profile.intro && <p className="profile-intro">{profile.intro}</p>}
      <div className="stat-row">
        <div><strong>{profile.listingCount}</strong><span>{t("listings")}</span></div>
        <div><strong>{profile.reviewCount}</strong><span>{t("reviews")}</span></div>
      </div>
      {profile.isHost && profile.listings.length > 0 && (
        <>
          <h2>{t("bestListings")}</h2>
          <div className="listing-grid">
            {profile.listings.map((listing) => (
              <Link key={listing.id} to={`/listings/${listing.id}`} className="listing-card">
                {listing.coverPhotoUrl ? <img className="listing-photo" src={mediaUrl(listing.coverPhotoUrl)} alt="" /> : <div className="listing-photo" />}
                <h2>{listing.title}</h2>
                <p>{listing.city}</p>
                <p>${listing.pricePerNight} / {t("night")}{listing.reviewCount > 0 ? ` · ${Number(listing.rating).toFixed(2)}` : ""}</p>
              </Link>
            ))}
          </div>
          <p><Link className="text-link" to={`/?hostId=${profile.id}&author=${encodeURIComponent(profile.displayName)}`}>{t("allAuthorListings")}</Link></p>
        </>
      )}
      {getToken() && currentUserId() !== profile.id && (
        <button type="button" className="auth-primary" onClick={async () => {
          const chat = await api<{ id: string }>(`/api/conversations/with/${profile.id}`, { method: "POST" });
          window.location.assign(`/messages?chat=${chat.id}`);
        }}>{t("writeUser")}</button>
      )}
      {staff && currentUserId() !== profile.id && !profile.isAdmin && (
        <button type="button" className="text-btn" onClick={() => setAskBlock(true)}>{profile.isBlocked ? t("unblock") : t("block")}</button>
      )}
      {open && (
        <div className="modal-backdrop" onClick={() => setOpen(false)}>
          <form className="modal-card" onClick={(event) => event.stopPropagation()} onSubmit={report}>
            <h2>{t("report")}</h2>
            <textarea value={text} onChange={(e) => setText(e.target.value)} rows={4} required placeholder={t("reviewText")} />
            {note && <p className="muted">{note}</p>}
            <div className="row-actions">
              <button className="auth-primary" type="submit">{t("sendReport")}</button>
              <button className="dropdown-toggle" type="button" onClick={() => setOpen(false)}>{t("close")}</button>
            </div>
          </form>
        </div>
      )}
      {askBlock && (
        <ConfirmDialog
          title={profile.isBlocked ? t("unblock") : t("block")}
          text={profile.isBlocked ? t("confirmUnblock") : t("confirmBlock")}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          busy={busy}
          onClose={() => setAskBlock(false)}
          onConfirm={() => {
            setBusy(true);
            api(`/api/admin/users/${profile.id}/${profile.isBlocked ? "unblock" : "block"}`, { method: "POST" })
              .then(() => api<PublicProfile>(`/api/users/${profile.id}`))
              .then(setProfile)
              .catch((e: Error) => setError(e.message))
              .finally(() => {
                setBusy(false);
                setAskBlock(false);
              });
          }}
        />
      )}
    </section>
  );
}

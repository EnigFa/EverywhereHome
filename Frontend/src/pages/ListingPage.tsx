import { FormEvent, useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api, mediaUrl, type Booking, type ListingDetail } from "../api/client";
import { getToken } from "../api/session";
import { useLanguage } from "../i18n";

export function ListingPage() {
  const { t } = useLanguage();
  const { id } = useParams();
  const navigate = useNavigate();
  const [listing, setListing] = useState<ListingDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [checkIn, setCheckIn] = useState("");
  const [checkOut, setCheckOut] = useState("");
  const [guests, setGuests] = useState(1);
  const [submitting, setSubmitting] = useState(false);
  const [photoIndex, setPhotoIndex] = useState(0);
  const [lightbox, setLightbox] = useState(false);
  const [favorite, setFavorite] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }
    api<ListingDetail>(`/api/listings/${id}`)
      .then((data) => {
        setListing(data);
        setPhotoIndex(0);
      })
      .catch((e: Error) => setError(e.message));
  }, [id]);

  const nights = useMemo(() => {
    if (!checkIn || !checkOut) {
      return 0;
    }
    const start = new Date(checkIn);
    const end = new Date(checkOut);
    const diff = Math.round((end.getTime() - start.getTime()) / 86_400_000);
    return diff > 0 ? diff : 0;
  }, [checkIn, checkOut]);

  const photos = (listing?.photoUrls ?? []).map(mediaUrl);
  const currentPhoto = photos[photoIndex];
  const canBrowse = photos.length > 1;

  function step(delta: number) {
    if (photos.length === 0) {
      return;
    }
    setPhotoIndex((current) => (current + delta + photos.length) % photos.length);
  }

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key === "ArrowLeft") {
        step(-1);
      }
      if (event.key === "ArrowRight") {
        step(1);
      }
      if (event.key === "Escape") {
        setLightbox(false);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [photos.length]);

  const signedIn = Boolean(getToken());

  useEffect(() => {
    if (!signedIn || !listing) return;
    api<Array<{ id: string }>>("/api/favorites")
      .then((items) => setFavorite(items.some((item) => item.id === listing.id)))
      .catch(() => undefined);
  }, [listing, signedIn]);

  if (error && !listing) {
    return <p className="error">{error}</p>;
  }
  if (!listing) {
    return <p>{t("loading")}</p>;
  }

  const stayTotal = nights * listing.pricePerNight;
  const total = nights > 0 ? stayTotal + listing.cleaningFee : 0;

  async function toggleFavorite() {
    if (!listing) return;
    try {
      await api(`/api/favorites/${listing.id}`, { method: favorite ? "DELETE" : "POST" });
      setFavorite((value) => !value);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function onBook(event: FormEvent) {
    event.preventDefault();
    if (!listing || nights <= 0) {
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const booking = await api<Booking>("/api/bookings", {
        method: "POST",
        body: JSON.stringify({
          listingId: listing.id,
          checkIn,
          checkOut,
          guestsCount: guests,
          paymentPlan: 0,
          messageToHost: null
        })
      });
      navigate(`/bookings/${booking.id}/pay`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <article className="listing-layout">
      <div>
        <h1>{listing.title}</h1>
        <p className="muted">
          {listing.rating > 0 ? `${listing.rating.toFixed(2)} · ${listing.reviewCount} ${t("reviews")} · ` : ""}
          {listing.city}, {listing.region}, {listing.country}
        </p>
        {currentPhoto ? (
          <div className="photo-stage">
            {canBrowse && (
              <button type="button" className="photo-nav prev" aria-label={t("previousPhoto")} onClick={() => step(-1)}>
                ‹
              </button>
            )}
            <button type="button" className="photo-current" onClick={() => setLightbox(true)}>
              <img src={currentPhoto} alt="" />
            </button>
            {canBrowse && (
              <button type="button" className="photo-nav next" aria-label={t("nextPhoto")} onClick={() => step(1)}>
                ›
              </button>
            )}
            {photos.length > 0 && (
              <span className="photo-count">
                {photoIndex + 1} / {photos.length}
              </span>
            )}
          </div>
        ) : (
          <div className="listing-hero" />
        )}
        <p className="listing-facts">
          {listing.maxGuests} {t("guestsCount")} · {listing.bedrooms} {t("bedrooms")} · {listing.beds} {t("beds")} · {listing.bathrooms} {t("bathrooms")}
        </p>
        <p>{t("host")} — {listing.host.displayName}</p>
        {signedIn && <div className="listing-actions"><button type="button" className="dropdown-toggle" onClick={() => void toggleFavorite()}>{favorite ? t("removeFavorite") : t("favorite")}</button><Link className="dropdown-toggle" to={`/messages?listingId=${listing.id}`}>{t("messageHost")}</Link></div>}
        <p>{listing.description}</p>
        <h2>{t("amenities")}</h2>
        <ul className="amenity-list">
          {listing.amenities.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
        <h2>{t("rules")}</h2>
        <p>{listing.houseRules}</p>
        <p>{listing.safetyRules}</p>
        <p>{listing.cancellationPolicy}</p>
      </div>
      <aside className="booking-widget">
        <p className="booking-price">
          ${listing.pricePerNight} <span>{t("perNight")}</span>
        </p>
        <form onSubmit={onBook}>
          <label>
            {t("checkIn")}
            <input type="date" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} required />
          </label>
          <label>
            {t("checkOut")}
            <input type="date" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} required />
          </label>
          <label>
            {t("guests")}
            <input
              type="number"
              min={1}
              max={listing.maxGuests}
              value={guests}
              onChange={(e) => setGuests(Number(e.target.value))}
            />
          </label>
          {nights > 0 && (
            <p>
              ${listing.pricePerNight} × {nights} = ${stayTotal.toFixed(0)}
              <br />
              {t("cleaning")} ${listing.cleaningFee}
              <br />
              <strong>{t("total")} ${total.toFixed(0)}</strong>
            </p>
          )}
          {error && <p className="error">{error}</p>}
          {signedIn ? (
            <button type="submit" className="auth-primary" disabled={nights <= 0 || submitting}>
              {submitting ? t("creating") : t("booking")}
            </button>
          ) : (
            <Link className="auth-primary book-login" to="/login">
              {t("bookingLogin")}
            </Link>
          )}
          <p className="auth-lead">{t("paymentStub")}</p>
        </form>
      </aside>
      {lightbox && currentPhoto && (
        <div className="lightbox" onClick={() => setLightbox(false)} role="dialog" aria-modal="true">
          {canBrowse && (
            <button
              type="button"
              className="photo-nav prev"
              aria-label={t("previousPhoto")}
              onClick={(event) => {
                event.stopPropagation();
                step(-1);
              }}
            >
              ‹
            </button>
          )}
          <img src={currentPhoto} alt="" onClick={(event) => event.stopPropagation()} />
          {canBrowse && (
            <button
              type="button"
              className="photo-nav next"
              aria-label={t("nextPhoto")}
              onClick={(event) => {
                event.stopPropagation();
                step(1);
              }}
            >
              ›
            </button>
          )}
          <button
            type="button"
            className="lightbox-close"
            aria-label={t("close")}
            onClick={(event) => {
              event.stopPropagation();
              setLightbox(false);
            }}
          >
            ×
          </button>
        </div>
      )}
    </article>
  );
}

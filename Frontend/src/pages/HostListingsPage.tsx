import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, CATEGORIES, type HostListing } from "../api/client";

export function HostListingsPage() {
  const navigate = useNavigate();
  const [listings, setListings] = useState<HostListing[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    api<HostListing[]>("/api/host/listings")
      .then(setListings)
      .catch((e: Error) => setError(e.message));
  }, [navigate]);

  async function remove(id: string) {
    if (!confirm("Видалити це оголошення?")) {
      return;
    }
    setError(null);
    try {
      await api(`/api/host/listings/${id}`, { method: "DELETE" });
      setListings((current) => current.filter((item) => item.id !== id));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    }
  }

  return (
    <section className="host-page">
      <div className="host-head">
        <h1>Мої оголошення</h1>
        <Link to="/host/listings/new" className="auth-primary">
          Нове оголошення
        </Link>
      </div>
      {error && <p className="error">{error}</p>}
      {listings.length === 0 && !error && <p className="muted">Поки немає оголошень.</p>}
      <ul className="bookings-list">
        {listings.map((listing) => (
          <li key={listing.id} className="booking-row">
            <div>
              <strong>{listing.title}</strong>
              <p className="muted">
                {listing.city} · ${listing.pricePerNight} / ніч ·{" "}
                {CATEGORIES.find((c) => c.value === listing.category)?.label ?? "Категорія"} ·{" "}
                {listing.isPublished ? "Опубліковано" : "Чернетка"}
              </p>
            </div>
            <div className="host-actions">
              <Link to={`/host/listings/${listing.id}`}>Редагувати</Link>
              <button type="button" className="text-btn" onClick={() => remove(listing.id)}>
                Видалити
              </button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}

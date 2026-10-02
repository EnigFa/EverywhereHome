import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, mediaUrl, type ListingCard } from "../api/client";
import { useLanguage } from "../i18n";

export function FavoritesPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [items, setItems] = useState<ListingCard[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setItems(await api<ListingCard[]>("/api/favorites"));
      setError(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    void load();
  }, [navigate]);

  async function remove(id: string) {
    try {
      await api(`/api/favorites/${id}`, { method: "DELETE" });
      setItems((current) => current.filter((item) => item.id !== id));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  return (
    <section>
      <div className="section-heading">
        <div>
          <h1>{t("favorites")}</h1>
          <p className="muted">{t("favoritesLead")}</p>
        </div>
      </div>
      {error && <p className="error">{error}</p>}
      {!error && items.length === 0 && <p className="muted">{t("noFavorites")}</p>}
      <div className="listing-grid">
        {items.map((item) => (
          <article key={item.id} className="listing-card favorite-card">
            <Link to={`/listings/${item.id}`}>
              {item.coverPhotoUrl ? <img className="listing-photo" src={mediaUrl(item.coverPhotoUrl)} alt="" /> : <div className="listing-photo" />}
              <h2>{item.title}</h2>
              <p>{item.city}, {item.region}</p>
              <p>${item.pricePerNight} / {t("night")}</p>
            </Link>
            <button type="button" className="text-btn" onClick={() => void remove(item.id)}>{t("removeFavorite")}</button>
          </article>
        ))}
      </div>
    </section>
  );
}

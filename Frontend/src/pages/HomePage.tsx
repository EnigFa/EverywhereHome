import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, mediaUrl, type ListingCard, type ListingSearchResult } from "../api/client";
import { CategoryPicker } from "../components/CategoryPicker";

type Filters = {
  q: string;
  city: string;
  guests: string;
  minPrice: string;
  maxPrice: string;
  categories: number[];
  checkIn: string;
  checkOut: string;
};

const emptyFilters: Filters = {
  q: "",
  city: "",
  guests: "",
  minPrice: "",
  maxPrice: "",
  categories: [],
  checkIn: "",
  checkOut: ""
};

export function HomePage() {
  const [listings, setListings] = useState<ListingCard[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [openFilters, setOpenFilters] = useState(false);

  function load(nextPage = page, nextSize = pageSize, nextFilters = filters) {
    const params = new URLSearchParams();
    if (nextFilters.q.trim()) {
      params.set("q", nextFilters.q.trim());
    }
    if (nextFilters.city.trim()) {
      params.set("city", nextFilters.city.trim());
    }
    if (nextFilters.guests) {
      params.set("guests", nextFilters.guests);
    }
    if (nextFilters.minPrice) {
      params.set("minPrice", nextFilters.minPrice);
    }
    if (nextFilters.maxPrice) {
      params.set("maxPrice", nextFilters.maxPrice);
    }
    if (nextFilters.checkIn && nextFilters.checkOut) {
      params.set("checkIn", nextFilters.checkIn);
      params.set("checkOut", nextFilters.checkOut);
    }
    for (const category of nextFilters.categories) {
      params.append("categories", String(category));
    }
    params.set("page", String(nextPage));
    params.set("pageSize", String(nextSize));
    setLoading(true);
    api<ListingSearchResult>(`/api/listings?${params}`)
      .then((result) => {
        setListings(result.items);
        setTotal(result.total);
        setPage(result.page);
        setPageSize(result.pageSize);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    load(1, pageSize, filters);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function onSearch(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setOpenFilters(false);
    load(1, pageSize, filters);
  }

  function resetFilters() {
    setFilters(emptyFilters);
  }

  function setFilter<K extends keyof Filters>(key: K, value: Filters[K]) {
    setFilters((current) => ({ ...current, [key]: value }));
  }

  const activeFilters =
    Number(Boolean(filters.q.trim())) +
    Number(Boolean(filters.city.trim())) +
    Number(Boolean(filters.guests)) +
    Number(Boolean(filters.minPrice || filters.maxPrice)) +
    Number(filters.categories.length > 0) +
    Number(Boolean(filters.checkIn && filters.checkOut));
  const pageCount = Math.max(1, Math.ceil(total / pageSize));

  return (
    <section className="home">
      <h1>Знайдіть оселю для наступної подорожі</h1>
      <form className="search-bar" onSubmit={onSearch}>
        <label className="title-search">
          Пошук
          <input
            value={filters.q}
            onChange={(e) => setFilter("q", e.target.value)}
            placeholder="Слова з назви"
          />
        </label>
        <button type="button" className="dropdown-toggle" onClick={() => setOpenFilters((open) => !open)}>
          {activeFilters > 0 ? `Фільтр (${activeFilters})` : "Фільтр"}
        </button>
        <button type="submit" className="auth-primary">
          Пошук
        </button>
      </form>
      {openFilters && (
        <div className="filter-panel">
          <label>
            Слова з назви
            <input value={filters.q} onChange={(e) => setFilter("q", e.target.value)} placeholder="студія, вигляд…" />
          </label>
          <label>
            Місто
            <input value={filters.city} onChange={(e) => setFilter("city", e.target.value)} placeholder="Київ" />
          </label>
          <label>
            Гості
            <input type="number" min={1} value={filters.guests} onChange={(e) => setFilter("guests", e.target.value)} />
          </label>
          <label>
            Ціна від, $
            <input type="number" min={0} value={filters.minPrice} onChange={(e) => setFilter("minPrice", e.target.value)} />
          </label>
          <label>
            Ціна до, $
            <input type="number" min={0} value={filters.maxPrice} onChange={(e) => setFilter("maxPrice", e.target.value)} />
          </label>
          <label>
            Прибуття
            <input type="date" value={filters.checkIn} onChange={(e) => setFilter("checkIn", e.target.value)} />
          </label>
          <label>
            Виїзд
            <input type="date" value={filters.checkOut} onChange={(e) => setFilter("checkOut", e.target.value)} />
          </label>
          <CategoryPicker values={filters.categories} onChange={(values) => setFilter("categories", values)} />
          <div className="filter-actions">
            <button type="button" className="text-btn" onClick={resetFilters}>
              Скинути
            </button>
            <button type="button" className="auth-primary" onClick={() => { setOpenFilters(false); load(1, pageSize, filters); }}>
              Застосувати
            </button>
          </div>
        </div>
      )}
      {error && <p className="error">{error}</p>}
      <div className="pager">
        <span>
          {total} оголошень
        </span>
        <label>
          На сторінці
          <select
            value={pageSize}
            onChange={(e) => {
              const size = Number(e.target.value);
              setPageSize(size);
              load(1, size, filters);
            }}
          >
            <option value={25}>25</option>
            <option value={50}>50</option>
            <option value={100}>100</option>
          </select>
        </label>
      </div>
      <div className="listing-grid">
        {listings.map((listing) => (
          <Link key={listing.id} to={`/listings/${listing.id}`} className="listing-card">
            {listing.coverPhotoUrl ? (
              <img className="listing-photo" src={mediaUrl(listing.coverPhotoUrl)} alt="" />
            ) : (
              <div className="listing-photo" />
            )}
            <h2>{listing.title}</h2>
            <p>
              {listing.city}, {listing.region}
            </p>
            <p>
              ${listing.pricePerNight} / ніч
              {listing.reviewCount > 0 ? ` · ${listing.rating.toFixed(2)} (${listing.reviewCount})` : ""}
            </p>
          </Link>
        ))}
      </div>
      {listings.length === 0 && !error && !loading && <p>Нічого не знайдено за цими фільтрами.</p>}
      {pageCount > 1 && (
        <div className="pager">
          <button type="button" className="dropdown-toggle" disabled={page <= 1} onClick={() => load(page - 1, pageSize, filters)}>
            Назад
          </button>
          <span>
            Сторінка {page} з {pageCount}
          </span>
          <button type="button" className="dropdown-toggle" disabled={page >= pageCount} onClick={() => load(page + 1, pageSize, filters)}>
            Далі
          </button>
        </div>
      )}
    </section>
  );
}

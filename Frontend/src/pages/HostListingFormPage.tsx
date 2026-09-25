import { ChangeEvent, FormEvent, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api, apiForm, mediaUrl, type HostListing, type HostListingEdit, type HostPhoto } from "../api/client";
import { CategoryPicker } from "../components/CategoryPicker";
import { useLanguage } from "../i18n";

const blank: HostListingEdit = {
  id: "",
  title: "",
  description: "",
  category: 0,
  categories: [],
  city: "",
  region: "",
  address: "",
  pricePerNight: 80,
  cleaningFee: 20,
  maxGuests: 2,
  bedrooms: 1,
  beds: 1,
  bathrooms: 1,
  houseRules: "",
  isPublished: true,
  photos: []
};

export function HostListingFormPage() {
  const { t } = useLanguage();
  const { id } = useParams();
  const isNew = !id || id === "new";
  const navigate = useNavigate();
  const [form, setForm] = useState<HostListingEdit>(blank);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    if (isNew) {
      return;
    }
    api<HostListingEdit>(`/api/host/listings/${id}`)
      .then((data) =>
        setForm({
          ...data,
          photos: data.photos ?? [],
          categories: data.categories?.length ? data.categories : data.category != null ? [data.category] : []
        })
      )
      .catch((e: Error) => setError(e.message));
  }, [id, isNew, navigate]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    if (form.categories.length === 0) {
      setError(t("chooseCategory"));
      return;
    }
    const body = JSON.stringify({
      title: form.title,
      description: form.description,
      category: form.categories[0] ?? 0,
      categories: form.categories,
      city: form.city,
      region: form.region,
      address: form.address,
      pricePerNight: Number(form.pricePerNight),
      cleaningFee: Number(form.cleaningFee),
      maxGuests: Number(form.maxGuests),
      bedrooms: Number(form.bedrooms),
      beds: Number(form.beds),
      bathrooms: Number(form.bathrooms),
      houseRules: form.houseRules,
      isPublished: form.isPublished
    });
    try {
      if (isNew) {
        const created = await api<HostListing>("/api/host/listings", { method: "POST", body });
        navigate(`/host/listings/${created.id}`);
      } else {
        await api(`/api/host/listings/${id}`, { method: "PUT", body });
        navigate("/host/listings");
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    }
  }

  function set<K extends keyof HostListingEdit>(key: K, value: HostListingEdit[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function onPhoto(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file || !id) {
      return;
    }
    setError(null);
    const data = new FormData();
    data.append("file", file);
    try {
      const photo = await apiForm<HostPhoto>(`/api/host/listings/${id}/photos`, data);
      setForm((current) => ({ ...current, photos: [...(current.photos ?? []), photo] }));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    }
  }

  async function removePhoto(photo: HostPhoto) {
    if (!id) {
      return;
    }
    setError(null);
    try {
      await api(`/api/host/listings/${id}/photos/${photo.id}`, { method: "DELETE" });
      setForm((current) => ({
        ...current,
        photos: (current.photos ?? []).filter((item) => item.id !== photo.id)
      }));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    }
  }

  return (
    <section className="host-page">
      <h1>{isNew ? t("newListing") : t("editListing")}</h1>
      <form className="profile-card host-form" onSubmit={onSubmit}>
        <label className="full">
          {t("title")}
          <input value={form.title} onChange={(e) => set("title", e.target.value)} required />
        </label>
        <label>
          {t("city")}
          <input value={form.city} onChange={(e) => set("city", e.target.value)} required />
        </label>
        <label>
          {t("region")}
          <input value={form.region} onChange={(e) => set("region", e.target.value)} />
        </label>
        <label className="full">
          {t("address")}
          <input value={form.address} onChange={(e) => set("address", e.target.value)} />
        </label>
        <div className="full">
          <CategoryPicker values={form.categories} onChange={(values) => set("categories", values)} />
        </div>
        <label>
          {t("priceNight")}
          <input
            type="number"
            min={1}
            value={form.pricePerNight}
            onChange={(e) => set("pricePerNight", Number(e.target.value))}
          />
        </label>
        <label>
          {t("cleaningPrice")}
          <input
            type="number"
            min={0}
            value={form.cleaningFee}
            onChange={(e) => set("cleaningFee", Number(e.target.value))}
          />
        </label>
        <label>
          {t("guests")}
          <input type="number" min={1} value={form.maxGuests} onChange={(e) => set("maxGuests", Number(e.target.value))} />
        </label>
        <label>
          {t("bedrooms")}
          <input type="number" min={0} value={form.bedrooms} onChange={(e) => set("bedrooms", Number(e.target.value))} />
        </label>
        <label>
          {t("beds")}
          <input type="number" min={0} value={form.beds} onChange={(e) => set("beds", Number(e.target.value))} />
        </label>
        <label>
          {t("bathrooms")}
          <input type="number" min={1} value={form.bathrooms} onChange={(e) => set("bathrooms", Number(e.target.value))} />
        </label>
        <label className="full">
          {t("description")}
          <textarea rows={4} value={form.description} onChange={(e) => set("description", e.target.value)} />
        </label>
        <label className="full">
          {t("houseRules")}
          <textarea rows={3} value={form.houseRules} onChange={(e) => set("houseRules", e.target.value)} />
        </label>
        <label className="flag">
          <input type="checkbox" checked={form.isPublished} onChange={(e) => set("isPublished", e.target.checked)} />
          {t("publishedLabel")}
        </label>
        {!isNew && (
          <div className="full photo-editor">
            <p>{t("photos")}</p>
            <div className="photo-thumbs">
              {(form.photos ?? []).map((photo) => (
                <div key={photo.id} className="photo-thumb">
                  <img src={mediaUrl(photo.url)} alt="" />
                  <button type="button" className="text-btn" onClick={() => removePhoto(photo)}>
                    {t("removePhoto")}
                  </button>
                </div>
              ))}
            </div>
            <input type="file" accept="image/jpeg,image/png,image/webp" onChange={onPhoto} />
          </div>
        )}
        {isNew && <p className="muted full">{t("addPhotoHint")}</p>}
        {error && <p className="error full">{error}</p>}
        <button type="submit" className="auth-primary full">
          {t("save")}
        </button>
      </form>
    </section>
  );
}

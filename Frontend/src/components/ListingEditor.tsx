import { ChangeEvent, FormEvent, useEffect, useState } from "react";
import { api, apiForm, mediaUrl, type HostListingEdit, type HostPhoto } from "../api/client";
import { CategoryPicker } from "./CategoryPicker";
import { ConfirmDialog } from "./ConfirmDialog";
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

type Props = {
  loadUrl: string;
  saveUrl: string;
  photoUrl: string;
  onSaved?: () => void;
};

export function ListingEditor({ loadUrl, saveUrl, photoUrl, onSaved }: Props) {
  const { t } = useLanguage();
  const [form, setForm] = useState<HostListingEdit>(blank);
  const [error, setError] = useState<string | null>(null);
  const [askSave, setAskSave] = useState(false);
  const [photoPending, setPhotoPending] = useState<HostPhoto | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api<HostListingEdit>(loadUrl)
      .then((data) =>
        setForm({
          ...data,
          photos: data.photos ?? [],
          categories: data.categories?.length ? data.categories : data.category != null ? [data.category] : []
        })
      )
      .catch((e: Error) => setError(e.message));
  }, [loadUrl]);

  function set<K extends keyof HostListingEdit>(key: K, value: HostListingEdit[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function requestSave(event: FormEvent) {
    event.preventDefault();
    if (form.categories.length === 0) {
      setError(t("chooseCategory"));
      return;
    }
    setError(null);
    setAskSave(true);
  }

  async function save() {
    setBusy(true);
    setError(null);
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
      await api(saveUrl, { method: "PUT", body });
      setAskSave(false);
      onSaved?.();
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
      setAskSave(false);
    } finally {
      setBusy(false);
    }
  }

  async function onPhoto(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    setError(null);
    const data = new FormData();
    data.append("file", file);
    try {
      const photo = await apiForm<HostPhoto>(`${photoUrl}/photos`, data);
      setForm((current) => ({ ...current, photos: [...(current.photos ?? []), photo] }));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function removePhoto() {
    if (!photoPending) return;
    setBusy(true);
    setError(null);
    try {
      await api(`${photoUrl}/photos/${photoPending.id}`, { method: "DELETE" });
      setForm((current) => ({ ...current, photos: (current.photos ?? []).filter((item) => item.id !== photoPending.id) }));
      setPhotoPending(null);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
      setPhotoPending(null);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="profile-card host-form" onSubmit={requestSave}>
      <h2>{t("editListing")}</h2>
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
        <input type="number" min={1} value={form.pricePerNight} onChange={(e) => set("pricePerNight", Number(e.target.value))} />
      </label>
      <label>
        {t("cleaningPrice")}
        <input type="number" min={0} value={form.cleaningFee} onChange={(e) => set("cleaningFee", Number(e.target.value))} />
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
      <div className="full photo-editor">
        <p>{t("photos")}</p>
        <div className="photo-thumbs">
          {(form.photos ?? []).map((photo) => (
            <div key={photo.id} className="photo-thumb">
              <img src={mediaUrl(photo.url)} alt="" />
              <button type="button" className="text-btn" onClick={() => setPhotoPending(photo)}>{t("removePhoto")}</button>
            </div>
          ))}
        </div>
        <input type="file" accept="image/jpeg,image/png,image/webp" onChange={onPhoto} />
      </div>
      {error && <p className="error full">{error}</p>}
      <button type="submit" className="auth-primary full">{t("save")}</button>
      {askSave && (
        <ConfirmDialog
          title={t("confirmAction")}
          text={t("confirmSaveListing")}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          busy={busy}
          onConfirm={() => void save()}
          onClose={() => setAskSave(false)}
        />
      )}
      {photoPending && (
        <ConfirmDialog
          title={t("removePhoto")}
          text={t("confirmRemovePhoto")}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          busy={busy}
          onConfirm={() => void removePhoto()}
          onClose={() => setPhotoPending(null)}
        />
      )}
    </form>
  );
}

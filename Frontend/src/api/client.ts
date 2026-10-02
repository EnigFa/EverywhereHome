import { getToken } from "./session";

export type ListingCard = {
  id: string;
  title: string;
  city: string;
  region: string;
  country: string;
  pricePerNight: number;
  rating: number;
  reviewCount: number;
  coverPhotoUrl: string | null;
  category: number;
  latitude: number;
  longitude: number;
};

export type ListingDetail = {
  id: string;
  title: string;
  description: string;
  city: string;
  region: string;
  country: string;
  address: string;
  pricePerNight: number;
  cleaningFee: number;
  rating: number;
  reviewCount: number;
  category: number;
  latitude: number;
  longitude: number;
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
  houseRules: string;
  safetyRules: string;
  cancellationPolicy: string;
  photoUrls: string[];
  amenities: string[];
  host: { id: string; displayName: string; avatarUrl: string | null; trustLevel: number };
  isPublished: boolean;
};

export type AuthResponse = {
  accessToken: string;
  displayName: string;
  email: string;
  isHost: boolean;
};

export type Profile = {
  email: string;
  displayName: string;
  avatarUrl: string | null;
  isHost: boolean;
  phoneVerified: boolean;
  phoneNumber: string | null;
  school: string | null;
  profession: string | null;
  languages: string | null;
  hometown: string | null;
  birthDecade: string | null;
  passion: string | null;
  uselessSkills: string | null;
  timeSink: string | null;
  favoriteSong: string | null;
  funFact: string | null;
  biographyTitle: string | null;
  pets: string | null;
  intro: string | null;
  emailConfirmed?: boolean;
  id?: string;
  isAdmin: boolean;
  isChiefAdmin: boolean;
};

export type HostListing = {
  id: string;
  title: string;
  city: string;
  pricePerNight: number;
  isPublished: boolean;
  category: number;
};

export type ListingSearchResult = {
  items: ListingCard[];
  total: number;
  page: number;
  pageSize: number;
};

export type HostListingEdit = {
  id: string;
  title: string;
  description: string;
  category: number;
  categories: number[];
  city: string;
  region: string;
  address: string;
  pricePerNight: number;
  cleaningFee: number;
  maxGuests: number;
  bedrooms: number;
  beds: number;
  bathrooms: number;
  houseRules: string;
  isPublished: boolean;
  photos: HostPhoto[];
};

export type HostPhoto = {
  id: string;
  url: string;
};

export type ChatMessage = { id: string; senderId: string; senderName: string; text: string; createdAtUtc: string; editedAtUtc?: string | null; isDeleted?: boolean; readByOther?: boolean };
export type Conversation = { id: string; kind: number; listingId: string | null; listingTitle: string | null; userId: string; userName: string; hostId: string | null; unreadCount: number };

export type Booking = {
  id: string;
  listingId: string;
  listingTitle: string;
  city: string;
  checkIn: string;
  checkOut: string;
  guestsCount: number;
  paymentPlan: number;
  totalAmount: number;
  status: number;
  messageToHost: string | null;
};

export function mediaUrl(url: string | null | undefined): string {
  if (!url) {
    return "";
  }
  if (/^https?:\/\//i.test(url)) {
    return url;
  }
  const origin = import.meta.env.VITE_API_ORIGIN ?? "";
  return `${origin}${url}`;
}

export async function apiForm<T>(path: string, form: FormData): Promise<T> {
  const token = getToken();
  const headers = new Headers();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  const response = await fetch(path, { method: "POST", body: form, headers });
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(error.message ?? "Помилка запиту");
  }
  return response.json() as Promise<T>;
}

export const BOOKING_STATUS = ["Очікує оплату", "Підтверджено", "Скасовано", "Завершено"];

export const CATEGORIES: { value: number; label: string }[] = [
  { value: 0, label: "Гарні краєвиди" },
  { value: 1, label: "Невеликі квартири" },
  { value: 2, label: "Великі квартири" },
  { value: 3, label: "Хостели" },
  { value: 4, label: "Luxe" },
  { value: 5, label: "У центрі міста" },
  { value: 6, label: "Сільська місцевість" },
  { value: 7, label: "Від дизайнера" }
];

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken();
  const headers = new Headers(init?.headers);
  if (init?.body) {
    headers.set("Content-Type", "application/json");
  }
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(path, { ...init, headers });
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(error.message ?? "Помилка запиту");
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

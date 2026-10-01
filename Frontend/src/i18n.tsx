import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export type Language = "uk" | "en";

type Dictionary = Record<string, string>;

const uk: Dictionary = {
  complete: "Завершити", cancel: "Скасувати",
  favorites: "Обране", favorite: "В обране", noFavorites: "Обраних оголошень поки немає.", removeFavorite: "Прибрати з обраного", noReviews: "Відгуків поки немає.", reviewPlaceholder: "Ваш відгук…", leaveReview: "Залишити відгук", hostApplication: "Стати господарем", hostApplications: "Заявки господарів", admin: "Адмін-панель", adminTitle: "Адмін-панель", noApplications: "Заявок немає.", openDocument: "Відкрити документ", applicationPending: "На розгляді", applicationApproved: "Схвалено", applicationRejected: "Відхилено", approve: "Схвалити", reject: "Відхилити", users: "Користувачі", administrator: "Адміністратор", hostRole: "Господар", guestRole: "Гість", blocked: "Заблокований", active: "Активний", block: "Заблокувати", unblock: "Розблокувати", fullName: "Повне імʼя", document: "Документ", sending: "Надсилаємо…", sendApplication: "Надіслати заявку",  profile: "Профіль", myListings: "Мої оголошення", bookings: "Бронювання", payments: "Мої способи оплати", logout: "Вийти", login: "Увійти", register: "Реєстрація",
  language: "Мова", ukrainian: "Українська", english: "English", close: "Закрити", loading: "Завантаження…", error: "Помилка",
  homeTitle: "Знайдіть оселю для наступної подорожі", search: "Пошук", titleWords: "Слова з назви", titlePlaceholder: "студія, вигляд…", city: "Місто", cityPlaceholder: "Київ", guests: "Гості", minPrice: "Ціна від, $", maxPrice: "Ціна до, $", checkIn: "Прибуття", checkOut: "Виїзд", filter: "Фільтр", reset: "Скинути", apply: "Застосувати", categories: "Категорії", listings: "оголошень", perPage: "На сторінці", nothingFound: "Нічого не знайдено за цими фільтрами.", previous: "Назад", next: "Далі", page: "Сторінка", night: "ніч", reviews: "відгуків",
  previousPhoto: "Попереднє фото", nextPhoto: "Наступне фото", guestsCount: "гості", bedrooms: "спальні", beds: "ліжка", bathrooms: "ванні", host: "Господар", amenities: "Зручності", rules: "Правила", booking: "Забронювати", bookingLogin: "Увійдіть, щоб забронювати", paymentStub: "Оплата — заглушка, гроші не списуються.", cleaning: "Прибирання", total: "Усього", creating: "Створюємо…", perNight: "ніч",
  myBookings: "Мої бронювання", pendingPayment: "Очікує оплати", cancelled: "Скасовано", completed: "Завершено", noBookings: "Поки немає бронювань.", pay: "Оплатити", confirmed: "Вже підтверджено", paymentConfirmation: "Підтвердження й оплата", paymentOption: "Варіант оплати", fullPayment: "Оплатити в повному обсязі", splitPayment: "Двома частинами", payWith: "Оплатити за допомогою", card: "Картка", paypal: "PayPal", cardNumber: "Номер картки", expiry: "Термін дії", cvv: "CVV", visualOnly: "Поля лише для вигляду. Дані картки нікуди не надсилаються.", confirming: "Підтверджуємо…", confirmStub: "Підтвердити (заглушка)",
  yourProfile: "Ваш профіль", name: "Імʼя", town: "Місто", profession: "Професія", languages: "Мови", about: "Про себе", save: "Зберегти", saved: "Зміни збережено.", aboutPlaceholder: "Коротко розкажіть гостям і господарям, хто ви.", languagePlaceholder: "Українська, англійська",
  myPaymentMethods: "Мої способи оплати", paymentStubInfo: "Оплата в проєкті — заглушка. Номер картки не зберігається, лише позначка для інтерфейсу.", remove: "Видалити", noMethods: "Ще немає збережених способів.", type: "Тип", last4: "Останні 4 цифри", add: "Додати",
  myListingsTitle: "Мої оголошення", newListing: "Нове оголошення", noListings: "Поки немає оголошень.", edit: "Редагувати", published: "Опубліковано", draft: "Чернетка", deleteConfirm: "Видалити це оголошення?", category: "Категорія",
  editListing: "Редагувати оголошення", title: "Назва", region: "Регіон", address: "Адреса", priceNight: "Ціна за ніч, $", cleaningPrice: "Прибирання, $", description: "Опис", houseRules: "Правила дому", publishedLabel: "Опубліковано", photos: "Фото", removePhoto: "Прибрати", addPhotoHint: "Після збереження можна буде додати фото.", chooseCategory: "Оберіть хоча б одну категорію.",
  loginTitle: "Вхід", registerTitle: "Реєстрація", loginLead: "Увійдіть через пошту або акаунт Google, Apple чи Facebook.", registerLead: "Створіть акаунт через пошту або швидко через Google, Apple чи Facebook.", continueGoogle: "Продовжити з Google", continueApple: "Продовжити з Apple", continueFacebook: "Продовжити з Facebook", keysNeeded: "потрібні ключі", or: "або", password: "Пароль", confirmPassword: "Підтвердження пароля", createAccount: "Створити акаунт", noAccount: "Немає акаунта?", alreadyAccount: "Вже є акаунт?", signup: "Зареєструватися", passwordsMismatch: "Паролі не збігаються.",
  loginFailed: "Не вдалося увійти", backToLogin: "Повернутися до входу", finishingLogin: "Завершуємо вхід…", footer: "© 2026 EverywhereHome"
};

const en: Dictionary = {
  complete: "Complete", cancel: "Cancel",
  favorites: "Favorites", favorite: "Favorite", noFavorites: "No favorite listings yet.", removeFavorite: "Remove favorite", noReviews: "No reviews yet.", reviewPlaceholder: "Your review…", leaveReview: "Leave a review", hostApplication: "Become a host", hostApplications: "Host applications", admin: "Admin panel", adminTitle: "Admin panel", noApplications: "No applications.", openDocument: "Open document", applicationPending: "Pending", applicationApproved: "Approved", applicationRejected: "Rejected", approve: "Approve", reject: "Reject", users: "Users", administrator: "Administrator", hostRole: "Host", guestRole: "Guest", blocked: "Blocked", active: "Active", block: "Block", unblock: "Unblock", fullName: "Full name", document: "Document", sending: "Sending…", sendApplication: "Send application",  profile: "Profile", myListings: "My listings", bookings: "Bookings", payments: "My payment methods", logout: "Log out", login: "Log in", register: "Sign up",
  language: "Language", ukrainian: "Українська", english: "English", close: "Close", loading: "Loading…", error: "Error",
  homeTitle: "Find a home for your next trip", search: "Search", titleWords: "Title keywords", titlePlaceholder: "studio, view…", city: "City", cityPlaceholder: "Kyiv", guests: "Guests", minPrice: "Minimum price, $", maxPrice: "Maximum price, $", checkIn: "Check-in", checkOut: "Check-out", filter: "Filter", reset: "Reset", apply: "Apply", categories: "Categories", listings: "listings", perPage: "Per page", nothingFound: "No listings match these filters.", previous: "Previous", next: "Next", page: "Page", night: "night", reviews: "reviews",
  previousPhoto: "Previous photo", nextPhoto: "Next photo", guestsCount: "guests", bedrooms: "bedrooms", beds: "beds", bathrooms: "bathrooms", host: "Host", amenities: "Amenities", rules: "Rules", booking: "Book now", bookingLogin: "Log in to book", paymentStub: "Payment is a demo; no money is charged.", cleaning: "Cleaning", total: "Total", creating: "Creating…", perNight: "night",
  myBookings: "My bookings", pendingPayment: "Pending payment", cancelled: "Cancelled", completed: "Completed", noBookings: "No bookings yet.", pay: "Pay", confirmed: "Already confirmed", paymentConfirmation: "Confirmation & payment", paymentOption: "Payment option", fullPayment: "Pay in full", splitPayment: "Pay in two parts", payWith: "Pay with", card: "Card", paypal: "PayPal", cardNumber: "Card number", expiry: "Expiry date", cvv: "CVV", visualOnly: "Visual-only fields. Card data is not sent anywhere.", confirming: "Confirming…", confirmStub: "Confirm (demo)",
  yourProfile: "Your profile", name: "Name", town: "City", profession: "Profession", languages: "Languages", about: "About you", save: "Save", saved: "Changes saved.", aboutPlaceholder: "Tell guests and hosts a little about yourself.", languagePlaceholder: "Ukrainian, English",
  myPaymentMethods: "My payment methods", paymentStubInfo: "Payments are a demo. Full card numbers are not stored; only a display label is saved.", remove: "Remove", noMethods: "No saved payment methods yet.", type: "Type", last4: "Last 4 digits", add: "Add",
  myListingsTitle: "My listings", newListing: "New listing", noListings: "No listings yet.", edit: "Edit", published: "Published", draft: "Draft", deleteConfirm: "Delete this listing?", category: "Category",
  editListing: "Edit listing", title: "Title", region: "Region", address: "Address", priceNight: "Price per night, $", cleaningPrice: "Cleaning, $", description: "Description", houseRules: "House rules", publishedLabel: "Published", photos: "Photos", removePhoto: "Remove", addPhotoHint: "You can add photos after saving.", chooseCategory: "Choose at least one category.",
  loginTitle: "Log in", registerTitle: "Sign up", loginLead: "Log in with email or your Google, Apple, or Facebook account.", registerLead: "Create an account with email or quickly with Google, Apple, or Facebook.", continueGoogle: "Continue with Google", continueApple: "Continue with Apple", continueFacebook: "Continue with Facebook", keysNeeded: "keys required", or: "or", password: "Password", confirmPassword: "Confirm password", createAccount: "Create account", noAccount: "Don't have an account?", alreadyAccount: "Already have an account?", signup: "Sign up", passwordsMismatch: "Passwords do not match.",
  loginFailed: "Sign-in failed", backToLogin: "Back to login", finishingLogin: "Finishing sign-in…", footer: "© 2026 EverywhereHome"
};

const dictionaries: Record<Language, Dictionary> = { uk, en };

type LanguageContextValue = { language: Language; setLanguage: (language: Language) => void; t: (key: string) => string };
const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(() => (localStorage.getItem("eh_language") as Language) || "uk");
  const setLanguage = (next: Language) => { setLanguageState(next); localStorage.setItem("eh_language", next); };
  useEffect(() => { document.documentElement.lang = language; }, [language]);
  const value = useMemo(() => ({ language, setLanguage, t: (key: string) => dictionaries[language][key] ?? dictionaries.uk[key] ?? key }), [language]);
  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage() {
  const context = useContext(LanguageContext);
  if (!context) throw new Error("useLanguage must be used inside LanguageProvider");
  return context;
}

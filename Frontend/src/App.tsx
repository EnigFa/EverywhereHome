import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { AuthCallbackPage } from "./pages/AuthCallbackPage";
import { AuthPage } from "./pages/AuthPage";
import { BookingsPage } from "./pages/BookingsPage";
import { HomePage } from "./pages/HomePage";
import { ListingPage } from "./pages/ListingPage";
import { PayPage } from "./pages/PayPage";
import { HostListingFormPage } from "./pages/HostListingFormPage";
import { HostListingsPage } from "./pages/HostListingsPage";
import { PaymentMethodsPage } from "./pages/PaymentMethodsPage";
import { ProfilePage } from "./pages/ProfilePage";
import { FavoritesPage } from "./pages/FavoritesPage";
import { AdminPage } from "./pages/AdminPage";
import { HostApplicationPage } from "./pages/HostApplicationPage";

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/listings/:id" element={<ListingPage />} />
          <Route path="/bookings" element={<BookingsPage />} />
          <Route path="/bookings/:id/pay" element={<PayPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/payments" element={<PaymentMethodsPage />} />
          <Route path="/favorites" element={<FavoritesPage />} />
          <Route path="/host/application" element={<HostApplicationPage />} />
          <Route path="/admin" element={<AdminPage />} />
          <Route path="/host/listings" element={<HostListingsPage />} />
          <Route path="/host/listings/new" element={<HostListingFormPage />} />
          <Route path="/host/listings/:id" element={<HostListingFormPage />} />
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/register" element={<AuthPage mode="register" />} />
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

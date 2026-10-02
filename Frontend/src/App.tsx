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
import { PublicProfilePage } from "./pages/PublicProfilePage";
import { FavoritesPage } from "./pages/FavoritesPage";
import { MessagesPage } from "./pages/MessagesPage";
import { HostApplicationPage } from "./pages/HostApplicationPage";
import { AdminApplicationsPage } from "./pages/AdminApplicationsPage";
import { AdminHomePage } from "./pages/AdminHomePage";
import { AdminListingPage } from "./pages/AdminListingPage";
import { AdminReportsPage } from "./pages/AdminReportsPage";
import { AdminSupportPage } from "./pages/AdminSupportPage";
import { AdminUsersPage } from "./pages/AdminUsersPage";
import { InfoPage } from "./pages/InfoPage";
import { ProfilePage } from "./pages/ProfilePage";

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
          <Route path="/messages" element={<MessagesPage />} />
          <Route path="/host/application" element={<HostApplicationPage />} />
          <Route path="/admin" element={<AdminHomePage />} />
          <Route path="/admin/reports" element={<AdminReportsPage />} />
          <Route path="/admin/support" element={<AdminSupportPage />} />
          <Route path="/admin/users" element={<AdminUsersPage />} />
          <Route path="/admin/applications" element={<AdminApplicationsPage />} />
          <Route path="/admin/listings" element={<AdminListingPage />} />
          <Route path="/users/:id" element={<PublicProfilePage />} />
          <Route path="/host/listings" element={<HostListingsPage />} />
          <Route path="/host/listings/new" element={<HostListingFormPage />} />
          <Route path="/host/listings/:id" element={<HostListingFormPage />} />
          <Route path="/about" element={<InfoPage titleKey="aboutUs" />} />
          <Route path="/jobs" element={<InfoPage titleKey="jobs" />} />
          <Route path="/rules" element={<InfoPage titleKey="rulesPage" />} />
          <Route path="/contacts" element={<InfoPage titleKey="contacts" />} />
          <Route path="/privacy" element={<InfoPage titleKey="privacy" />} />
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/register" element={<AuthPage mode="register" />} />
          <Route path="/auth/callback" element={<AuthCallbackPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

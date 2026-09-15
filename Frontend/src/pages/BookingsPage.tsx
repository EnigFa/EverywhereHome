import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, BOOKING_STATUS, type Booking } from "../api/client";

export function BookingsPage() {
  const navigate = useNavigate();
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    api<Booking[]>("/api/bookings")
      .then(setBookings)
      .catch((e: Error) => setError(e.message));
  }, [navigate]);

  return (
    <section>
      <h1>Мої бронювання</h1>
      {error && <p className="error">{error}</p>}
      {bookings.length === 0 && !error && <p>Поки немає бронювань.</p>}
      <ul className="bookings-list">
        {bookings.map((booking) => (
          <li key={booking.id} className="booking-row">
            <div>
              <Link to={`/listings/${booking.listingId}`}>{booking.listingTitle}</Link>
              <p>
                {booking.city} · {booking.checkIn} — {booking.checkOut} · ${booking.totalAmount}
              </p>
              <p>{BOOKING_STATUS[booking.status] ?? booking.status}</p>
            </div>
            {booking.status === 0 && (
              <Link className="auth-primary book-login" to={`/bookings/${booking.id}/pay`}>
                Оплатити
              </Link>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}

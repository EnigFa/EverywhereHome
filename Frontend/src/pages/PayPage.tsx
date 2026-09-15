import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api, type Booking } from "../api/client";

export function PayPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [booking, setBooking] = useState<Booking | null>(null);
  const [method, setMethod] = useState(0);
  const [plan, setPlan] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    if (!id) {
      return;
    }
    api<Booking>(`/api/bookings/${id}`)
      .then(setBooking)
      .catch((e: Error) => setError(e.message));
  }, [id, navigate]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    if (!id) {
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await api<Booking>(`/api/bookings/${id}/pay-stub`, {
        method: "POST",
        body: JSON.stringify({ method })
      });
      navigate("/bookings");
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    } finally {
      setSubmitting(false);
    }
  }

  if (error && !booking) {
    return <p className="error">{error}</p>;
  }
  if (!booking) {
    return <p>Завантаження…</p>;
  }

  if (booking.status === 1) {
    return (
      <section className="pay-page">
        <h1>Вже підтверджено</h1>
        <p>
          {booking.listingTitle} · {booking.checkIn} — {booking.checkOut}
        </p>
        <Link to="/bookings">Мої бронювання</Link>
      </section>
    );
  }

  const todayShare = (booking.totalAmount * 0.2).toFixed(2);
  const laterShare = (booking.totalAmount * 0.8).toFixed(2);

  return (
    <section className="pay-page">
      <h1>Підтвердження й оплата</h1>
      <p>
        {booking.listingTitle}, {booking.city}
      </p>
      <p>
        {booking.checkIn} — {booking.checkOut} · {booking.guestsCount} гість
      </p>
      <p>
        <strong>Усього ${booking.totalAmount}</strong>
      </p>
      <form onSubmit={onSubmit} className="pay-form">
        <fieldset>
          <legend>Варіант оплати</legend>
          <label>
            <input type="radio" checked={plan === 0} onChange={() => setPlan(0)} />
            Оплатити в повному обсязі (${booking.totalAmount})
          </label>
          <label>
            <input type="radio" checked={plan === 1} onChange={() => setPlan(1)} />
            Двома частинами (${todayShare} зараз, ${laterShare} пізніше — лише відображення)
          </label>
        </fieldset>
        <fieldset>
          <legend>Оплатити за допомогою</legend>
          <label>
            <input type="radio" checked={method === 0} onChange={() => setMethod(0)} />
            Картка
          </label>
          <label>
            <input type="radio" checked={method === 1} onChange={() => setMethod(1)} />
            PayPal
          </label>
          {method === 0 && (
            <div className="card-fields">
              <input placeholder="Номер картки" autoComplete="off" />
              <input placeholder="Термін дії" autoComplete="off" />
              <input placeholder="CVV" autoComplete="off" />
              <p className="auth-lead">Поля лише для вигляду. Дані картки нікуди не надсилаються.</p>
            </div>
          )}
        </fieldset>
        {error && <p className="error">{error}</p>}
        <button type="submit" className="auth-primary" disabled={submitting}>
          {submitting ? "Підтверджуємо…" : "Підтвердити (заглушка)"}
        </button>
      </form>
    </section>
  );
}

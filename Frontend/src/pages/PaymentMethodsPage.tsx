import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

type Method = { id: string; brand: string; last4: string };

const STORAGE_KEY = "eh_pay_methods";

function loadMethods(): Method[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Method[]) : [];
  } catch {
    return [];
  }
}

export function PaymentMethodsPage() {
  const navigate = useNavigate();
  const [methods, setMethods] = useState<Method[]>([]);
  const [brand, setBrand] = useState("Visa");
  const [last4, setLast4] = useState("");

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    setMethods(loadMethods());
  }, [navigate]);

  function persist(next: Method[]) {
    setMethods(next);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }

  function onAdd(event: FormEvent) {
    event.preventDefault();
    const digits = last4.replace(/\D/g, "").slice(-4);
    if (digits.length !== 4) {
      return;
    }
    persist([...methods, { id: crypto.randomUUID(), brand, last4: digits }]);
    setLast4("");
  }

  function remove(id: string) {
    persist(methods.filter((item) => item.id !== id));
  }

  return (
    <section className="host-page">
      <h1>Мої способи оплати</h1>
      <p className="muted">Оплата в проєкті — заглушка. Номер картки не зберігається, лише позначка для інтерфейсу.</p>
      <ul className="bookings-list">
        {methods.map((method) => (
          <li key={method.id} className="booking-row">
            <span>
              {method.brand} •••• {method.last4}
            </span>
            <button type="button" className="text-btn" onClick={() => remove(method.id)}>
              Видалити
            </button>
          </li>
        ))}
      </ul>
      {methods.length === 0 && <p className="muted">Ще немає збережених способів.</p>}
      <form className="profile-card" onSubmit={onAdd}>
        <label>
          Тип
          <select value={brand} onChange={(e) => setBrand(e.target.value)}>
            <option>Visa</option>
            <option>Mastercard</option>
            <option>PayPal</option>
          </select>
        </label>
        <label>
          Останні 4 цифри
          <input
            value={last4}
            onChange={(e) => setLast4(e.target.value)}
            inputMode="numeric"
            maxLength={4}
            placeholder="4242"
            required
          />
        </label>
        <button type="submit" className="auth-primary full">
          Додати
        </button>
      </form>
    </section>
  );
}

type IconProps = { name: "Google" | "PayPal" | "Visa" | "Mastercard" | "Card" };

export function BrandIcon({ name }: IconProps) {
  if (name === "Google") {
    return (
      <svg className="brand-icon" viewBox="0 0 18 18" aria-hidden="true">
        <path fill="#4285F4" d="M17.64 9.2c0-.64-.06-1.25-.16-1.84H9v3.48h4.84a4.14 4.14 0 0 1-1.8 2.72v2.26h2.91c1.7-1.57 2.68-3.88 2.68-6.62z" />
        <path fill="#34A853" d="M9 18c2.43 0 4.47-.8 5.96-2.18l-2.91-2.26c-.8.54-1.84.86-3.05.86-2.34 0-4.33-1.58-5.04-3.71H.96v2.33A9 9 0 0 0 9 18z" />
        <path fill="#FBBC05" d="M3.96 10.71A5.41 5.41 0 0 1 3.68 9c0-.59.1-1.17.28-1.71V4.96H.96A9 9 0 0 0 0 9c0 1.45.35 2.83.96 4.04l3-2.33z" />
        <path fill="#EA4335" d="M9 3.58c1.32 0 2.51.45 3.44 1.35l2.58-2.59C13.46.89 11.43 0 9 0A9 9 0 0 0 .96 4.96l3 2.33C4.67 5.16 6.66 3.58 9 3.58z" />
      </svg>
    );
  }
  if (name === "PayPal") {
    return (
      <svg className="brand-icon" viewBox="0 0 24 24" aria-hidden="true">
        <path fill="#003087" d="M8.4 20.5h-2.2a.6.6 0 0 1-.6-.7L7.7 6.2A.8.8 0 0 1 8.5 5.5h5.4c2.4 0 4.1.6 4.9 1.9.7 1.1.7 2.5.1 4.2-.9 2.6-2.9 3.9-5.8 3.9h-1.8a.7.7 0 0 0-.7.6l-.8 4.4z" />
        <path fill="#009cde" d="M19.2 8.2c-.6 2.8-2.7 4.3-5.6 4.3h-1.6a.7.7 0 0 0-.7.6l-.9 5.1h2.1c.3 0 .6-.2.7-.5l.7-3.8a.7.7 0 0 1 .7-.6h1.5c2.8 0 4.8-1.1 5.5-4 .3-1.1.2-2-.2-2.8-.4.6-.9 1.1-1.6 1.7z" />
      </svg>
    );
  }
  if (name === "Visa") {
    return (
      <svg className="brand-icon brand-icon-wide" viewBox="0 0 36 16" aria-hidden="true">
        <rect width="36" height="16" rx="3" fill="#1a1f71" />
        <text x="18" y="12" textAnchor="middle" fill="#fff" fontSize="9" fontFamily="Arial, sans-serif" fontStyle="italic" fontWeight="700">VISA</text>
      </svg>
    );
  }
  if (name === "Mastercard") {
    return (
      <svg className="brand-icon brand-icon-wide" viewBox="0 0 36 16" aria-hidden="true">
        <rect width="36" height="16" rx="3" fill="#fff" stroke="#eadfD2" />
        <circle cx="15" cy="8" r="5" fill="#eb001b" />
        <circle cx="21" cy="8" r="5" fill="#f79e1b" />
        <path d="M18 4.2a5 5 0 0 1 0 7.6 5 5 0 0 1 0-7.6z" fill="#ff5f00" />
      </svg>
    );
  }
  return (
    <svg className="brand-icon" viewBox="0 0 24 24" aria-hidden="true">
      <rect x="2" y="5" width="20" height="14" rx="2" fill="none" stroke="#241f1a" strokeWidth="1.6" />
      <path d="M2 9h20" stroke="#241f1a" strokeWidth="1.6" />
    </svg>
  );
}

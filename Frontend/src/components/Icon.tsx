import type { ReactNode } from "react";

export type IconName =
  | "shield"
  | "plus"
  | "user"
  | "login"
  | "register"
  | "grid"
  | "calendar"
  | "card"
  | "heart"
  | "chat"
  | "logout"
  | "info"
  | "briefcase"
  | "scroll"
  | "mail"
  | "lock"
  | "globe"
  | "pen"
  | "trash";

const shapes: Record<IconName, ReactNode> = {
  shield: <path d="M12 3 5 6v5c0 4.2 2.8 7.4 7 8.5 4.2-1.1 7-4.3 7-8.5V6l-7-3z" />,
  plus: <path d="M12 5v14M5 12h14" />,
  user: <><circle cx="12" cy="8" r="3.2" /><path d="M5.5 19.2c1.1-2.5 3.2-3.7 6.5-3.7s5.4 1.2 6.5 3.7" /></>,
  login: <><path d="M10 7V5.5A1.5 1.5 0 0 1 11.5 4h7A1.5 1.5 0 0 1 20 5.5v13a1.5 1.5 0 0 1-1.5 1.5h-7A1.5 1.5 0 0 1 10 18.5V17" /><path d="M4 12h10M11 9l3 3-3 3" /></>,
  register: <><circle cx="10" cy="8" r="3" /><path d="M4.5 19c1-2.3 2.9-3.5 5.5-3.5 1.2 0 2.3.3 3.2.8" /><path d="M17 11v6M14 14h6" /></>,
  grid: <path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z" />,
  calendar: <><rect x="4" y="5" width="16" height="15" rx="2" /><path d="M8 3v4M16 3v4M4 10h16" /></>,
  card: <><rect x="3" y="6" width="18" height="12" rx="2" /><path d="M3 10h18" /></>,
  heart: <path d="M12 19s-7-4.4-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.6-7 9-7 9z" />,
  chat: <path d="M5 6.5A2.5 2.5 0 0 1 7.5 4h9A2.5 2.5 0 0 1 19 6.5v7A2.5 2.5 0 0 1 16.5 16H10l-4 3.5V6.5z" />,
  logout: <><path d="M9 7V5.5A1.5 1.5 0 0 1 10.5 4h8A1.5 1.5 0 0 1 20 5.5v13a1.5 1.5 0 0 1-1.5 1.5h-8A1.5 1.5 0 0 1 9 18.5V17" /><path d="M13 12H4M7 9l-3 3 3 3" /></>,
  info: <><circle cx="12" cy="12" r="8" /><path d="M12 11v5M12 8h.01" /></>,
  briefcase: <><rect x="3" y="7" width="18" height="13" rx="2" /><path d="M8 7V5.5A1.5 1.5 0 0 1 9.5 4h5A1.5 1.5 0 0 1 16 5.5V7M3 12h18" /></>,
  scroll: <><path d="M7 4h10a2 2 0 0 1 2 2v12l-2-1-2 1-2-1-2 1-2-1-2 1V6a2 2 0 0 1 2-2z" /><path d="M9 8h6M9 12h6" /></>,
  mail: <><rect x="3" y="5" width="18" height="14" rx="2" /><path d="m4 7 8 6 8-6" /></>,
  lock: <><rect x="5" y="10" width="14" height="10" rx="2" /><path d="M8 10V7.5a4 4 0 0 1 8 0V10" /></>,
  globe: <><circle cx="12" cy="12" r="8" /><path d="M4 12h16M12 4c2.2 2.3 3.3 5 3.3 8S14.2 17.7 12 20c-2.2-2.3-3.3-5-3.3-8S9.8 6.3 12 4z" /></>,
  pen: <><path d="M4 20h4L19 9l-4-4L4 16v4z" /><path d="m13 7 4 4" /></>,
  trash: <><path d="M5 7h14" /><path d="M9 7V5h6v2" /><path d="M7 7l1 13h8l1-13" /></>
};

export function Icon({ name }: { name: IconName }) {
  return (
    <svg className="ui-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      {shapes[name]}
    </svg>
  );
}

import { useEffect, useRef, useState } from "react";
import { useLanguage, type Language } from "../i18n";

export function LanguageSwitcher() {
  const { language, setLanguage, t } = useLanguage();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const close = (event: MouseEvent) => { if (ref.current && !ref.current.contains(event.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);
  const choose = (next: Language) => { setLanguage(next); setOpen(false); };
  return <div className="language-switcher" ref={ref}>
    <button type="button" className="language-toggle" aria-label={t("language")} onClick={() => setOpen((v) => !v)}>
      <span aria-hidden>🌐</span><span>{language === "uk" ? "UA" : "EN"}</span><span aria-hidden>⌄</span>
    </button>
    {open && <div className="language-menu">
      <button type="button" className={language === "uk" ? "selected" : ""} onClick={() => choose("uk")}>🇺🇦 {t("ukrainian")}</button>
      <button type="button" className={language === "en" ? "selected" : ""} onClick={() => choose("en")}>🇬🇧 {t("english")}</button>
    </div>}
  </div>;
}

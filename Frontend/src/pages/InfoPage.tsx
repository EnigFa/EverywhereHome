import { Link } from "react-router-dom";
import { useLanguage } from "../i18n";

export function InfoPage({ titleKey }: { titleKey: string }) {
  const { t } = useLanguage();
  return (
    <section className="stub-page">
      <h1>{t(titleKey)}</h1>
      <p>{t("stubText")}</p>
      <p><Link className="back-link" to="/">{t("homeTitle")}</Link></p>
    </section>
  );
}

import { useLayoutEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { setSession } from "../api/session";
import { useLanguage } from "../i18n";
export function AuthCallbackPage(){const {t}=useLanguage();const[params]=useSearchParams();const queryError=params.get("error");useLayoutEffect(()=>{if(queryError)return;const hash=new URLSearchParams(window.location.hash.replace(/^#/,""));const token=params.get("token")??hash.get("token");if(token){setSession(token);window.location.replace("/");return}window.location.replace("/login")},[params,queryError]);if(queryError)return <section className="auth-card"><h1>{t("loginFailed")}</h1><p className="error">{queryError}</p><a href="/login">{t("backToLogin")}</a></section>;return <p>{t("finishingLogin")}</p>}

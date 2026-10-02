import type { ReactNode } from "react";
import type { ChatMessage } from "../api/client";
import { useLanguage } from "../i18n";

export function ChatBubble({ message, mine, children }: { message: ChatMessage; mine: boolean; children?: ReactNode }) {
  const { t } = useLanguage();
  return (
    <div className={`${mine ? "message-bubble mine" : "message-bubble"}${message.isDeleted ? " deleted" : ""}`}>
      <span className="bubble-line">
        <span>{message.isDeleted ? t("messageDeleted") : message.text}</span>
        {children}
      </span>
      {(Boolean(message.editedAtUtc && !message.isDeleted) || Boolean(mine && message.readByOther)) && (
        <span className="bubble-meta">
          {!message.isDeleted && message.editedAtUtc && <small className="message-note">{t("messageEdited")}</small>}
          {mine && message.readByOther && <span className="read-mark" aria-label={t("messageRead")}>✓</span>}
        </span>
      )}
    </div>
  );
}

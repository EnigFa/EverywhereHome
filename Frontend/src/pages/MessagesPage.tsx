import { FormEvent, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api, type ChatMessage, type Conversation } from "../api/client";
import { ChatBubble } from "../components/ChatBubble";
import { Icon } from "../components/Icon";
import { SideMenu } from "../components/SideMenu";
import { currentUserId, getToken } from "../api/session";
import { useLanguage } from "../i18n";

export function MessagesPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [items, setItems] = useState<Conversation[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [text, setText] = useState("");
  const [dialog, setDialog] = useState<null | { kind: "edit"; messageId: string; text: string } | { kind: "delete"; messageId: string } | { kind: "history" }>(null);
  const [error, setError] = useState<string | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);
  const [sideOpen, setSideOpen] = useState(true);
  const me = currentUserId();
  const opened = params.get("chat");
  const listingChat = params.get("listingId");

  function loadInbox() {
    api<Conversation[]>("/api/conversations").then(setItems).catch((e: Error) => setError(e.message));
  }

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    if (listingChat) {
      api<Conversation>(`/api/listings/${listingChat}/conversation`, { method: "POST" })
        .then((chat) => navigate(`/messages?chat=${chat.id}`, { replace: true }))
        .catch((e: Error) => setError(e.message));
      return;
    }
    loadInbox();
    if (opened) setSelected(opened);
    const token = getToken();
    if (token) {
      fetch("/api/admin/summary", { headers: { Authorization: `Bearer ${token}` } }).then((response) => setIsAdmin(response.ok));
    }
  }, [navigate, opened, listingChat]);

  useEffect(() => {
    if (!selected || selected === "new-support") {
      if (selected === "new-support") setMessages([]);
      return;
    }
    api<ChatMessage[]>(`/api/conversations/${selected}/messages`).then((next) => {
      setMessages(next);
      loadInbox();
    }).catch((e: Error) => setError(e.message));
  }, [selected]);

  async function send(event: FormEvent) {
    event.preventDefault();
    if (!text.trim()) return;
    const path = selected === "new-support" ? "/api/support/messages" : `/api/conversations/${selected}/messages`;
    try {
      const created = await api<ChatMessage>(path, { method: "POST", body: JSON.stringify({ text: text.trim() }) });
      setText("");
      if (selected === "new-support") {
        const inbox = await api<Conversation[]>("/api/conversations");
        setItems(inbox);
        const support = inbox.find((item) => item.kind === 1);
        if (support) setSelected(support.id);
      } else {
        setMessages((current) => [...current, created]);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function saveEdit() {
    if (!selected || dialog?.kind !== "edit" || !dialog.text.trim()) return;
    const updated = await api<ChatMessage>(`/api/conversations/${selected}/messages/${dialog.messageId}`, {
      method: "PUT",
      body: JSON.stringify({ text: dialog.text.trim() })
    });
    setMessages((current) => current.map((item) => item.id === dialog.messageId ? updated : item));
    setDialog(null);
  }

  async function removeMessage() {
    if (!selected || dialog?.kind !== "delete") return;
    const updated = await api<ChatMessage>(`/api/conversations/${selected}/messages/${dialog.messageId}`, { method: "DELETE" });
    setMessages((current) => current.map((item) => item.id === dialog.messageId ? updated : item));
    setDialog(null);
  }

  async function clearHistory() {
    if (!selected || dialog?.kind !== "history") return;
    await api(`/api/conversations/${selected}/history`, { method: "DELETE" });
    setMessages([]);
    setDialog(null);
  }

  function title(item: Conversation) {
    if (item.kind === 1) return t("support");
    if (item.kind === 2) return t("report");
    return item.listingTitle || item.userName;
  }

  function kindLabel(item: Conversation) {
    if (item.kind === 1) return t("supportThread");
    if (item.kind === 2) return t("reportThread");
    if (item.kind === 3) return t("directThread");
    return t("listingThread");
  }

  const current = items.find((item) => item.id === selected);

  return (
    <section className="messages-page">
      <h1>{t("messages")}</h1>
      {error && <p className="error">{error}</p>}
      <div className={sideOpen ? "messages-layout" : "messages-layout is-collapsed"}>
        <SideMenu open={sideOpen} onToggle={() => setSideOpen((open) => !open)} title={t("companions")} hideLabel={t("hideMenu")} showLabel={t("showMenu")}>
          {!isAdmin && (
            <button type="button" className="support-start" onClick={() => {
              const support = items.find((item) => item.kind === 1);
              setSelected(support ? support.id : "new-support");
            }}>{t("writeSupport")}</button>
          )}
          {items.length === 0 && !listingChat && <p className="muted">{t("noMessages")}</p>}
          {items.map((item) => (
            <button key={item.id} type="button" className={`conversation-item${selected === item.id ? " selected" : ""}${item.unreadCount ? " unread" : ""}`} onClick={() => setSelected(item.id)}>
              <span><b>{title(item)}</b><small>{kindLabel(item)}</small></span>
              {item.unreadCount > 0 && <b>{item.unreadCount}</b>}
            </button>
          ))}
        </SideMenu>
        <div className="message-panel">
          <div className="chat-head">
            <strong>{current ? title(current) : selected === "new-support" ? t("support") : t("chooseChat")}</strong>
            {current && <span>{kindLabel(current)}</span>}
            {current && <button type="button" className="text-btn" onClick={() => setDialog({ kind: "history" })}>{t("deleteHistory")}</button>}
          </div>
          <div className="message-list">
            {messages.map((message) => {
              const mine = message.senderId === me;
              return (
                <ChatBubble key={message.id} message={message} mine={mine}>
                  {mine && !message.isDeleted && (
                    <span className="message-tools">
                      <button type="button" aria-label={t("editMessage")} onClick={() => setDialog({ kind: "edit", messageId: message.id, text: message.text })}><Icon name="pen" /></button>
                      <button type="button" aria-label={t("deleteMessage")} onClick={() => setDialog({ kind: "delete", messageId: message.id })}><Icon name="trash" /></button>
                    </span>
                  )}
                </ChatBubble>
              );
            })}
          </div>
          {(selected || selected === "new-support") && (
            <form onSubmit={send} className="message-form">
              <textarea value={text} onChange={(e) => setText(e.target.value)} rows={3} placeholder={t("messagePlaceholder")} />
              <button className="auth-primary" type="submit">{t("sendMessage")}</button>
            </form>
          )}
        </div>
      </div>
      {dialog && (
        <div className="modal-backdrop" onClick={() => setDialog(null)}>
          <form className="modal-card" onClick={(event) => event.stopPropagation()} onSubmit={(event) => { event.preventDefault(); if (dialog.kind === "edit") void saveEdit(); else if (dialog.kind === "delete") void removeMessage(); else void clearHistory(); }}>
            <h2>{dialog.kind === "edit" ? t("editMessage") : dialog.kind === "delete" ? t("deleteMessage") : t("deleteHistory")}</h2>
            <p>{dialog.kind === "edit" ? t("confirmEdit") : dialog.kind === "delete" ? t("confirmDeleteMessage") : t("confirmDeleteHistory")}</p>
            {dialog.kind === "edit" && <textarea value={dialog.text} onChange={(e) => setDialog({ ...dialog, text: e.target.value })} rows={4} required />}
            <div className="row-actions">
              <button className="auth-primary" type="submit">{t("confirmAction")}</button>
              <button className="dropdown-toggle" type="button" onClick={() => setDialog(null)}>{t("close")}</button>
            </div>
          </form>
        </div>
      )}
    </section>
  );
}

import { FormEvent, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api } from "../api/client";
import { useLanguage } from "../i18n";

type Message = { id: string; senderId: string; senderName: string; text: string; createdAtUtc: string };
type Conversation = { id: string; kind: number; listingId: string | null; listingTitle: string | null; userId: string; userName: string; hostId: string | null };

export function MessagesPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const listingId = params.get("listingId");
  const adminUserId = params.get("adminUserId");
  const [messages, setMessages] = useState<Message[]>([]);
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<string | null>(null);
  const [mode, setMode] = useState<"support" | "host" | "admin">(adminUserId ? "admin" : "support");

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    void loadSupport();
    void loadHostConversations();
  }, [navigate]);

  useEffect(() => {
    if (adminUserId) {
      void loadAdminSupport(adminUserId);
    } else if (listingId) {
      void loadListingMessages(listingId);
    }
  }, [adminUserId, listingId]);

  async function loadSupport() {
    try {
      setMessages(await api<Message[]>("/api/support/messages"));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function loadHostConversations() {
    try {
      setConversations(await api<Conversation[]>("/api/host/conversations"));
    } catch {
      // A regular guest is not a host; the support inbox remains available.
    }
  }

  async function loadAdminSupport(userId: string) {
    try {
      setMessages(await api<Message[]>(`/api/admin/support/${userId}/messages`));
      setMode("admin");
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function loadListingMessages(id: string) {
    try {
      setMessages(await api<Message[]>(`/api/listings/${id}/messages`));
      setMode("support");
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function selectConversation(id: string) {
    try {
      setSelected(id);
      setMode("host");
      setMessages(await api<Message[]>(`/api/conversations/${id}/messages`));
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function send(event: FormEvent) {
    event.preventDefault();
    const value = text.trim();
    if (!value) return;
    try {
      const endpoint = mode === "admin" && adminUserId
        ? `/api/admin/support/${adminUserId}/messages`
        : mode === "host" && selected
          ? `/api/conversations/${selected}/messages`
          : listingId
            ? `/api/listings/${listingId}/messages`
            : "/api/support/messages";
      const message = await api<Message>(endpoint, { method: "POST", body: JSON.stringify({ text: value }) });
      setMessages((current) => [...current, message]);
      setText("");
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  return (
    <section className="messages-page">
      <div className="section-heading">
        <div><h1>{t("messages")}</h1><p className="muted">{t("messagesLead")}</p></div>
      </div>
      <div className="messages-layout">
        <aside className="profile-card message-sidebar">
          <button type="button" className={mode === "support" && !listingId ? "auth-primary full" : "dropdown-toggle full"} onClick={() => { setMode("support"); setSelected(null); void loadSupport(); }}>{t("support")}</button>
          {conversations.length > 0 && <>
            <h3>{t("hostConversations")}</h3>
            {conversations.map((conversation) => <button key={conversation.id} type="button" className="conversation-item" onClick={() => void selectConversation(conversation.id)}>{conversation.listingTitle ?? conversation.userName}</button>)}
          </>}
        </aside>
        <div className="profile-card message-panel">
          {error && <p className="error">{error}</p>}
          <div className="message-list">
            {messages.length === 0 && !error && <p className="muted">{t("noMessages")}</p>}
            {messages.map((message) => <div className="message-bubble" key={message.id}><strong>{message.senderName}</strong><span>{message.text}</span><small>{new Date(message.createdAtUtc).toLocaleString()}</small></div>)}
          </div>
          <form onSubmit={send} className="message-form">
            <textarea value={text} onChange={(event) => setText(event.target.value)} placeholder={t("messagePlaceholder")} rows={3} />
            <button type="submit" className="auth-primary">{t("sendMessage")}</button>
          </form>
        </div>
      </div>
    </section>
  );
}

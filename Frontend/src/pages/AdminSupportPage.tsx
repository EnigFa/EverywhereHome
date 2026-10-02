import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type ChatMessage } from "../api/client";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { SideMenu } from "../components/SideMenu";
import { ChatBubble } from "../components/ChatBubble";
import { UserLink } from "../components/UserLink";
import { currentUserId, getToken } from "../api/session";
import { useLanguage } from "../i18n";

type Ticket = {
  id: string;
  userId: string;
  userName: string;
  status: number;
  assigneeId: string | null;
  assigneeName: string | null;
  resolvedByName: string | null;
  createdAtUtc: string;
  resolvedAtUtc: string | null;
  decision: string | null;
};

export function AdminSupportPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [status, setStatus] = useState(0);
  const [items, setItems] = useState<Ticket[]>([]);
  const [openId, setOpenId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [resolveOpen, setResolveOpen] = useState(false);
  const [decision, setDecision] = useState("");
  const [chat, setChat] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [sideOpen, setSideOpen] = useState(true);
  const [askPath, setAskPath] = useState<null | { text: string; path: string }>(null);
  const me = currentUserId();

  function load(next = status) {
    api<Ticket[]>(`/api/admin/support?status=${next}`).then(setItems).catch((e: Error) => setError(e.message));
  }

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    load(status);
  }, [navigate, status]);

  useEffect(() => {
    if (!openId) return;
    api<ChatMessage[]>(`/api/conversations/${openId}/messages`).then(setMessages).catch(() => setMessages([]));
  }, [openId]);

  async function run(path: string, body?: unknown) {
    setError(null);
    try {
      await api(path, { method: "POST", body: JSON.stringify(body ?? {}) });
      setOpenId(null);
      setDecision("");
      load(status);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function sendChat(event: FormEvent) {
    event.preventDefault();
    if (!openId || !chat.trim()) return;
    const created = await api<ChatMessage>(`/api/conversations/${openId}/messages`, { method: "POST", body: JSON.stringify({ text: chat.trim() }) });
    setMessages((current) => [...current, created]);
    setChat("");
  }

  return (
    <section className="admin-section">
      <Link className="back-link" to="/admin">{t("backToAdmin")}</Link>
      <div className={sideOpen ? "admin-split" : "admin-split is-collapsed"}>
        <SideMenu open={sideOpen} onToggle={() => setSideOpen((open) => !open)} title={t("sections")} hideLabel={t("hideMenu")} showLabel={t("showMenu")}>
          <div className="tab-row side-tabs">
            <button type="button" className={status === 0 ? "auth-primary" : "dropdown-toggle"} onClick={() => setStatus(0)}>{t("queue")}</button>
            <button type="button" className={status === 2 ? "auth-primary" : "dropdown-toggle"} onClick={() => setStatus(2)}>{t("inProgress")}</button>
            <button type="button" className={status === 1 ? "auth-primary" : "dropdown-toggle"} onClick={() => setStatus(1)}>{t("archive")}</button>
          </div>
        </SideMenu>
        <div className="admin-work">
          <h1>{t("support")}</h1>
          {error && <p className="error">{error}</p>}
          {items.length === 0 && <p className="muted">{t("none")}</p>}
          <ul className="bookings-list">
        {items.map((ticket) => (
          <li key={ticket.id} className="case-card">
            <button type="button" className="case-toggle" onClick={() => setOpenId(openId === ticket.id ? null : ticket.id)}>
              <strong><UserLink id={ticket.userId} name={ticket.userName} /></strong>
              <span>{new Date(ticket.createdAtUtc).toLocaleString()}</span>
            </button>
            {openId === ticket.id && (
              <div className="case-body">
                <p>{t("received")}: {new Date(ticket.createdAtUtc).toLocaleString()}</p>
                <p>{t("fromUser")}: <UserLink id={ticket.userId} name={ticket.userName} /></p>
                {ticket.status === 1 && <><p>{t("resolvedAt")}: {ticket.resolvedAtUtc ? new Date(ticket.resolvedAtUtc).toLocaleString() : "—"}</p><p>{t("resolvedBy")}: {ticket.resolvedByName}</p><p>{t("decision")}: {ticket.decision}</p></>}
                {ticket.status === 0 && <button type="button" className="auth-primary" onClick={() => setAskPath({ text: t("confirmTake"), path: `/api/admin/support/${ticket.id}/take` })}>{t("take")}</button>}
                {ticket.status !== 0 && (
                  <div className="case-chat">
                    <div className="message-list">
                      {messages.map((message) => <ChatBubble key={message.id} message={message} mine={message.senderId === me} />)}
                    </div>
                    {ticket.status === 2 && (
                      <form onSubmit={sendChat} className="message-form">
                        <textarea value={chat} onChange={(e) => setChat(e.target.value)} rows={2} />
                        <button className="auth-primary" type="submit">{t("sendMessage")}</button>
                      </form>
                    )}
                  </div>
                )}
                {ticket.status === 2 && (
                  <div className="row-actions">
                    <button type="button" className="auth-primary" onClick={() => { setDecision(""); setResolveOpen(true); }}>{t("resolvedButton")}</button>
                    <button type="button" className="text-btn" onClick={() => setAskPath({ text: t("confirmRelease"), path: `/api/admin/support/${ticket.id}/release` })}>{t("release")}</button>
                  </div>
                )}
                {resolveOpen && openId === ticket.id && (
                  <div className="modal-backdrop" onClick={() => setResolveOpen(false)}>
                    <form className="modal-card" onClick={(event) => event.stopPropagation()} onSubmit={(event) => { event.preventDefault(); run(`/api/admin/support/${ticket.id}/resolve`, { decision }); setResolveOpen(false); }}>
                      <h2>{t("resolvedButton")}</h2>
                      <textarea value={decision} onChange={(e) => setDecision(e.target.value)} rows={4} required placeholder={t("decision")} />
                      <div className="row-actions">
                        <button className="auth-primary" type="submit">{t("confirmResolved")}</button>
                        <button className="dropdown-toggle" type="button" onClick={() => setResolveOpen(false)}>{t("close")}</button>
                      </div>
                    </form>
                  </div>
                )}
              </div>
            )}
          </li>
        ))}
      </ul>
        </div>
      </div>
      {askPath && (
        <ConfirmDialog
          title={t("confirmAction")}
          text={askPath.text}
          confirmLabel={t("confirmAction")}
          closeLabel={t("close")}
          onClose={() => setAskPath(null)}
          onConfirm={() => {
            const path = askPath.path;
            setAskPath(null);
            void run(path);
          }}
        />
      )}
    </section>
  );
}

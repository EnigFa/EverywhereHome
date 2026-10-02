import { FormEvent, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, type ChatMessage } from "../api/client";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { SideMenu } from "../components/SideMenu";
import { ChatBubble } from "../components/ChatBubble";
import { UserLink } from "../components/UserLink";
import { currentUserId, getToken } from "../api/session";
import { useLanguage } from "../i18n";

type Report = {
  id: string;
  target: number;
  status: number;
  text: string;
  listingId: string | null;
  listingTitle: string | null;
  reportedUserId: string | null;
  reportedUserName: string | null;
  reporterId: string;
  reporterName: string;
  createdAtUtc: string;
  assigneeId: string | null;
  assigneeName: string | null;
  resolvedById: string | null;
  resolvedByName: string | null;
  resolvedAtUtc: string | null;
  decision: string | null;
  conversationId: string | null;
};

const tabs = [
  { status: 0, key: "queue" },
  { status: 2, key: "inProgress" },
  { status: 1, key: "archive" }
] as const;

export function AdminReportsPage() {
  const { t } = useLanguage();
  const navigate = useNavigate();
  const [status, setStatus] = useState(0);
  const [items, setItems] = useState<Report[]>([]);
  const [openId, setOpenId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [resolveOpen, setResolveOpen] = useState(false);
  const [decision, setDecision] = useState("");
  const [unpublish, setUnpublish] = useState(false);
  const [block, setBlock] = useState(false);
  const [chat, setChat] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [sideOpen, setSideOpen] = useState(true);
  const [askPath, setAskPath] = useState<null | { text: string; path: string }>(null);
  const me = currentUserId();

  function load(next = status) {
    api<Report[]>(`/api/admin/reports?status=${next}`).then(setItems).catch((e: Error) => setError(e.message));
  }

  useEffect(() => {
    if (!getToken()) {
      navigate("/login");
      return;
    }
    load(status);
  }, [navigate, status]);

  useEffect(() => {
    const item = items.find((report) => report.id === openId);
    if (!item?.conversationId) {
      setMessages([]);
      return;
    }
    api<ChatMessage[]>(`/api/conversations/${item.conversationId}/messages`).then(setMessages).catch(() => setMessages([]));
  }, [openId, items]);

  async function run(path: string, body?: unknown) {
    setError(null);
    try {
      await api(path, { method: "POST", body: body ? JSON.stringify(body) : "{}" });
      setOpenId(null);
      setDecision("");
      load(status);
    } catch (e) {
      setError(e instanceof Error ? e.message : t("error"));
    }
  }

  async function sendChat(event: FormEvent, conversationId: string) {
    event.preventDefault();
    if (!chat.trim()) return;
    const created = await api<ChatMessage>(`/api/conversations/${conversationId}/messages`, {
      method: "POST",
      body: JSON.stringify({ text: chat.trim() })
    });
    setMessages((current) => [...current, created]);
    setChat("");
  }

  return (
    <section className="admin-section">
      <Link className="back-link" to="/admin">{t("backToAdmin")}</Link>
      <div className={sideOpen ? "admin-split" : "admin-split is-collapsed"}>
        <SideMenu open={sideOpen} onToggle={() => setSideOpen((open) => !open)} title={t("sections")} hideLabel={t("hideMenu")} showLabel={t("showMenu")}>
          <div className="tab-row side-tabs">
            {tabs.map((tab) => (
              <button key={tab.status} type="button" className={status === tab.status ? "auth-primary" : "dropdown-toggle"} onClick={() => { setStatus(tab.status); setOpenId(null); }}>{t(tab.key)}</button>
            ))}
          </div>
        </SideMenu>
        <div className="admin-work">
          <h1>{t("reports")}</h1>
          {error && <p className="error">{error}</p>}
          {items.length === 0 && <p className="muted">{t("none")}</p>}
          <ul className="bookings-list">
        {items.map((report) => (
          <li key={report.id} className="case-card">
            <button type="button" className="case-toggle" onClick={() => setOpenId(openId === report.id ? null : report.id)}>
              <strong>{report.listingTitle || report.reportedUserName || t("report")}</strong>
              <span>{new Date(report.createdAtUtc).toLocaleString()} · {report.reporterName}</span>
            </button>
            {openId === report.id && (
              <div className="case-body">
                <p>{t("received")}: {new Date(report.createdAtUtc).toLocaleString()}</p>
                <p>{t("fromUser")}: <UserLink id={report.reporterId} name={report.reporterName} /></p>
                <p>{t("aboutUser")}: {report.listingId ? <Link className="user-link" to={`/listings/${report.listingId}`}>{report.listingTitle}</Link> : <UserLink id={report.reportedUserId} name={report.reportedUserName} />}</p>
                <p>{report.text}</p>
                {report.status === 1 && (
                  <>
                    <p>{t("resolvedAt")}: {report.resolvedAtUtc ? new Date(report.resolvedAtUtc).toLocaleString() : "—"}</p>
                    <p>{t("resolvedBy")}: <UserLink id={report.resolvedById} name={report.resolvedByName} /></p>
                    <p>{t("decision")}: {report.decision}</p>
                  </>
                )}
                {report.status === 0 && <button type="button" className="auth-primary" onClick={() => setAskPath({ text: t("confirmTake"), path: `/api/admin/reports/${report.id}/take` })}>{t("take")}</button>}
                {report.status === 2 && (
                  <>
                    <p>{t("assignee")}: <UserLink id={report.assigneeId} name={report.assigneeName} /></p>
                    {report.conversationId && (
                      <div className="case-chat">
                        <div className="message-list">
                          {messages.map((message) => (
                            <ChatBubble key={message.id} message={message} mine={message.senderId === me} />
                          ))}
                        </div>
                        <form onSubmit={(event) => sendChat(event, report.conversationId!)} className="message-form">
                          <textarea value={chat} onChange={(e) => setChat(e.target.value)} rows={2} />
                          <button className="auth-primary" type="submit">{t("sendMessage")}</button>
                        </form>
                      </div>
                    )}
                    <div className="row-actions">
                      <button type="button" className="auth-primary" onClick={() => { setDecision(""); setResolveOpen(true); }}>{t("resolvedButton")}</button>
                      <button type="button" className="text-btn" onClick={() => setAskPath({ text: t("confirmRelease"), path: `/api/admin/reports/${report.id}/release` })}>{t("release")}</button>
                    </div>
                    {resolveOpen && (
                      <div className="modal-backdrop" onClick={() => setResolveOpen(false)}>
                        <form className="modal-card" onClick={(event) => event.stopPropagation()} onSubmit={(event) => { event.preventDefault(); run(`/api/admin/reports/${report.id}/resolve`, { decision, unpublish, block }); setResolveOpen(false); }}>
                          <h2>{t("resolvedButton")}</h2>
                          <textarea value={decision} onChange={(e) => setDecision(e.target.value)} rows={4} required placeholder={t("decision")} />
                          {report.listingId && <label><input type="checkbox" checked={unpublish} onChange={(e) => setUnpublish(e.target.checked)} /> {t("unpublish")}</label>}
                          {report.reportedUserId && <label><input type="checkbox" checked={block} onChange={(e) => setBlock(e.target.checked)} /> {t("block")}</label>}
                          <div className="row-actions">
                            <button className="auth-primary" type="submit">{t("confirmResolved")}</button>
                            <button className="dropdown-toggle" type="button" onClick={() => setResolveOpen(false)}>{t("close")}</button>
                          </div>
                        </form>
                      </div>
                    )}
                  </>
                )}
                {report.status === 1 && report.conversationId && (
                  <div className="message-list">
                    {messages.map((message) => <ChatBubble key={message.id} message={message} mine={message.senderId === me} />)}
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

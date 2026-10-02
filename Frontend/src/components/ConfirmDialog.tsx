import type { ReactNode } from "react";

type Props = {
  title: string;
  text?: string;
  confirmLabel: string;
  closeLabel: string;
  busy?: boolean;
  onConfirm: () => void;
  onClose: () => void;
  children?: ReactNode;
};

export function ConfirmDialog({ title, text, confirmLabel, closeLabel, busy, onConfirm, onClose, children }: Props) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" role="dialog" aria-modal="true" onClick={(event) => event.stopPropagation()}>
        <h2>{title}</h2>
        {text && <p>{text}</p>}
        {children}
        <div className="row-actions">
          <button type="button" className="auth-primary" disabled={busy} onClick={onConfirm}>{confirmLabel}</button>
          <button type="button" className="dropdown-toggle" onClick={onClose}>{closeLabel}</button>
        </div>
      </div>
    </div>
  );
}

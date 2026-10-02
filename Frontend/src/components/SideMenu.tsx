import type { ReactNode } from "react";

type SideMenuProps = {
  open: boolean;
  onToggle: () => void;
  title: string;
  hideLabel: string;
  showLabel: string;
  children: ReactNode;
};

export function SideMenu({ open, onToggle, title, hideLabel, showLabel, children }: SideMenuProps) {
  return (
    <aside className={open ? "side-menu" : "side-menu is-collapsed"}>
      <button type="button" className="side-menu-toggle" onClick={onToggle} aria-expanded={open} aria-label={open ? hideLabel : showLabel}>
        {open ? hideLabel : ">>"}
      </button>
      {open && (
        <div className="side-menu-body">
          <h2>{title}</h2>
          {children}
        </div>
      )}
    </aside>
  );
}

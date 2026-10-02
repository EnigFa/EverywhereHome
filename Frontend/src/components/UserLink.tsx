import { Link } from "react-router-dom";

export function UserLink({ id, name }: { id?: string | null; name?: string | null }) {
  if (!id) {
    return <>{name || "—"}</>;
  }
  return <Link className="user-link" to={`/users/${id}`}>{name || id}</Link>;
}

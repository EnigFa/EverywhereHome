import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, type Profile } from "../api/client";

const empty: Profile = {
  email: "",
  displayName: "",
  avatarUrl: null,
  isHost: false,
  phoneVerified: false,
  phoneNumber: null,
  school: null,
  profession: null,
  languages: null,
  hometown: null,
  birthDecade: null,
  passion: null,
  uselessSkills: null,
  timeSink: null,
  favoriteSong: null,
  funFact: null,
  biographyTitle: null,
  pets: null,
  intro: null
};

export function ProfilePage() {
  const navigate = useNavigate();
  const [profile, setProfile] = useState<Profile>(empty);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!localStorage.getItem("eh_token")) {
      navigate("/login");
      return;
    }
    api<Profile>("/api/profile")
      .then(setProfile)
      .catch((e: Error) => setError(e.message));
  }, [navigate]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setSaved(false);
    try {
      const result = await api<Profile>("/api/profile", {
        method: "PUT",
        body: JSON.stringify({
          displayName: profile.displayName,
          school: profile.school,
          profession: profile.profession,
          languages: profile.languages,
          hometown: profile.hometown,
          birthDecade: profile.birthDecade,
          passion: profile.passion,
          uselessSkills: profile.uselessSkills,
          timeSink: profile.timeSink,
          favoriteSong: profile.favoriteSong,
          funFact: profile.funFact,
          biographyTitle: profile.biographyTitle,
          pets: profile.pets,
          intro: profile.intro
        })
      });
      setProfile(result);
      setSaved(true);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка");
    }
  }

  const initial = (profile.displayName || profile.email || "?").slice(0, 1).toUpperCase();

  return (
    <section className="profile-page">
      <div className="profile-hero">
        <div className="avatar" aria-hidden>
          {initial}
        </div>
        <div>
          <h1>Ваш профіль</h1>
          <p className="muted">{profile.email}</p>
        </div>
      </div>
      <form className="profile-card" onSubmit={onSubmit}>
        <label>
          Імʼя
          <input
            value={profile.displayName}
            onChange={(e) => setProfile({ ...profile, displayName: e.target.value })}
            required
          />
        </label>
        <label>
          Місто
          <input
            value={profile.hometown ?? ""}
            onChange={(e) => setProfile({ ...profile, hometown: e.target.value })}
            placeholder="Київ"
          />
        </label>
        <label>
          Професія
          <input
            value={profile.profession ?? ""}
            onChange={(e) => setProfile({ ...profile, profession: e.target.value })}
          />
        </label>
        <label>
          Мови
          <input
            value={profile.languages ?? ""}
            onChange={(e) => setProfile({ ...profile, languages: e.target.value })}
            placeholder="Українська, англійська"
          />
        </label>
        <label className="full">
          Про себе
          <textarea
            rows={4}
            value={profile.intro ?? ""}
            onChange={(e) => setProfile({ ...profile, intro: e.target.value })}
            placeholder="Коротко розкажіть гостям і господарям, хто ви."
          />
        </label>
        {error && <p className="error full">{error}</p>}
        {saved && <p className="muted full">Зміни збережено.</p>}
        <button type="submit" className="auth-primary full">
          Зберегти
        </button>
      </form>
    </section>
  );
}

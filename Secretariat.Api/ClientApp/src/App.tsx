import { useEffect, useRef, useState } from 'react';
import { useIsMutating, useQuery, useQueryClient } from '@tanstack/react-query';
import { NavLink, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { ArrowRightLeft, CheckCircle2, FileText, FolderClosed, Inbox, Menu, Users, X } from 'lucide-react';
import { api } from './api';
import { roles, type AppUser } from './types';
import { SessionContext } from './session';
import { ErrorState, Loading } from './components/ui';
import { DocumentList } from './features/DocumentList';
import { DocumentDetail } from './features/DocumentDetail';
import { UserList } from './features/UserList';

const navigation = [
  { path: 'correspondence', label: 'Korespondencja', Icon: Inbox },
  { path: 'internal-correspondence', label: 'Obieg wewnętrzny', Icon: ArrowRightLeft },
  { path: 'contracts', label: 'Umowy', Icon: FileText },
  { path: 'users', label: 'Użytkownicy', Icon: Users },
];
function savedUser() {
  try {
    return Number(localStorage.getItem('secretariat.user.v1')) || 0;
  } catch {
    return 0;
  }
}

export function App() {
  const users = useQuery({
    queryKey: ['users'],
    queryFn: ({ signal }) => api<AppUser[]>('appusers', undefined, { signal }),
  });
  const [selectedId, setSelectedId] = useState(savedUser);
  const [menu, setMenu] = useState(false);
  const [toast, setToast] = useState('');
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const client = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const mutating = useIsMutating() > 0;
  const user =
    users.data?.find((u) => u.id === selectedId) || users.data?.find((u) => u.role === 1) || users.data?.[0];
  const active = navigation.find((item) => location.pathname.startsWith('/' + item.path)) || navigation[0];
  useEffect(() => {
    document.title = `${active.label} · Sekretariat`;
    setMenu(false);
  }, [active.label, location.pathname]);
  useEffect(() => () => clearTimeout(timer.current), []);
  function notify(message: string) {
    clearTimeout(timer.current);
    setToast(message);
    timer.current = setTimeout(() => setToast(''), 4500);
  }
  function changeUser(id: number) {
    void client.cancelQueries();
    client.removeQueries({ predicate: (query) => query.queryKey[0] !== 'users' });
    setSelectedId(id);
    setToast('');
    try {
      localStorage.setItem('secretariat.user.v1', String(id));
    } catch {
      /* Selection still works when browser storage is unavailable. */
    }
    navigate('/' + active.path);
  }
  return (
    <div className="app-shell">
      <a
        className="skip-link"
        href="#main"
        onClick={(event) => {
          event.preventDefault();
          document.getElementById('main')?.focus();
        }}
      >
        Przejdź do treści
      </a>
      {menu && (
        <button className="sidebar-backdrop" aria-label="Zamknij menu" onClick={() => setMenu(false)} />
      )}
      <aside className={`sidebar ${menu ? 'is-open' : ''}`}>
        <NavLink to="/correspondence" className="brand">
          <span className="brand-mark">
            <FolderClosed size={24} />
          </span>
          Sekretariat
        </NavLink>
        <nav aria-label="Nawigacja główna">
          {navigation.map(({ path, label, Icon }) => (
            <NavLink
              key={path}
              to={'/' + path}
              className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
            >
              <Icon size={20} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="profile">
          <div className="avatar">
            {user?.displayName
              .split(' ')
              .filter(Boolean)
              .slice(0, 2)
              .map((n) => n[0])
              .join('') || 'S'}
          </div>
          <div className="profile-copy">
            <label htmlFor="current-user">Użytkownik</label>
            <select
              id="current-user"
              aria-label="Aktualny użytkownik"
              value={user?.id || ''}
              disabled={mutating || !users.data?.length}
              onChange={(event) => changeUser(Number(event.target.value))}
            >
              {users.data?.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.displayName}
                  {users.data!.filter((other) => other.displayName === u.displayName).length > 1
                    ? ` · #${u.id}`
                    : ''}
                </option>
              ))}
            </select>
            <span>{user ? roles[user.role] || 'Nieprzypisana rola' : '—'}</span>
          </div>
        </div>
      </aside>
      <div className="workspace">
        <header className="topbar">
          <div className="breadcrumb">
            <button
              className="icon-button mobile-menu"
              aria-label="Otwórz menu"
              title="Menu"
              onClick={() => setMenu(!menu)}
            >
              <Menu size={21} />
            </button>
            <span>Dokumenty</span>
            <span className="breadcrumb-slash">/</span>
            <span>{active.label}</span>
          </div>
          <time>
            {new Intl.DateTimeFormat('pl-PL', { day: 'numeric', month: 'long', year: 'numeric' }).format(
              new Date(),
            )}
          </time>
        </header>
        <main id="main" tabIndex={-1}>
          {users.isPending ? (
            <Loading />
          ) : users.isError ? (
            <ErrorState error={users.error} retry={() => void users.refetch()} />
          ) : !user ? (
            <div className="state">
              <h1>Brak użytkowników</h1>
              <p>Nie ma jeszcze konta, które może otworzyć dokumenty.</p>
            </div>
          ) : (
            <SessionContext.Provider value={{ user, users: users.data!, notify }}>
              <div key={user.id}>
                <Routes>
                  <Route path="/" element={<Navigate to="/correspondence" replace />} />
                  <Route path="/correspondence" element={<DocumentList module="correspondence" />} />
                  <Route
                    path="/internal-correspondence"
                    element={<DocumentList module="internal-correspondence" />}
                  />
                  <Route path="/contracts" element={<DocumentList module="contracts" />} />
                  <Route path="/:module/:id" element={<DocumentDetail />} />
                  <Route path="/users" element={<UserList />} />
                  <Route
                    path="*"
                    element={
                      <div className="state">
                        <h1>Nie znaleziono strony</h1>
                        <NavLink to="/correspondence" className="button secondary">
                          Wróć do korespondencji
                        </NavLink>
                      </div>
                    }
                  />
                </Routes>
              </div>
            </SessionContext.Provider>
          )}
        </main>
      </div>
      <div className={`toast ${toast ? 'visible' : ''}`} role="status" aria-live="polite">
        {toast && (
          <>
            <CheckCircle2 size={19} />
            <span>{toast}</span>
            <button aria-label="Zamknij powiadomienie" onClick={() => setToast('')}>
              <X size={16} />
            </button>
          </>
        )}
      </div>
    </div>
  );
}

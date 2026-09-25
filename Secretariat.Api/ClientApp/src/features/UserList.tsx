import { useState, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Pencil, Plus, Search } from 'lucide-react';
import { api } from '../api';
import { useSession } from '../session';
import { roles, type AppUser } from '../types';
import { Empty, FormError, Modal, SaveButton } from '../components/ui';

export function UserList() {
  const { user, users, notify } = useSession();
  const [search, setSearch] = useState('');
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<AppUser | null>(null);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [role, setRole] = useState('1');
  const client = useQueryClient();
  const isAdministrator = user.role === 4;
  const mutation = useMutation({
    mutationFn: () =>
      api(editing ? `appusers/${editing.id}` : 'appusers', user.id, {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify({
          displayName: name.trim(),
          email: email.trim(),
          role: Number(role),
          entraObjectId: editing?.entraObjectId ?? null,
        }),
      }),
    onSuccess: async () => {
      await client.cancelQueries({ predicate: (query) => query.queryKey[0] !== 'users' });
      client.removeQueries({ predicate: (query) => query.queryKey[0] !== 'users' });
      await client.invalidateQueries({ queryKey: ['users'] });
      setCreating(false);
      setEditing(null);
      setName('');
      setEmail('');
      setRole('1');
      notify(editing ? 'Zapisano zmiany użytkownika.' : 'Dodano użytkownika.');
    },
  });
  const filtered = users.filter((u) =>
    `${u.displayName} ${u.email}`.toLocaleLowerCase('pl').includes(search.toLocaleLowerCase('pl')),
  );
  function submit(event: FormEvent) {
    event.preventDefault();
    if (isAdministrator && name.trim() && email.trim() && role) mutation.mutate();
  }
  function openEditor(account: AppUser | null) {
    mutation.reset();
    setEditing(account);
    setCreating(!account);
    setName(account?.displayName || '');
    setEmail(account?.email || '');
    setRole(account ? (account.role >= 1 && account.role <= 4 ? String(account.role) : '') : '1');
  }
  function closeEditor() {
    setCreating(false);
    setEditing(null);
  }
  return (
    <>
      <div className="page-heading">
        <h1>Użytkownicy</h1>
        {isAdministrator && (
          <button className="button primary" onClick={() => openEditor(null)}>
            <Plus size={19} />
            Nowy użytkownik
          </button>
        )}
      </div>
      <div className="section-rule" />
      <div className="toolbar">
        <label className="search-field">
          <Search size={19} />
          <input
            type="search"
            aria-label="Szukaj użytkowników"
            placeholder="Szukaj osoby lub adresu e-mail"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </label>
      </div>
      {filtered.length ? (
        <div className="table-scroll">
          <table className="users-table">
            <thead>
              <tr>
                <th>Osoba</th>
                <th>E-mail</th>
                <th>Rola</th>
                {isAdministrator && (
                  <th>
                    <span className="sr-only">Działania</span>
                  </th>
                )}
              </tr>
            </thead>
            <tbody>
              {filtered.map((u) => (
                <tr key={u.id}>
                  <td>
                    <div className="person">
                      <span className="avatar small" aria-hidden="true">
                        {u.displayName
                          .split(' ')
                          .filter(Boolean)
                          .slice(0, 2)
                          .map((n) => n[0])
                          .join('')}
                      </span>
                      <strong>{u.displayName}</strong>
                    </div>
                  </td>
                  <td>
                    <a href={`mailto:${u.email}`}>{u.email}</a>
                  </td>
                  <td>
                    <span className={`badge ${u.role >= 3 ? 'green' : u.role === 2 ? 'blue' : 'neutral'}`}>
                      {roles[u.role] || 'Nieprzypisana rola'}
                    </span>
                  </td>
                  {isAdministrator && (
                    <td>
                      <button
                        className="icon-button"
                        title={`Edytuj użytkownika: ${u.displayName}`}
                        aria-label={`Edytuj użytkownika: ${u.displayName}`}
                        onClick={() => openEditor(u)}
                      >
                        <Pencil size={17} />
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <Empty title="Brak pasujących użytkowników" />
      )}
      {isAdministrator && (creating || editing) && (
        <Modal
          title={editing ? 'Edycja użytkownika' : 'Nowy użytkownik'}
          onClose={closeEditor}
          busy={mutation.isPending}
        >
          <form onSubmit={submit}>
            <fieldset className="form-fields" disabled={mutation.isPending}>
              <label>
                Imię i nazwisko
                <input
                  autoFocus
                  required
                  maxLength={200}
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </label>
              <label>
                E-mail
                <input
                  type="email"
                  required
                  maxLength={254}
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </label>
              <label>
                Rola
                <select
                  required
                  value={role}
                  disabled={editing?.id === user.id}
                  title={editing?.id === user.id ? 'Twoją rolę może zmienić inny administrator' : undefined}
                  onChange={(e) => setRole(e.target.value)}
                >
                  <option value="" disabled>
                    Wybierz rolę
                  </option>
                  {Object.entries(roles)
                    .filter(([id]) => id !== '0')
                    .map(([id, label]) => (
                      <option key={id} value={id}>
                        {label}
                      </option>
                    ))}
                </select>
              </label>
              <FormError error={mutation.error} />
            </fieldset>
            <footer className="modal-footer">
              <button
                className="button secondary"
                type="button"
                disabled={mutation.isPending}
                onClick={closeEditor}
              >
                Anuluj
              </button>
              <SaveButton
                pending={mutation.isPending}
                label={editing ? 'Zapisz zmiany' : 'Dodaj użytkownika'}
              />
            </footer>
          </form>
        </Modal>
      )}
    </>
  );
}

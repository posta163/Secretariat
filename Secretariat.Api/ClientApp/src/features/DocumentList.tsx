import { useDeferredValue, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  ArrowDownToLine,
  ArrowUpFromLine,
  ChevronLeft,
  ChevronRight,
  Plus,
  RefreshCw,
  Search,
} from 'lucide-react';
import { api } from '../api';
import { useSession } from '../session';
import { date, modules, privileged, statuses, userName, type Document, type Module } from '../types';
import { Badge, Empty, ErrorState, Loading } from '../components/ui';
import { DocumentForm } from './DocumentForm';

const PAGE_SIZE = 8;
export function DocumentList({ module }: { module: Module }) {
  return <List key={module} module={module} />;
}
function List({ module }: { module: Module }) {
  const { user, users } = useSession();
  const [search, setSearch] = useState('');
  const deferredSearch = useDeferredValue(search);
  const [type, setType] = useState(0);
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const query = useQuery({
    queryKey: [user.id, module],
    queryFn: ({ signal }) => api<Document[]>(module, user.id, { signal }),
  });
  const correspondence = module === 'correspondence';
  const canCreate = !correspondence || privileged(user);
  const normalize = (text: string) =>
    text
      .toLocaleLowerCase('pl')
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/ł/g, 'l');
  const rows = (query.data || [])
    .filter(
      (doc) =>
        (!type || doc.type === type) &&
        (!status || (correspondence ? String(doc.isRead) === status : String(doc.status) === status)) &&
        normalize(
          [doc.number, doc.subject, doc.sender, doc.contractor, userName(users, doc.recipientUserId)].join(
            ' ',
          ),
        ).includes(normalize(deferredSearch)),
    )
    .sort((a, b) => b.id - a.id);
  const pages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const currentPage = Math.min(page, pages);
  const visible = rows.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);
  return (
    <>
      <div className="page-heading">
        <h1>{modules[module].title}</h1>
        {canCreate && (
          <button className="button primary" onClick={() => setCreating(true)}>
            <Plus size={19} />
            {modules[module].create}
          </button>
        )}
      </div>
      {correspondence ? (
        <div className="tabs" aria-label="Typ korespondencji">
          {['Wszystkie', 'Przychodząca', 'Wychodząca'].map((label, index) => (
            <button
              key={label}
              aria-pressed={type === index}
              className={type === index ? 'selected' : ''}
              onClick={() => {
                setType(index);
                setPage(1);
              }}
            >
              {label}
            </button>
          ))}
        </div>
      ) : (
        <div className="section-rule" />
      )}
      <div className="toolbar">
        <label className="search-field">
          <Search size={19} />
          <input
            aria-label="Szukaj dokumentów"
            type="search"
            placeholder={
              correspondence
                ? 'Szukaj numeru, tematu lub nadawcy'
                : module === 'contracts'
                  ? 'Szukaj numeru, tematu lub kontrahenta'
                  : 'Szukaj numeru lub tematu'
            }
            value={search}
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </label>
        <select
          aria-label="Filtr statusu"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value);
            setPage(1);
          }}
        >
          <option value="">Wszystkie statusy</option>
          {correspondence ? (
            <>
              <option value="false">Nieprzeczytane</option>
              <option value="true">Przeczytane</option>
            </>
          ) : (
            Object.entries(statuses).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))
          )}
        </select>
        <button
          className="icon-button refresh"
          title="Odśwież listę"
          aria-label="Odśwież listę"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
        >
          <RefreshCw size={19} className={query.isFetching ? 'spin' : ''} />
        </button>
      </div>
      {query.isPending ? (
        <Loading />
      ) : query.isError ? (
        <ErrorState error={query.error} retry={() => void query.refetch()} />
      ) : rows.length === 0 ? (
        <Empty
          title={search || status || type ? 'Brak pasujących dokumentów' : 'Brak dokumentów'}
          action={
            search || status || type ? (
              <button
                className="button secondary"
                onClick={() => {
                  setSearch('');
                  setStatus('');
                  setType(0);
                }}
              >
                Wyczyść filtry
              </button>
            ) : undefined
          }
        />
      ) : (
        <>
          <div className="table-scroll">
            <table className="document-table">
              <thead>
                <tr>
                  <th>Numer</th>
                  <th>
                    {correspondence
                      ? 'Temat i nadawca'
                      : module === 'contracts'
                        ? 'Przedmiot i kontrahent'
                        : 'Temat'}
                  </th>
                  <th>
                    {correspondence ? 'Adresat' : module === 'contracts' ? 'Osoba odpowiedzialna' : 'Autor'}
                  </th>
                  <th>
                    {correspondence
                      ? 'Data wpływu'
                      : module === 'contracts'
                        ? 'Data umowy'
                        : 'Data utworzenia'}
                  </th>
                  <th>Status</th>
                  <th>
                    <span className="sr-only">Szczegóły</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {visible.map((doc) => (
                  <tr key={doc.id}>
                    <td>
                      <div className="number-cell">
                        {correspondence &&
                          (doc.type === 2 ? (
                            <ArrowUpFromLine className="outgoing" size={20} />
                          ) : (
                            <ArrowDownToLine className="incoming" size={20} />
                          ))}
                        <Link to={`/${module}/${doc.id}`} className="document-link">
                          {doc.number || `#${doc.id}`}
                        </Link>
                      </div>
                    </td>
                    <td>
                      <Link to={`/${module}/${doc.id}`} className="subject-link">
                        {doc.subject || 'Bez tematu'}
                      </Link>
                      {(doc.sender || doc.contractor) && (
                        <span className="cell-secondary">{doc.sender || doc.contractor}</span>
                      )}
                    </td>
                    <td>
                      {userName(
                        users,
                        correspondence
                          ? doc.recipientUserId
                          : module === 'contracts'
                            ? doc.responsibleUserId
                            : doc.createdByUserId,
                      )}
                    </td>
                    <td className="date-cell">
                      {date(doc.receivedDate || doc.contractDate || doc.createdAt)}
                    </td>
                    <td>
                      <Badge {...(correspondence ? { read: doc.isRead ?? false } : { status: doc.status })} />
                    </td>
                    <td>
                      <Link
                        className="icon-button row-link"
                        to={`/${module}/${doc.id}`}
                        title="Szczegóły dokumentu"
                        aria-label={`Szczegóły ${doc.number}`}
                      >
                        <ChevronRight size={19} />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="pagination">
            <span>
              {(currentPage - 1) * PAGE_SIZE + 1}–{Math.min(currentPage * PAGE_SIZE, rows.length)} z{' '}
              {rows.length}
            </span>
            <div>
              <button
                className="icon-button"
                title="Poprzednia strona"
                aria-label="Poprzednia strona"
                disabled={currentPage === 1}
                onClick={() => setPage(currentPage - 1)}
              >
                <ChevronLeft size={18} />
              </button>
              <span className="page-number" aria-label={`Strona ${currentPage} z ${pages}`}>
                {currentPage} / {pages}
              </span>
              <button
                className="icon-button"
                title="Następna strona"
                aria-label="Następna strona"
                disabled={currentPage === pages}
                onClick={() => setPage(currentPage + 1)}
              >
                <ChevronRight size={18} />
              </button>
            </div>
          </div>
        </>
      )}
      {creating && <DocumentForm module={module} onClose={() => setCreating(false)} />}
    </>
  );
}

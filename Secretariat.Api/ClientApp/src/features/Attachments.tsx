import { useRef, useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, File, LoaderCircle, Paperclip, Upload } from 'lucide-react';
import { api, download } from '../api';
import { useSession } from '../session';
import { dateTime, privileged, userName, type Attachment, type Document } from '../types';
import { ErrorState, FormError, Loading } from '../components/ui';

export function Attachments({
  module,
  document: doc,
}: {
  module: 'contracts' | 'correspondence';
  document: Document;
}) {
  const { user, users, notify } = useSession();
  const client = useQueryClient();
  const path = `${module}/${doc.id}/attachments`;
  const queryKey = [user.id, module, doc.id, 'attachments'];
  const query = useQuery({ queryKey, queryFn: ({ signal }) => api<Attachment[]>(path, user.id, { signal }) });
  const input = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<globalThis.File | null>(null);
  const [type, setType] = useState('1');
  const [error, setError] = useState<unknown>(null);
  const [downloading, setDownloading] = useState<number | null>(null);
  const contract = module === 'contracts';
  const approval = doc.approvers?.find((a) => a.approverUserId === user.id);
  const original = contract
    ? doc.status === 1 &&
      (privileged(user) || doc.createdByUserId === user.id || doc.responsibleUserId === user.id)
    : privileged(user);
  const signed =
    contract && user.role === 2 && approval?.status === 1 && (doc.status === 1 || doc.status === 2);
  const attachmentType = original && signed ? type : signed ? '2' : '1';
  const upload = useMutation({
    mutationFn: (body: FormData) => api(path, user.id, { method: 'POST', body }),
    onSuccess: async () => {
      setFile(null);
      if (input.current) input.current.value = '';
      await client.invalidateQueries({ queryKey });
      notify('Dodano załącznik.');
    },
  });
  function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    upload.reset();
    if (!file || file.size === 0) {
      setError('Wybierz niepusty plik.');
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      setError('Maksymalny rozmiar pliku wynosi 10 MB.');
      return;
    }
    if (contract && !(attachmentType === '2' ? /\.(pdf|p7m)$/i : /\.(pdf|docx)$/i).test(file.name)) {
      setError('Niedozwolony format pliku.');
      return;
    }
    const body = new FormData();
    body.append('file', file);
    if (contract) body.append('type', attachmentType);
    upload.mutate(body);
  }
  async function getFile(attachment: Attachment) {
    setError(null);
    setDownloading(attachment.id);
    try {
      await download(`${path}/${attachment.id}/download`, user.id, attachment.originalFileName);
    } catch (reason) {
      setError(reason);
    } finally {
      setDownloading(null);
    }
  }
  return (
    <section className="detail-section">
      <h2>
        <Paperclip size={19} />
        Załączniki <span className="section-count">{query.data?.length ?? '—'}</span>
      </h2>
      {query.isPending ? (
        <Loading />
      ) : query.isError ? (
        <ErrorState error={query.error} retry={() => void query.refetch()} />
      ) : query.data.length ? (
        <ul className="attachment-list">
          {query.data.map((a) => (
            <li key={a.id}>
              <span className="file-symbol">
                <File size={23} />
              </span>
              <div className="file-info">
                <strong>{a.originalFileName}</strong>
                <span>
                  {a.fileSize < 1024 * 1024
                    ? `${Math.max(1, Math.round(a.fileSize / 1024))} KB`
                    : `${(a.fileSize / 1024 / 1024).toFixed(1)} MB`}{' '}
                  · {dateTime(a.uploadedAt)}
                  {a.uploadedByUserId ? ` · ${userName(users, a.uploadedByUserId)}` : ''}
                </span>
              </div>
              {contract && (
                <span className={`badge ${a.type === 2 ? 'green' : 'neutral'}`}>
                  {a.type === 2 ? 'Podpisany' : 'Oryginał'}
                </span>
              )}
              <button
                className="icon-button"
                disabled={downloading !== null}
                onClick={() => void getFile(a)}
                title={`Pobierz ${a.originalFileName}`}
                aria-label={`Pobierz ${a.originalFileName}`}
              >
                {downloading === a.id ? <LoaderCircle className="spin" size={18} /> : <Download size={18} />}
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <p className="muted empty-files">Brak załączników.</p>
      )}
      {(original || signed) && (
        <form className="upload-form" onSubmit={submit}>
          <fieldset disabled={upload.isPending}>
            <label className="file-input-label">
              <span>
                Plik{' '}
                <small>
                  maks. 10 MB{contract ? (attachmentType === '2' ? ' · PDF, P7M' : ' · PDF, DOCX') : ''}
                </small>
              </span>
              <input
                ref={input}
                type="file"
                aria-label="Plik załącznika"
                required
                accept={contract ? (attachmentType === '2' ? '.pdf,.p7m' : '.pdf,.docx') : undefined}
                onChange={(e) => {
                  setFile(e.target.files?.[0] || null);
                  setError(null);
                  upload.reset();
                }}
              />
            </label>
            {contract && (
              <label>
                Rodzaj
                <select
                  aria-label="Rodzaj załącznika"
                  value={attachmentType}
                  onChange={(e) => setType(e.target.value)}
                >
                  {original && <option value="1">Oryginał</option>}
                  {signed && <option value="2">Podpisany</option>}
                </select>
              </label>
            )}
            <button className="button secondary" type="submit" disabled={!file || upload.isPending}>
              <Upload size={17} />
              {upload.isPending ? 'Przesyłanie…' : 'Dodaj załącznik'}
            </button>
          </fieldset>
        </form>
      )}
      <FormError error={error || upload.error} />
    </section>
  );
}

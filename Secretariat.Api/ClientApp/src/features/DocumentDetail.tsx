import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Check, CheckCheck, Circle, Clock3, FileText, Pencil, X } from 'lucide-react';
import { api } from '../api';
import { useSession } from '../session';
import {
  approvalStatuses,
  date,
  dateTime,
  modules,
  privileged,
  userName,
  type Document,
  type Module,
} from '../types';
import { Badge, ErrorState, FormError, Loading, Modal } from '../components/ui';
import { DocumentForm } from './DocumentForm';
import { Attachments } from './Attachments';

export function DocumentDetail() {
  const { module, id } = useParams();
  if (!module || !(module in modules) || !id || !/^\d+$/.test(id))
    return <ErrorState error={new Error('Nie znaleziono dokumentu.')} />;
  return <Detail key={`${module}/${id}`} module={module as Module} id={Number(id)} />;
}
function Detail({ module, id }: { module: Module; id: number }) {
  const { user, users, notify } = useSession();
  const client = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null);
  const query = useQuery({
    queryKey: [user.id, module, id],
    queryFn: ({ signal }) => api<Document>(`${module}/${id}`, user.id, { signal }),
  });
  const change = useMutation({
    mutationFn: (action: string) => api(`${module}/${id}/${action}`, user.id, { method: 'POST' }),
    onSuccess: async (_, action) => {
      await client.invalidateQueries({ queryKey: [user.id, module] });
      setDecision(null);
      notify(
        action === 'read'
          ? 'Oznaczono jako przeczytane.'
          : action === 'approve'
            ? 'Zapisano akceptację.'
            : 'Dokument został odrzucony.',
      );
    },
  });
  const back = (
    <Link to={'/' + module} className="back-link">
      <ArrowLeft size={17} />
      {modules[module].title}
    </Link>
  );
  if (query.isPending)
    return (
      <>
        {back}
        <Loading />
      </>
    );
  if (query.isError)
    return (
      <>
        {back}
        <ErrorState error={query.error} retry={() => void query.refetch()} />
      </>
    );
  const doc = query.data;
  const correspondence = module === 'correspondence';
  const contract = module === 'contracts';
  const canEdit = correspondence
    ? privileged(user)
    : contract && doc.status === 1 && (privileged(user) || doc.createdByUserId === user.id);
  const approval = doc.approvers?.find((a) => a.approverUserId === user.id);
  const canDecide = user.role === 2 && approval?.status === 1 && (doc.status === 1 || doc.status === 2);
  const info = correspondence
    ? [
        ['Nadawca', doc.sender],
        ['Adresat', userName(users, doc.recipientUserId)],
        ['Data wpływu', date(doc.receivedDate)],
        ['Typ', doc.type === 1 ? 'Przychodząca' : doc.type === 2 ? 'Wychodząca' : 'Nieokreślony'],
        ['Zarejestrowano', dateTime(doc.createdDate)],
        ['Przeczytano', dateTime(doc.readAt)],
      ]
    : contract
      ? [
          ['Kontrahent', doc.contractor],
          ['Osoba odpowiedzialna', userName(users, doc.responsibleUserId)],
          ['Data zawarcia', date(doc.contractDate)],
          ['Autor', userName(users, doc.createdByUserId)],
          ['Obowiązuje od', date(doc.validFrom)],
          ['Obowiązuje do', date(doc.validTo)],
        ]
      : [
          ['Autor', userName(users, doc.createdByUserId)],
          ['Data utworzenia', dateTime(doc.createdAt)],
        ];
  return (
    <>
      {back}
      <div className="detail-heading">
        <div className="document-identity">
          <span className="document-symbol">
            <FileText size={25} />
          </span>
          <div>
            <p className="document-number">{doc.number}</p>
            <h1>{doc.subject || 'Bez tematu'}</h1>
          </div>
        </div>
        <Badge {...(correspondence ? { read: doc.isRead ?? false } : { status: doc.status })} />
      </div>
      <div className="detail-actions">
        {canEdit && (
          <button className="button secondary" disabled={change.isPending} onClick={() => setEditing(true)}>
            <Pencil size={16} />
            Edytuj
          </button>
        )}
        {correspondence && !doc.isRead && (
          <button
            className="button secondary"
            disabled={change.isPending}
            onClick={() => change.mutate('read')}
          >
            <CheckCheck size={17} />
            Oznacz jako przeczytane
          </button>
        )}
        {canDecide && (
          <>
            <button
              className="button primary"
              disabled={change.isPending}
              onClick={() => {
                change.reset();
                setDecision('approve');
              }}
            >
              <Check size={17} />
              Zatwierdź
            </button>
            <button
              className="button danger-outline"
              disabled={change.isPending}
              onClick={() => {
                change.reset();
                setDecision('reject');
              }}
            >
              <X size={17} />
              Odrzuć
            </button>
          </>
        )}
      </div>
      {!decision && <FormError error={change.error} />}
      <section className="detail-section">
        <h2>Dane dokumentu</h2>
        <dl className="metadata">
          {info.map(([label, value]) => (
            <div key={label}>
              <dt>{label}</dt>
              <dd>{value || '—'}</dd>
            </div>
          ))}
        </dl>
        {doc.relatedIncomingCorrespondence && (
          <div className="related-document">
            <span>Odpowiedź na</span>
            <Link to={`/correspondence/${doc.relatedIncomingCorrespondence.id}`}>
              {doc.relatedIncomingCorrespondence.number}
            </Link>
          </div>
        )}
        {doc.description && (
          <div className="document-text">
            <h3>Opis</h3>
            <p>{doc.description}</p>
          </div>
        )}
        {doc.contractualPenalties && (
          <div className="document-text">
            <h3>Kary umowne</h3>
            <p>{doc.contractualPenalties}</p>
          </div>
        )}
        {doc.comment && (
          <div className="document-text">
            <h3>Komentarz</h3>
            <p>{doc.comment}</p>
          </div>
        )}
      </section>
      {!correspondence && (
        <section className="detail-section">
          <h2>
            Akceptacja{' '}
            <span className="section-count">
              {doc.approvers?.filter((a) => a.status === 2).length || 0} / {doc.approvers?.length || 0}
            </span>
          </h2>
          {doc.approvers?.length ? (
            <div className="approval-list">
              {doc.approvers.map((a) => (
                <div className="approval-row" key={a.approverUserId}>
                  <span className={`approval-icon status-${a.status}`}>
                    {a.status === 2 ? (
                      <Check size={19} />
                    ) : a.status === 3 ? (
                      <X size={19} />
                    ) : (
                      <Clock3 size={19} />
                    )}
                  </span>
                  <div className="approval-name">
                    <strong>{userName(users, a.approverUserId)}</strong>
                    <span>{contract ? (a.role === 1 ? 'Kierownik' : 'Dyrektor') : 'Akceptujący'}</span>
                  </div>
                  <div className="approval-result">
                    <strong>{approvalStatuses[a.status]}</strong>
                    <span>{a.reviewedAt ? dateTime(a.reviewedAt) : '—'}</span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="muted">
              <Circle size={15} /> Brak przypisanych akceptujących.
            </p>
          )}
        </section>
      )}
      {module !== 'internal-correspondence' && <Attachments module={module} document={doc} />}
      {editing && <DocumentForm module={module} document={doc} onClose={() => setEditing(false)} />}
      {decision && (
        <Modal
          title={decision === 'approve' ? 'Zatwierdzić dokument?' : 'Odrzucić dokument?'}
          busy={change.isPending}
          onClose={() => setDecision(null)}
        >
          <div className="confirmation-body">
            <p>
              <strong>{doc.number}</strong>
            </p>
            <p>{doc.subject}</p>
            <p className="muted">
              {decision === 'approve'
                ? 'Decyzja zostanie zapisana na Twoim koncie i nie będzie można jej cofnąć.'
                : 'Odrzucenie zakończy proces akceptacji tego dokumentu.'}
            </p>
            <FormError error={change.error} />
          </div>
          <footer className="modal-footer">
            <button
              className="button secondary"
              disabled={change.isPending}
              onClick={() => setDecision(null)}
            >
              Anuluj
            </button>
            <button
              className={`button ${decision === 'approve' ? 'primary' : 'danger'}`}
              disabled={change.isPending}
              onClick={() => change.mutate(decision)}
            >
              {decision === 'approve' ? <Check size={17} /> : <X size={17} />}
              {change.isPending
                ? 'Zapisywanie…'
                : decision === 'approve'
                  ? 'Potwierdź akceptację'
                  : 'Potwierdź odrzucenie'}
            </button>
          </footer>
        </Modal>
      )}
    </>
  );
}

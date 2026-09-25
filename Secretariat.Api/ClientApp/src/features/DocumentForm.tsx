import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { api } from '../api';
import { useSession } from '../session';
import { inputDate, modules, today, type Document, type Module } from '../types';
import { FormError, Modal, SaveButton } from '../components/ui';

export function DocumentForm({
  module,
  document,
  onClose,
}: {
  module: Module;
  document?: Document;
  onClose: () => void;
}) {
  const { user, users, notify } = useSession();
  const client = useQueryClient();
  const navigate = useNavigate();
  const correspondence = module === 'correspondence';
  const contract = module === 'contracts';
  const [fields, setFields] = useState({
    subject: document?.subject || '',
    sender: document?.sender || '',
    type: String(document?.type || 1),
    recipientUserId: String(document?.recipientUserId || ''),
    receivedDate: inputDate(document?.receivedDate) || today(),
    relatedIncomingCorrespondenceId: String(document?.relatedIncomingCorrespondenceId || ''),
    description: document?.description || '',
    contractor: document?.contractor || '',
    contractDate: inputDate(document?.contractDate) || today(),
    validFrom: inputDate(document?.validFrom),
    validTo: inputDate(document?.validTo),
    contractualPenalties: document?.contractualPenalties || '',
    comment: document?.comment || '',
    responsibleUserId: String(document?.responsibleUserId || user.id),
    managerUserId: '',
    directorUserId: '',
  });
  const [approvers, setApprovers] = useState<number[]>([]);
  const [validation, setValidation] = useState('');
  const incoming = useQuery({
    queryKey: [user.id, 'correspondence'],
    queryFn: ({ signal }) => api<Document[]>('correspondence', user.id, { signal }),
    enabled: correspondence && !document && fields.type === '2',
  });
  const candidates = users.filter((u) => u.role === 2);
  const set = (name: keyof typeof fields, value: string) =>
    setFields((previous) => ({ ...previous, [name]: value }));
  const save = useMutation({
    mutationFn: (body: object) =>
      api<Document>(`${module}${document ? '/' + document.id : ''}`, user.id, {
        method: document ? 'PUT' : 'POST',
        body: JSON.stringify(body),
      }),
    onSuccess: async (result) => {
      await client.invalidateQueries({ queryKey: [user.id, module] });
      notify(document ? 'Zapisano zmiany.' : 'Dokument został utworzony.');
      onClose();
      if (!document) navigate(`/${module}/${result.id}`);
    },
  });
  function submit(event: FormEvent) {
    event.preventDefault();
    setValidation('');
    if (!fields.subject.trim()) {
      setValidation('Temat jest wymagany.');
      return;
    }
    const asDate = (value: string) => (value ? value + 'T00:00:00' : null);
    let body: object;
    if (correspondence) {
      if (!fields.sender.trim()) {
        setValidation('Nadawca jest wymagany.');
        return;
      }
      body = {
        subject: fields.subject.trim(),
        sender: fields.sender.trim(),
        type: Number(fields.type),
        receivedDate: asDate(fields.receivedDate),
        recipientUserId: fields.recipientUserId ? Number(fields.recipientUserId) : null,
        relatedIncomingCorrespondenceId:
          fields.type === '2' && fields.relatedIncomingCorrespondenceId
            ? Number(fields.relatedIncomingCorrespondenceId)
            : null,
        ...(document ? { number: document.number } : {}),
      };
    } else if (contract) {
      if (!fields.contractor.trim()) {
        setValidation('Kontrahent jest wymagany.');
        return;
      }
      if (fields.validFrom && fields.validTo && fields.validFrom > fields.validTo) {
        setValidation('Koniec obowiązywania nie może poprzedzać początku.');
        return;
      }
      if (!document && fields.managerUserId === fields.directorUserId) {
        setValidation('Kierownik i dyrektor muszą być różnymi osobami.');
        return;
      }
      body = {
        subject: fields.subject.trim(),
        contractor: fields.contractor.trim(),
        contractDate: asDate(fields.contractDate),
        validFrom: asDate(fields.validFrom),
        validTo: asDate(fields.validTo),
        contractualPenalties: fields.contractualPenalties.trim() || null,
        comment: fields.comment.trim() || null,
        responsibleUserId: Number(fields.responsibleUserId),
        ...(!document
          ? { managerUserId: Number(fields.managerUserId), directorUserId: Number(fields.directorUserId) }
          : {}),
      };
    } else {
      if (!fields.description.trim() || !approvers.length) {
        setValidation('Uzupełnij opis i wybierz przynajmniej jednego akceptującego.');
        return;
      }
      body = {
        subject: fields.subject.trim(),
        description: fields.description.trim(),
        approverUserIds: approvers,
      };
    }
    save.mutate(body);
  }
  return (
    <Modal
      title={document ? `Edytuj: ${document.number}` : modules[module].create}
      onClose={onClose}
      busy={save.isPending}
    >
      <form onSubmit={submit}>
        <fieldset disabled={save.isPending} className="form-fields">
          {correspondence && !document && (
            <label>
              Typ korespondencji
              <select value={fields.type} onChange={(e) => set('type', e.target.value)}>
                <option value="1">Przychodząca</option>
                <option value="2">Wychodząca</option>
              </select>
            </label>
          )}
          <label>
            {contract ? 'Przedmiot umowy' : 'Temat'}
            <input
              required
              autoFocus
              maxLength={contract ? 300 : 200}
              value={fields.subject}
              onChange={(e) => set('subject', e.target.value)}
            />
          </label>
          {correspondence && (
            <>
              <label>
                Nadawca
                <input required value={fields.sender} onChange={(e) => set('sender', e.target.value)} />
              </label>
              <div className="form-grid">
                <label>
                  Data wpływu
                  <input
                    required
                    type="date"
                    value={fields.receivedDate}
                    onChange={(e) => set('receivedDate', e.target.value)}
                  />
                </label>
                {!document && (
                  <label>
                    Adresat
                    <select
                      value={fields.recipientUserId}
                      onChange={(e) => set('recipientUserId', e.target.value)}
                    >
                      <option value="">Nie przypisano</option>
                      {users.map((u) => (
                        <option key={u.id} value={u.id}>
                          {u.displayName}
                        </option>
                      ))}
                    </select>
                  </label>
                )}
              </div>
              {!document && fields.type === '2' && (
                <label>
                  Odpowiedź na pismo
                  <select
                    disabled={incoming.isPending || incoming.isError}
                    value={fields.relatedIncomingCorrespondenceId}
                    onChange={(e) => set('relatedIncomingCorrespondenceId', e.target.value)}
                  >
                    <option value="">Bez powiązania</option>
                    {incoming.data
                      ?.filter((d) => d.type === 1)
                      .map((d) => (
                        <option key={d.id} value={d.id}>
                          {d.number} · {d.subject}
                        </option>
                      ))}
                  </select>
                  {incoming.isError && (
                    <span className="field-error">Nie udało się pobrać pism przychodzących.</span>
                  )}
                </label>
              )}
            </>
          )}
          {contract && (
            <>
              <label>
                Kontrahent
                <input
                  required
                  maxLength={200}
                  value={fields.contractor}
                  onChange={(e) => set('contractor', e.target.value)}
                />
              </label>
              <div className="form-grid">
                <label>
                  Data zawarcia
                  <input
                    required
                    type="date"
                    value={fields.contractDate}
                    onChange={(e) => set('contractDate', e.target.value)}
                  />
                </label>
                <label>
                  Osoba odpowiedzialna
                  <select
                    required
                    value={fields.responsibleUserId}
                    onChange={(e) => set('responsibleUserId', e.target.value)}
                  >
                    <option value="">Wybierz osobę</option>
                    {users.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.displayName}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
              <div className="form-grid">
                <label>
                  Obowiązuje od
                  <input
                    type="date"
                    value={fields.validFrom}
                    onChange={(e) => set('validFrom', e.target.value)}
                  />
                </label>
                <label>
                  Obowiązuje do
                  <input
                    type="date"
                    min={fields.validFrom}
                    value={fields.validTo}
                    onChange={(e) => set('validTo', e.target.value)}
                  />
                </label>
              </div>
              {!document && (
                <div className="form-grid">
                  <label>
                    Kierownik
                    <select
                      required
                      value={fields.managerUserId}
                      onChange={(e) => set('managerUserId', e.target.value)}
                    >
                      <option value="">Wybierz kierownika</option>
                      {candidates.map((u) => (
                        <option key={u.id} value={u.id}>
                          {u.displayName}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Dyrektor
                    <select
                      required
                      value={fields.directorUserId}
                      onChange={(e) => set('directorUserId', e.target.value)}
                    >
                      <option value="">Wybierz dyrektora</option>
                      {candidates.map((u) => (
                        <option key={u.id} value={u.id}>
                          {u.displayName}
                        </option>
                      ))}
                    </select>
                  </label>
                </div>
              )}
              <label>
                Kary umowne
                <textarea
                  rows={2}
                  value={fields.contractualPenalties}
                  onChange={(e) => set('contractualPenalties', e.target.value)}
                />
              </label>
              <label>
                Komentarz
                <textarea rows={2} value={fields.comment} onChange={(e) => set('comment', e.target.value)} />
              </label>
            </>
          )}
          {!contract && !correspondence && (
            <>
              <label>
                Opis
                <textarea
                  required
                  rows={5}
                  value={fields.description}
                  onChange={(e) => set('description', e.target.value)}
                />
              </label>
              <fieldset className="approver-picker">
                <legend>Akceptujący</legend>
                {candidates.length ? (
                  candidates.map((u) => (
                    <label className="checkbox-label" key={u.id}>
                      <input
                        type="checkbox"
                        checked={approvers.includes(u.id)}
                        onChange={(e) =>
                          setApprovers((current) =>
                            e.target.checked ? [...current, u.id] : current.filter((id) => id !== u.id),
                          )
                        }
                      />
                      <span>
                        {u.displayName}
                        <small>{u.email}</small>
                      </span>
                    </label>
                  ))
                ) : (
                  <p className="muted">Brak użytkowników z rolą akceptującego.</p>
                )}
              </fieldset>
            </>
          )}
          <FormError error={validation || save.error} />
        </fieldset>
        <footer className="modal-footer">
          <button className="button secondary" type="button" disabled={save.isPending} onClick={onClose}>
            Anuluj
          </button>
          <SaveButton pending={save.isPending} label={document ? 'Zapisz zmiany' : 'Utwórz dokument'} />
        </footer>
      </form>
    </Modal>
  );
}

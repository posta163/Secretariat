import { useEffect, useRef, type ReactNode } from 'react';
import { AlertCircle, Check, FileSearch, LoaderCircle, RefreshCw, X } from 'lucide-react';
import { statuses } from '../types';

export function Loading() {
  return (
    <div className="state" role="status">
      <LoaderCircle className="spin" size={28} />
      <p>Ładowanie danych…</p>
    </div>
  );
}
export function ErrorState({ error, retry }: { error: unknown; retry?: () => void }) {
  return (
    <div className="state error" role="alert">
      <AlertCircle size={28} />
      <p>{error instanceof Error ? error.message : 'Nie udało się pobrać danych.'}</p>
      {retry && (
        <button className="button secondary" onClick={retry}>
          <RefreshCw size={16} />
          Spróbuj ponownie
        </button>
      )}
    </div>
  );
}
export function Empty({ title = 'Brak dokumentów', action }: { title?: string; action?: ReactNode }) {
  return (
    <div className="state">
      <FileSearch size={36} strokeWidth={1.3} />
      <h2>{title}</h2>
      {action}
    </div>
  );
}
export function Badge({ status, read }: { status?: number; read?: boolean }) {
  const label =
    read !== undefined ? (read ? 'Przeczytane' : 'Nieprzeczytane') : statuses[status || 0] || 'Nieokreślony';
  const color =
    read !== undefined
      ? read
        ? 'neutral'
        : 'blue'
      : { 1: 'neutral', 2: 'amber', 3: 'green', 4: 'red' }[status || 0] || 'neutral';
  return (
    <span className={`badge ${color}`}>
      <span className="status-dot" />
      {label}
    </span>
  );
}
export function Modal({
  title,
  children,
  onClose,
  busy = false,
}: {
  title: string;
  children: ReactNode;
  onClose: () => void;
  busy?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const dialog = ref.current!;
    dialog.showModal();
    return () => dialog.close();
  }, []);
  return (
    <dialog
      ref={ref}
      className="modal"
      aria-label={title}
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) onClose();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget && !busy) onClose();
      }}
    >
      <div className="modal-inner">
        <header className="modal-header">
          <h2>{title}</h2>
          <button
            className="icon-button"
            type="button"
            title="Zamknij"
            aria-label="Zamknij"
            onClick={onClose}
            disabled={busy}
          >
            <X size={20} />
          </button>
        </header>
        {children}
      </div>
    </dialog>
  );
}
export function FormError({ error }: { error: unknown }) {
  return error ? (
    <div className="inline-error" role="alert">
      <AlertCircle size={18} />
      <span>{error instanceof Error ? error.message : String(error)}</span>
    </div>
  ) : null;
}
export function SaveButton({ pending, label = 'Zapisz' }: { pending: boolean; label?: string }) {
  return (
    <button className="button primary" type="submit" disabled={pending}>
      {pending ? <LoaderCircle className="spin" size={17} /> : <Check size={17} />}
      {pending ? 'Zapisywanie…' : label}
    </button>
  );
}

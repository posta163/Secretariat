export interface AppUser {
  id: number;
  displayName: string;
  email: string;
  role: number;
  entraObjectId?: string | null;
}
export type Module = 'correspondence' | 'internal-correspondence' | 'contracts';
export interface Approval {
  approverUserId: number;
  approverName?: string;
  role?: number;
  status: number;
  reviewedAt?: string | null;
}
export interface Document {
  id: number;
  number: string;
  subject: string;
  type?: number;
  sender?: string;
  receivedDate?: string;
  createdDate?: string;
  createdAt?: string;
  isRead?: boolean;
  readAt?: string | null;
  recipientUserId?: number | null;
  recipientUser?: AppUser;
  relatedIncomingCorrespondenceId?: number | null;
  relatedIncomingCorrespondence?: Document | null;
  description?: string;
  status?: number;
  createdByUserId?: number;
  createdByUser?: AppUser;
  createdByName?: string;
  approvers?: Approval[];
  contractor?: string;
  contractDate?: string;
  validFrom?: string | null;
  validTo?: string | null;
  contractualPenalties?: string | null;
  comment?: string | null;
  responsibleUserId?: number;
  responsibleUserName?: string;
}
export interface Attachment {
  id: number;
  originalFileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  type?: number;
  uploadedByUserId?: number;
}
export const modules: Record<Module, { title: string; create: string; singular: string }> = {
  correspondence: { title: 'Korespondencja', create: 'Nowe pismo', singular: 'Pismo' },
  'internal-correspondence': { title: 'Obieg wewnętrzny', create: 'Nowy dokument', singular: 'Dokument' },
  contracts: { title: 'Umowy', create: 'Nowa umowa', singular: 'Umowa' },
};
export const roles: Record<number, string> = {
  0: 'Nieprzypisana rola',
  1: 'Pracownik',
  2: 'Akceptujący',
  3: 'Sekretariat',
  4: 'Administrator',
};
export const statuses: Record<number, string> = {
  1: 'Nowy',
  2: 'W akceptacji',
  3: 'Zaakceptowany',
  4: 'Odrzucony',
};
export const approvalStatuses: Record<number, string> = { 1: 'Oczekuje', 2: 'Zaakceptowano', 3: 'Odrzucono' };
export const privileged = (user?: AppUser) => user?.role === 3 || user?.role === 4;
export function date(value?: string | null) {
  if (!value) return '—';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '—' : new Intl.DateTimeFormat('pl-PL').format(parsed);
}
export function dateTime(value?: string | null) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('pl-PL', { dateStyle: 'medium', timeStyle: 'short' }).format(
    new Date(value.endsWith('Z') ? value : value + 'Z'),
  );
}
export const inputDate = (value?: string | null) => value?.slice(0, 10) || '';
export const today = () => new Date().toLocaleDateString('sv-SE');
export const userName = (users: AppUser[], id?: number | null) =>
  users.find((u) => u.id === id)?.displayName || 'Nie przypisano';

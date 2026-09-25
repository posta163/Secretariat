import { createContext, useContext } from 'react';
import type { AppUser } from './types';
export const SessionContext = createContext<{
  user: AppUser;
  users: AppUser[];
  notify: (message: string) => void;
} | null>(null);
export function useSession() {
  const session = useContext(SessionContext);
  if (!session) throw new Error('Brak kontekstu użytkownika.');
  return session;
}

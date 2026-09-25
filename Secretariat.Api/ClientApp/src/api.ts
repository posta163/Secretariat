export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message);
  }
}
async function checked(response: Response) {
  if (response.ok) return response;
  let message = 'Nie udało się wykonać operacji.';
  const body = await response.text();
  try {
    const data = JSON.parse(body);
    message = data.errors
      ? Object.values(data.errors).flat().join(' ')
      : data.detail || data.title || message;
  } catch {
    if (body && !body.includes('<') && body.length < 1500) message = body;
  }
  if (response.status === 401) message = 'Wybierz użytkownika, aby kontynuować.';
  if (response.status >= 500) message = 'Serwer nie może teraz wykonać operacji. Spróbuj ponownie.';
  throw new ApiError(message, response.status);
}
export async function api<T>(path: string, userId?: number, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers);
  if (userId) headers.set('X-User-Id', String(userId));
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json');
  let response: Response;
  try {
    response = await fetch(`/api/${path}`, { ...options, headers });
  } catch (error) {
    if ((error as Error).name === 'AbortError') throw error;
    throw new Error('Brak połączenia z serwerem. Spróbuj ponownie.');
  }
  await checked(response);
  return response.status === 204 ? (undefined as T) : response.json();
}
export async function download(path: string, userId: number, filename: string) {
  const response = await checked(await fetch(`/api/${path}`, { headers: { 'X-User-Id': String(userId) } }));
  const url = URL.createObjectURL(await response.blob());
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

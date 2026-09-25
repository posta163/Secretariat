import { test, expect, type Page } from '@playwright/test';
import type { AppUser } from '../src/types';

const users: AppUser[] = [
  {
    id: 1,
    displayName: 'Adam Pracownik',
    email: 'adam@example.test',
    role: 1,
    entraObjectId: 'existing-entra-id',
  },
  { id: 2, displayName: 'Piotr Kierownik', email: 'piotr@example.test', role: 2 },
  { id: 3, displayName: 'Anna Sekretariat', email: 'anna@example.test', role: 3 },
  { id: 4, displayName: 'Maria Dyrektor', email: 'maria@example.test', role: 2 },
  { id: 5, displayName: 'Inny Pracownik', email: 'inny@example.test', role: 1 },
  { id: 6, displayName: 'Administrator', email: 'admin@example.test', role: 4 },
  { id: 7, displayName: 'Starsze Konto', email: 'legacy@example.test', role: 0 },
];
async function setup(page: Page, actor = 1) {
  const state = {
    users: structuredClone(users),
    documents: Array.from({ length: 12 }, (_, i) => ({
      id: i + 1,
      number: `KP/2026/${String(i + 1).padStart(4, '0')}`,
      subject: `Pismo numer ${i + 1}`,
      sender: 'Firma Testowa',
      receivedDate: '2026-09-24T00:00:00',
      createdDate: '2026-09-24T08:00:00',
      type: 1,
      isRead: false,
      recipientUserId: 1,
    })),
    contract: {
      id: 4,
      number: 'UM/2026/0004',
      subject: 'Dostawa komputerów',
      contractor: 'Firma ABC',
      contractDate: '2026-09-24T00:00:00',
      status: 1,
      createdByUserId: 1,
      responsibleUserId: 1,
      approvers: [
        { approverUserId: 2, role: 1, status: 1 },
        { approverUserId: 4, role: 2, status: 1 },
      ],
    },
    internal: {
      id: 5,
      number: 'KWN/2026/0005',
      subject: 'Zakup wyposażenia',
      description: 'Nowe monitory',
      createdAt: '2026-09-24T08:00:00',
      createdByUserId: 1,
      status: 1,
      approvers: [
        { approverUserId: 2, status: 1 },
        { approverUserId: 4, status: 1 },
      ],
    },
    attachments: [] as {
      id: number;
      originalFileName: string;
      fileSize: number;
      uploadedAt: string;
      type: number;
    }[],
    writes: [] as { path: string; user: number; method: string; body: any }[],
  };
  await page.addInitScript((id) => localStorage.setItem('secretariat.user.v1', String(id)), actor);
  await page.route('**/api/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname.replace('/api/', '');
    const user = Number(request.headers()['x-user-id']);
    const method = request.method();
    const json = (data: unknown, status = 200) =>
      route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(data) });
    if (method === 'GET') {
      if (path === 'appusers') return json(state.users);
      if (path.endsWith('/download'))
        return route.fulfill({ status: 200, contentType: 'application/pdf', body: '%PDF-1.4 test' });
      if (path.endsWith('/attachments')) return json(state.attachments);
      if (path === 'correspondence') return json(user === 5 ? [] : state.documents);
      if (path.startsWith('correspondence/'))
        return json(state.documents.find((d) => d.id === Number(path.split('/')[1])) || {}, 200);
      if (path === 'contracts') return json(user === 5 ? [] : [state.contract]);
      if (path.startsWith('contracts/'))
        return user === 5 ? json({ title: 'Brak dostępu' }, 403) : json(state.contract);
      if (path === 'internal-correspondence') return json(user === 5 ? [] : [state.internal]);
      if (path.startsWith('internal-correspondence/')) return json(state.internal);
    }
    const body = request.headers()['content-type']?.includes('json')
      ? request.postDataJSON()
      : request.postData();
    state.writes.push({ path, user, method, body });
    if (path.endsWith('/attachments')) {
      state.attachments.push({
        id: 1,
        originalFileName: 'podpis.pdf',
        fileSize: 100,
        uploadedAt: '2026-09-24T12:00:00',
        type: 2,
      });
      return json(state.attachments[0], 201);
    }
    if (path.endsWith('/read')) {
      state.documents.find((d) => d.id === Number(path.split('/')[1]))!.isRead = true;
      return route.fulfill({ status: 204 });
    }
    if (path.endsWith('/approve') || path.endsWith('/reject')) {
      const doc = path.startsWith('contracts') ? state.contract : state.internal;
      doc.approvers.find((a) => a.approverUserId === user)!.status = path.endsWith('/approve') ? 2 : 3;
      doc.status = path.endsWith('/approve') ? 2 : 4;
      return json({});
    }
    if (method === 'PUT') {
      if (path.startsWith('appusers/'))
        Object.assign(
          state.users.find((u) => u.id === Number(path.split('/')[1]))!,
          body,
        );
      else if (path.startsWith('contracts')) Object.assign(state.contract, body);
      else
        Object.assign(
          state.documents.find((d) => d.id === Number(path.split('/')[1]))!,
          body,
        );
      return route.fulfill({ status: 204 });
    }
    if (path === 'correspondence') {
      const doc = { ...state.documents[0], ...body, id: 13, number: 'KW/2026/0001' };
      state.documents.push(doc);
      return json(doc, 201);
    }
    if (path === 'contracts') {
      Object.assign(state.contract, body);
      return json(state.contract, 201);
    }
    if (path === 'internal-correspondence') {
      Object.assign(state.internal, body);
      return json(state.internal, 201);
    }
    if (path === 'appusers') {
      const u = { ...body, id: Math.max(...state.users.map((u) => u.id)) + 1 };
      state.users.push(u);
      return json(u, 201);
    }
    return json({ title: 'Nie znaleziono' }, 404);
  });
  return state;
}

test('lista, wyszukiwanie, paginacja i szczegóły', async ({ page }) => {
  await setup(page);
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/');
  await expect(page).toHaveTitle('Korespondencja · Sekretariat');
  await expect(page.getByRole('link', { name: 'KP/2026/0012', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Następna strona' }).click();
  await expect(page.getByRole('link', { name: 'KP/2026/0001', exact: true })).toBeVisible();
  await page.getByRole('searchbox').fill('numer 12');
  await page.getByRole('link', { name: 'Pismo numer 12', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Pismo numer 12' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Edytuj', exact: true })).toHaveCount(0);
  expect(errors).toEqual([]);
});

test('tworzenie odpowiedzi KW oraz zachowanie numeru przy edycji', async ({ page }) => {
  const state = await setup(page, 3);
  await page.goto('/');
  await page.getByRole('button', { name: 'Nowe pismo' }).click();
  await page.getByRole('combobox', { name: 'Typ korespondencji', exact: true }).selectOption('2');
  await page.getByLabel('Temat', { exact: true }).fill('Odpowiedź na zapytanie');
  await page.getByLabel('Nadawca', { exact: true }).fill('Sekretariat');
  await page.getByRole('combobox', { name: 'Adresat', exact: true }).selectOption('1');
  await page.getByRole('combobox', { name: 'Odpowiedź na pismo' }).selectOption('12');
  await page.getByRole('button', { name: 'Utwórz dokument' }).click();
  await expect(page.getByRole('heading', { name: 'Odpowiedź na zapytanie' })).toBeVisible();
  expect(state.writes[0]).toMatchObject({
    user: 3,
    body: { type: 2, recipientUserId: 1, relatedIncomingCorrespondenceId: 12 },
  });
  await page.getByRole('button', { name: 'Edytuj', exact: true }).click();
  await page.getByLabel('Temat', { exact: true }).fill('Poprawiona odpowiedź');
  await page.getByRole('button', { name: 'Zapisz zmiany' }).click();
  await expect(page.getByRole('heading', { name: 'Poprawiona odpowiedź' })).toBeVisible();
  expect(state.writes[1].body.number).toBe('KW/2026/0001');
});

test('zmiana użytkownika usuwa poprzednie dokumenty', async ({ page }) => {
  await setup(page);
  await page.goto('/#/contracts');
  await expect(page.getByRole('link', { name: 'Dostawa komputerów' })).toBeVisible();
  await page.getByLabel('Aktualny użytkownik').selectOption('5');
  await expect(page.getByRole('heading', { name: 'Brak dokumentów' })).toBeVisible();
  await expect(page.getByText('Dostawa komputerów')).toHaveCount(0);
});

test('oznaczanie przeczytania aktualizuje szczegóły', async ({ page }) => {
  const state = await setup(page);
  await page.goto('/#/correspondence/12');
  await page.getByRole('button', { name: 'Oznacz jako przeczytane' }).click();
  await expect(page.getByRole('button', { name: 'Oznacz jako przeczytane' })).toHaveCount(0);
  expect(state.writes[0]).toMatchObject({ path: 'correspondence/12/read', user: 1 });
});

test('umowa wymaga dwóch różnych akceptujących i wysyła poprawne dane', async ({ page }) => {
  const state = await setup(page);
  await page.goto('/#/contracts');
  await page.getByRole('button', { name: 'Nowa umowa' }).click();
  await page.getByLabel('Przedmiot umowy').fill('Nowa dostawa');
  await page.getByLabel('Kontrahent', { exact: true }).fill('Nowa Firma');
  await page.getByRole('combobox', { name: 'Kierownik', exact: true }).selectOption('2');
  await page.getByRole('combobox', { name: 'Dyrektor', exact: true }).selectOption('2');
  await page.getByRole('button', { name: 'Utwórz dokument' }).click();
  await expect(page.getByRole('alert')).toContainText('różnymi osobami');
  expect(state.writes).toHaveLength(0);
  await page.getByRole('combobox', { name: 'Dyrektor', exact: true }).selectOption('4');
  await page.getByRole('button', { name: 'Utwórz dokument' }).click();
  await expect(page.getByRole('heading', { name: 'Nowa dostawa' })).toBeVisible();
  expect(state.writes[0].body).toMatchObject({ managerUserId: 2, directorUserId: 4, responsibleUserId: 1 });
});

test('obieg wewnętrzny wymaga akceptującego', async ({ page }) => {
  const state = await setup(page);
  await page.goto('/#/internal-correspondence');
  await page.getByRole('button', { name: 'Nowy dokument' }).click();
  await page.getByLabel('Temat', { exact: true }).fill('Wniosek o zakup');
  await page.getByLabel('Opis', { exact: true }).fill('Zakup monitora');
  await page.getByRole('button', { name: 'Utwórz dokument' }).click();
  await expect(page.getByRole('alert')).toContainText('przynajmniej jednego');
  await page.getByRole('checkbox', { name: /Piotr Kierownik/ }).check();
  await page.getByRole('button', { name: 'Utwórz dokument' }).click();
  await expect(page.getByRole('heading', { name: 'Wniosek o zakup' })).toBeVisible();
  expect(state.writes[0].body.approverUserIds).toEqual([2]);
});

test('akceptacja z potwierdzeniem blokuje kolejną decyzję', async ({ page }) => {
  const state = await setup(page, 2);
  await page.goto('/#/contracts/4');
  await page.getByRole('button', { name: 'Zatwierdź', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  expect(state.writes).toHaveLength(0);
  await page.getByRole('button', { name: 'Potwierdź akceptację' }).click();
  await expect(page.getByRole('button', { name: 'Zatwierdź', exact: true })).toHaveCount(0);
  expect(state.writes[0]).toMatchObject({ path: 'contracts/4/approve', user: 2 });
});

test('odrzucenie obiegu wewnętrznego kończy proces', async ({ page }) => {
  const state = await setup(page, 2);
  await page.goto('/#/internal-correspondence/5');
  await page.getByRole('button', { name: 'Odrzuć', exact: true }).click();
  await page.getByRole('button', { name: 'Potwierdź odrzucenie' }).click();
  await expect(page.getByText('Odrzucony', { exact: true })).toBeVisible();
  expect(state.writes[0]).toMatchObject({ path: 'internal-correspondence/5/reject', user: 2 });
});

test('przesyłanie podpisanego dokumentu i pobieranie z nagłówkiem użytkownika', async ({ page }) => {
  const state = await setup(page, 2);
  await page.goto('/#/contracts/4');
  await page
    .getByLabel('Plik załącznika')
    .setInputFiles({ name: 'podpis.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4 test') });
  await page.getByRole('button', { name: 'Dodaj załącznik' }).click();
  await expect(page.getByText('podpis.pdf', { exact: true })).toBeVisible();
  expect(state.writes[0].body).toContain('name="type"\r\n\r\n2');
  const request = page.waitForRequest((req) => req.url().endsWith('/download'));
  const downloaded = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Pobierz podpis.pdf' }).click();
  expect((await request).headers()['x-user-id']).toBe('2');
  expect((await downloaded).suggestedFilename()).toBe('podpis.pdf');
});

test('błąd API i odzyskanie po ponowieniu', async ({ page }) => {
  await setup(page);
  let failing = true;
  await page.route('**/api/contracts', (route) =>
    failing ? route.fulfill({ status: 500 }) : route.fallback(),
  );
  await page.goto('/#/contracts');
  await expect(page.getByRole('alert')).toContainText('Serwer');
  failing = false;
  await page.getByRole('button', { name: 'Spróbuj ponownie' }).click();
  await expect(page.getByRole('link', { name: 'Dostawa komputerów' })).toBeVisible();
});

test('telefon: menu, formularz i brak przewijania całej strony w poziomie', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await setup(page, 3);
  await page.goto('/');
  await page.getByRole('button', { name: 'Otwórz menu' }).click();
  await page.getByRole('link', { name: 'Umowy', exact: true }).click();
  await page.getByRole('button', { name: 'Nowa umowa' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const box = await page.getByRole('dialog').boundingBox();
  expect(box!.width).toBeLessThanOrEqual(390);
  await page.getByRole('button', { name: 'Anuluj', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('tworzenie użytkownika odświeża listę oraz wybór profilu', async ({ page }) => {
  const state = await setup(page, 6);
  await page.goto('/#/users');
  await page.getByRole('button', { name: 'Nowy użytkownik' }).click();
  await page.getByLabel('Imię i nazwisko').fill('Nowa Osoba');
  await page.getByLabel('E-mail', { exact: true }).fill('nowa@example.test');
  await page.getByRole('button', { name: 'Dodaj użytkownika' }).click();
  await expect(page.getByRole('cell', { name: 'Nowa Osoba', exact: true })).toBeVisible();
  expect(state.writes[0].body.role).toBe(1);
});

test('administrator edytuje dane i role bez usuwania identyfikatora Entra', async ({ page }) => {
  const state = await setup(page, 6);
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/#/users');
  await expect(page).toHaveTitle('Użytkownicy · Sekretariat');
  await page.getByRole('button', { name: 'Edytuj użytkownika: Adam Pracownik', exact: true }).click();
  await expect(page.getByLabel('Imię i nazwisko')).toHaveValue('Adam Pracownik');
  await page.getByLabel('Imię i nazwisko').fill('Adam Administrator');
  await page.getByLabel('E-mail', { exact: true }).fill('nowy.adam@example.test');
  await page.getByRole('combobox', { name: 'Rola', exact: true }).selectOption('4');
  await page.getByRole('button', { name: 'Zapisz zmiany' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  const row = page
    .getByRole('row')
    .filter({ has: page.getByRole('cell', { name: 'Adam Administrator', exact: true }) });
  await expect(row.getByRole('cell', { name: 'Administrator', exact: true })).toBeVisible();
  expect(state.writes[0]).toMatchObject({
    path: 'appusers/1',
    method: 'PUT',
    user: 6,
    body: {
      displayName: 'Adam Administrator',
      email: 'nowy.adam@example.test',
      role: 4,
      entraObjectId: 'existing-entra-id',
    },
  });
  await page.getByLabel('Aktualny użytkownik').selectOption('1');
  await expect(page.getByRole('button', { name: 'Nowy użytkownik' })).toBeVisible();
  expect(errors).toEqual([]);
});

for (const actor of [1, 2, 3, 7]) {
  test(`rola użytkownika ${actor} nie udostępnia zarządzania kontami`, async ({ page }) => {
    await setup(page, actor);
    await page.goto('/#/users');
    await expect(page.getByRole('heading', { name: 'Użytkownicy', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Nowy użytkownik' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /^Edytuj użytkownika:/ })).toHaveCount(0);
  });
}

test('administrator nie może zmienić własnej roli, anulowanie nie zapisuje danych', async ({ page }) => {
  const state = await setup(page, 6);
  await page.goto('/#/users');
  await page.getByRole('button', { name: 'Edytuj użytkownika: Administrator', exact: true }).click();
  await expect(page.getByRole('combobox', { name: 'Rola', exact: true })).toBeDisabled();
  await page.getByLabel('Imię i nazwisko').fill('Niezapisana zmiana');
  await page.getByRole('button', { name: 'Anuluj', exact: true }).click();
  expect(state.writes).toHaveLength(0);
  await page.getByRole('button', { name: 'Nowy użytkownik' }).click();
  await expect(page.getByLabel('Imię i nazwisko')).toHaveValue('');
  await expect(page.getByRole('combobox', { name: 'Rola', exact: true })).toHaveValue('1');
});

test('odmowa zapisu zachowuje formularz i pozwala ponowić zmianę roli', async ({ page }) => {
  const state = await setup(page, 6);
  let failing = true;
  await page.route('**/api/appusers/1', (route) =>
    failing ? route.fulfill({ status: 403, body: 'Brak uprawnień do edycji.' }) : route.fallback(),
  );
  await page.goto('/#/users');
  await page.getByRole('button', { name: 'Edytuj użytkownika: Adam Pracownik', exact: true }).click();
  await page.getByRole('combobox', { name: 'Rola', exact: true }).selectOption('2');
  await page.getByRole('button', { name: 'Zapisz zmiany' }).click();
  await expect(page.getByRole('alert')).toContainText('Brak uprawnień');
  await expect(page.getByRole('combobox', { name: 'Rola', exact: true })).toHaveValue('2');
  expect(state.writes).toHaveLength(0);
  failing = false;
  await page.getByRole('button', { name: 'Zapisz zmiany' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(state.users.find((u) => u.id === 1)?.role).toBe(2);
});

test('telefon: edycja starszego konta wymaga nadania poprawnej roli', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const state = await setup(page, 6);
  await page.goto('/#/users');
  await page.getByRole('button', { name: 'Edytuj użytkownika: Starsze Konto', exact: true }).click();
  await expect(page.getByRole('combobox', { name: 'Rola', exact: true })).toHaveValue('');
  await page.getByRole('button', { name: 'Zapisz zmiany' }).click();
  expect(state.writes).toHaveLength(0);
  await page.getByRole('combobox', { name: 'Rola', exact: true }).selectOption('3');
  const box = await page.getByRole('dialog').boundingBox();
  expect(box!.width).toBeLessThanOrEqual(390);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.getByRole('button', { name: 'Zapisz zmiany' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(state.users.find((u) => u.id === 7)?.role).toBe(3);
});

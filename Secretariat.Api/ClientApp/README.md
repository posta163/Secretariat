# Sekretariat: frontend

React, TypeScript i Vite. Dane pochodza z istniejacego API, bez danych demonstracyjnych w aplikacji.

## Uruchomienie razem z API

Z katalogu `ClientApp`:

```powershell
npm ci
npm run build
```

Z katalogu `Secretariat.Api`:

```powershell
dotnet run --launch-profile http
```

Adres: http://localhost:5244. Pliki produkcyjne trafiaja do `wwwroot` i sa serwowane przez ASP.NET Core. Po kazdej zmianie frontendu ponownie wykonaj `npm run build`. Przy publikowaniu najpierw zbuduj frontend, a potem wykonaj `dotnet publish`. Nie trzeba konfigurowac CORS.

## Praca nad interfejsem

Uruchom API, a w `ClientApp` wykonaj `npm run dev`. Vite przekazuje `/api` do http://localhost:5244. Zmienna `API_PROXY_TARGET` pozwala wskazac inny adres backendu. W przegladarce otworz adres wypisany przez Vite.

## Zakres

- Korespondencja KP/KW: lista, filtry, tworzenie, szczegoly, edycja, oznaczanie odczytu i zalaczniki.
- Obieg wewnetrzny: dokumenty, wybor akceptujacych, szczegoly i decyzje.
- Umowy: lista, tworzenie, edycja przed akceptacja, decyzje, dokumenty oryginalne i podpisane.
- Uzytkownicy: lista i tworzenie dla Sekretariatu oraz Administratora.
- Uklad responsywny, formularze w dialogach, stany pustych list, bledow i ladowania.

Identyfikacja korzysta z istniejacego `X-User-Id`. Wybor konta w bocznym panelu jest lokalnym mechanizmem testowym, nie logowaniem. Kontrola przyciskow po stronie klienta nie zastepuje autoryzacji API. Istniejacy endpoint tworzenia uzytkownikow nadal wymaga zabezpieczenia przed wdrozeniem publicznym.

Frontend nie udaje funkcji, ktorych API nie obsluguje: usuwania, edycji obiegu wewnetrznego, zmiany akceptujacych ani weryfikacji podpisu. Edycja pisma zachowuje nadany numer i pola nieedytowalne przez obecny endpoint.

## Sprawdzenie

```powershell
npm run typecheck
npm run build
npm test
```

Testy Playwright uruchamiaja Vite na osobnym porcie i uzywaja kontrolowanych odpowiedzi API. Nie zmieniaja istniejacej bazy. Pierwsze uruchomienie moze wymagac `npx playwright install chromium`.

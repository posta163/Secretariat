## Dzień 1

## Aktualny stan projektu

Zrealizowano:

- utworzenie rozwiązania `Secretariat`,
- utworzenie projektu `Secretariat.Api`,
- konfigurację HTTPS i OpenAPI,
- konfigurację Entity Framework Core,
- konfigurację SQL Server LocalDB,
- utworzenie bazy `SecretariatDb`,
- utworzenie pierwszej migracji,
- utworzenie modelu `Correspondence`,
- pobieranie listy korespondencji,
- pobieranie pojedynczej korespondencji po ID,
- dodawanie nowej korespondencji,
- edycję istniejącej korespondencji,
- automatyczne nadawanie numeru korespondencji.

### Aktualnie dostępne endpointy

| Metoda		| Endpoint						| Opis								|
|				|								|									|
| GET			| `/api/correspondence`			| Pobiera listę korespondencji		|
| GET			| `/api/correspondence/{id}`	| Pobiera szczegóły korespondencji	|
| POST			| `/api/correspondence`			| Dodaje nową korespondencję		|
| PUT			| `/api/correspondence/{id}`	| Edytuje istniejącą korespondencję |
			
### Automatyczna numeracja

Numer korespondencji jest generowany przez backend w formacie:

`COR/RRRR/NNNN`

Przykład:

`COR/2026/0002`

W obecnej wersji demonstracyjnej numer jest generowany na podstawie liczby dokumentów utworzonych w danym roku.



> Aktualizacja: w Dniu 2 po dodaniu typów korespondencji mechanizm został rozszerzony o osobną numerację `KP` dla korespondencji przychodzącej oraz `KW` dla wychodzącej.



## Dzień 2 

Drugiego dnia prac rozszerzono moduł korespondencji o obsługę użytkowników, adresatów oraz typów korespondencji. 
Przygotowano również strukturę użytkownika pod przyszłą integrację z Microsoft Entra ID.

### Użytkownicy

Utworzono model `AppUser` przechowujący podstawowe informacje o użytkowniku:

- `Id` – lokalny identyfikator użytkownika,
- `DisplayName` – imię i nazwisko użytkownika,
- `Email` – adres e-mail,
- `EntraObjectId` – opcjonalny identyfikator użytkownika Microsoft Entra ID.

Pole `EntraObjectId` obecnie może mieć wartość `null`.

Zostało dodane w celu przygotowania aplikacji pod przyszłą integrację z Microsoft 365 i Microsoft Entra ID.

### Obsługa użytkowników w bazie danych

Do `SecretariatDbContext` dodano:
public DbSet<AppUser> AppUsers { get; set; }


### Endpointy dodane w Dniu 2

| Metoda	| Endpoint			| Opis							|
|			|					|								|
| GET		| `/api/appusers`	| Pobiera listę użytkowników	|
| POST		| `/api/appusers`	| Dodaje nowego użytkownika		|



## Dzień 3


Trzeciego dnia prac rozszerzono moduł korespondencji o obsługę załączników oraz oznaczanie korespondencji jako odczytanej.

### Załączniki

Utworzono model:

CorrespondenceAttachment


## Dzień 4

- Dodano role użytkowników.
- Dodano lokalny mechanizm identyfikacji aktualnego użytkownika.
- Dodano filtrowanie korespondencji zależnie od roli użytkownika.
- Dodano relację pomiędzy korespondencją wychodzącą i przychodzącą.
- Dodano walidację relacji KW → KP.
- Rozszerzono szczegóły korespondencji o powiązany dokument.
- Utworzono moduł korespondencji wewnętrznej.
- Dodano automatyczną numerację KWN.
- Dodano tworzenie, listowanie i pobieranie szczegółów korespondencji wewnętrznej.
- Autor korespondencji wewnętrznej jest ustalany na podstawie aktualnego użytkownika.


### Role użytkowników

Role użytkowników są przechowywane jako wartości liczbowe  `UserRole`.

| Wartość	| Nazwa w kodzie	| Znaczenie			|
|			|					|					|
| 1			| Employee			| Pracownik			|
| 2			| Approver			| Akceptujący		|
| 3			| Secretariat		| Sekretariat		|
| 4			| Administrator		| Administrator		|


###  Typ korespondencji

Typ korespondencji jest przechowywany `CorrespondenceType`.

|Wartość	|	Nazwa w kodzie		| Znaczenie		|
|			|						|				|
|0			| Unknown				| typ			|
|1			| Incoming				| przychodząca	|
|2			| Outgoing				| wychodząca	|


Status korespondencji wewnętrznej

Status korespondencji wewnętrznej jest przechowywany  `InternalCorrespondenceStatus`.

|Wartość	|	Nazwa w kodzie		| Znaczenie		|
|			|						|				|
|1			| New					| Nowa			|
|2			| InProgress			| W trakcie		|
|3			| Approved				| Zaakceptowana	|
|4			| Rejected				| Odrzucona		|



## Dzień 5

Piątego dnia prac rozbudowano moduł korespondencji wewnętrznej o proces akceptacji dokumentów przez wyznaczonych użytkowników.

Zrealizowano:

- utworzenie modelu `InternalCorrespondenceApprover`,
- dodanie indywidualnych statusów akceptacji,
- skonfigurowanie relacji między dokumentami a użytkownikami,
- utworzenie migracji `AddInternalCorrespondenceApprovers`,
- dodanie DTO `CreateInternalCorrespondenceRequest`,
- możliwość wskazania kilku akceptujących podczas tworzenia dokumentu,
- walidację użytkowników posiadających rolę `Approver`,
- obsługę zatwierdzania i odrzucania dokumentów,
- automatyczną aktualizację statusu całej korespondencji,
- zapisywanie daty podjęcia decyzji przez każdego akceptującego.

### Przypisywanie akceptujących

Podczas tworzenia korespondencji wewnętrznej można wskazać identyfikatory użytkowników odpowiedzialnych za jej akceptację.

Backend sprawdza, czy:

- wskazano przynajmniej jednego akceptującego,
- lista nie zawiera powtarzających się użytkowników,
- wszyscy wskazani użytkownicy istnieją,
- każdy z nich posiada rolę `Approver`.

Autor, numer dokumentu oraz początkowy status są ustalane przez backend. Każdy przypisany akceptujący otrzymuje początkowy status `Pending`.

### Model i relacje

Utworzono model `InternalCorrespondenceApprover`, który łączy dokument z przypisanym użytkownikiem oraz przechowuje jego indywidualną decyzję i datę jej podjęcia.

W `SecretariatDbContext` skonfigurowano relacje i unikalny indeks uniemożliwiający dwukrotne przypisanie tego samego użytkownika do jednego dokumentu.

### Proces akceptacji

- `New` – nowy dokument oczekujący na decyzje.
- `InProgress` – przynajmniej jedna osoba zaakceptowała dokument, ale pozostałe decyzje są jeszcze wymagane.
- `Approved` – wszyscy przypisani akceptujący zaakceptowali dokument.
- `Rejected` – przynajmniej jedna osoba odrzuciła dokument.

Jedno odrzucenie kończy cały proces. Po osiągnięciu końcowego statusu `Approved` lub `Rejected` nie można podejmować kolejnych decyzji.

### Status pojedynczej akceptacji

| Wartość	| Nazwa w kodzie	| Znaczenie				|
|--------	|----------------	|-----------			|
| 1			| Pending			| Oczekuje na decyzję	|
| 2			| Approved			| Zaakceptowana			|
| 3			| Rejected			| Odrzucona				|

Status pojedynczej akceptacji jest przechowywany jako `InternalApprovalStatus` i jest niezależny od statusu całego dokumentu.

### Endpointy

| Metoda	| Endpoint										| Opis													|
|--------	|----------										|------													|
| POST		| `/api/internal-correspondence`				| Tworzy dokument z wybranymi akceptującymi				|
| GET		| `/api/internal-correspondence`				| Pobiera listę dokumentów								|
| GET		| `/api/internal-correspondence/{id}`			| Pobiera szczegóły dokumentu i statusy akceptujących	|
| POST		| `/api/internal-correspondence/{id}/approve`	| Zatwierdza dokument przez przypisanego akceptującego	|
| POST		| `/api/internal-correspondence/{id}/reject`	| Odrzuca dokument przez przypisanego akceptującego		|





## Dzień 6

Szóstego dnia prac utworzono podstawowy moduł zarządzania umowami. Dodano modele, strukturę bazy danych, automatyczną numerację oraz endpointy umożliwiające tworzenie, przeglądanie, wyszukiwanie i edycję umów.

Moduł został przygotowany pod planowany proces akceptacji umów przez dwóch użytkowników oraz późniejszą integrację z Microsoft 365.

### Model umowy

Utworzono model `Contract`, zawierający następujące informacje:

- `Id` – identyfikator umowy,
- `Number` – automatycznie generowany numer,
- `Contractor` – kontrahent,
- `Subject` – przedmiot umowy,
- `ContractDate` – data zawarcia umowy,
- `ValidFrom` i `ValidTo` – okres obowiązywania,
- `ContractualPenalties` – kary umowne,
- `Comment` – dodatkowy komentarz,
- `CreatedAt` – data utworzenia rekordu,
- `CreatedByUserId` – autor wniosku,
- `ResponsibleUserId` – osoba odpowiedzialna za umowę,
- `Status` – aktualny status umowy.

Autor wniosku i osoba odpowiedzialna mogą być różnymi użytkownikami.

### Statusy umów

Utworzono osobny enum `ContractStatus`.

| Wartość	| Nazwa w kodzie	| Znaczenie				|
|--------:	|----------------	|-----------			|
| 1			| New				| Nowa					|
| 2			| InProgress		| W trakcie akceptacji	|
| 3			| Approved			| Zaakceptowana			|
| 4			| Rejected			| Odrzucona				|

Obecnie nowe umowy otrzymują status `New`. Pozostałe statusy zostaną wykorzystane w procesie akceptacji.

### Baza danych

Do `SecretariatDbContext` dodano:

`DbSet<Contract> Contracts`

Skonfigurowano dwie relacje z tabelą `AppUsers`:

- autor wniosku,
- osoba odpowiedzialna za umowę.

Zastosowano `DeleteBehavior.Restrict`, aby usunięcie użytkownika nie powodowało automatycznego usunięcia powiązanych umów.

Dodano również unikalny indeks dla numeru umowy.

Utworzono i wykonano migrację `AddContracts`.

### Automatyczna numeracja

Numer umowy jest generowany przez backend w formacie:

`UM/RRRR/NNNN`

Przykład:

`UM/2026/0001`

### DTO i walidacja

Utworzono:

- `CreateContractRequest`,
- `UpdateContractRequest`.

Zastosowanie DTO oddziela dane przesyłane przez klienta od encji bazy danych.

Backend samodzielnie ustala numer umowy, autora, datę utworzenia i początkowy status.

Dodano walidację:

- wymaganych danych kontrahenta i przedmiotu umowy,
- daty zawarcia umowy,
- poprawności okresu obowiązywania,
- istnienia osoby odpowiedzialnej.

### Endpointy modułu umów

| Metoda	| Endpoint						| Opis						|
|--------	|----------						|------						|
| POST		| `/api/contracts`				| Tworzy nową umowę			|
| GET		| `/api/contracts`				| Pobiera listę umów		|
| GET		| `/api/contracts/{id}`			| Pobiera szczegóły umowy	|
| GET		| `/api/contracts?search=tekst` | Wyszukuje umowy			|
| PUT		| `/api/contracts/{id}`			| Edytuje istniejącą umowę	|

### Wyszukiwanie

Dodano wyszukiwanie umów według:

- numeru umowy,
- nazwy kontrahenta,
- przedmiotu umowy.

Wykorzystano LINQ i `IQueryable`, dzięki czemu filtrowanie jest wykonywane w bazie danych, a nie na wcześniej pobranej kolekcji.

W zapytaniach służących wyłącznie do odczytu zastosowano `AsNoTracking()`.

### Edycja umów

Dodano możliwość edycji nowych wniosków o umowę.

Uprawnienia do edycji posiadają:

- autor wniosku,
- Sekretariat,
- Administrator.

Edycja jest możliwa wyłącznie dla umów o statusie `New`.

Numer, autor, data utworzenia i status nie podlegają edycji przez ten endpoint.






## Dzień 7

Siódmego dnia prac rozbudowano moduł umów o proces akceptacji przez kierownika i dyrektora oraz obsługę załączników.

Wykorzystano rozwiązania opracowane wcześniej dla korespondencji wewnętrznej, dostosowując je do procesu zatwierdzania umów przez dwie różne osoby.

### Proces akceptacji umów

Utworzono modele:

- `ContractApprover`,
- `ContractApproverRole`,
- `ContractApprovalStatus`.

Każda umowa może mieć dwóch przypisanych akceptujących: kierownika i dyrektora.

Podczas tworzenia umowy backend sprawdza, czy obie osoby istnieją, posiadają rolę `Approver` i są różnymi użytkownikami.

Każdy akceptujący ma własny status oraz datę podjęcia decyzji.

### Role akceptujących

| Wartość	| Nazwa w kodzie	| Znaczenie |
|--------:	|----------------	|-----------|
| 1			| Manager			| Kierownik |
| 2			| Director			| Dyrektor	|

### Statusy akceptacji

| Wartość	| Nazwa w kodzie	| Znaczenie				|
|--------:	|----------------	|-----------			|
| 1			| Pending			| Oczekuje na decyzję	|
| 2			| Approved			| Zaakceptowana			|
| 3			| Rejected			| Odrzucona				|

Zatwierdzenie przez pierwszego akceptującego zmienia status umowy na `InProgress`.

Umowa otrzymuje status `Approved` po zatwierdzeniu przez obie osoby.

Odrzucenie przez jednego akceptującego kończy proces i zmienia status umowy na `Rejected`.

Dodano również blokadę ponownego podejmowania decyzji oraz zmiany zakończonego procesu akceptacji.


### Załączniki do umów

Utworzono modele:

- `ContractAttachment`,
- `ContractAttachmentType`.

Rozróżniono dwa rodzaje załączników:

| Wartość	| Nazwa w kodzie	| Znaczenie				|
|--------:	|----------------	|-----------			|
| 1			| Original			| Dokument podstawowy	|
| 2			| Signed			| Dokument podpisany	|
	
Model załącznika przechowuje nazwę pliku, rozmiar, typ, ścieżkę, datę przesłania oraz identyfikator użytkownika, który dodał dokument.

Rozszerzono `IFileStorage` i `LocalFileStorage` o obsługę plików umów.

Pliki są zapisywane lokalnie w katalogu:

`uploads/contracts/{contractId}`

Metadane plików są przechowywane w bazie danych.



### Bezpieczeństwo załączników

Dodano kontrolę dostępu do przesyłania i pobierania plików.

Dokument podstawowy mogą przesłać uprawnieni użytkownicy, jeżeli umowa ma status `New`.

Podpisaną wersję może przesłać przypisany akceptujący przed podjęciem własnej decyzji.

Wprowadzono ograniczenie rozmiaru pliku do 10 MB oraz podstawową walidację dozwolonych rozszerzeń.

W obecnym MVP aplikacja nie weryfikuje kryptograficznej poprawności podpisu elektronicznego.

### Endpointy dodane w Dniu 7

| Metoda	| Endpoint													| Opis						|
|--------																|----------					|
| POST		| `/api/contracts/{id}/approve`								| Zatwierdza umowę			|
| POST		| `/api/contracts/{id}/reject`								| Odrzuca umowę				|
| POST		| `/api/contracts/{id}/attachments`							| Przesyła załącznik		|
| GET		| `/api/contracts/{id}/attachments`							| Pobiera listę załączników |
| GET		| `/api/contracts/{id}/attachments/{attachmentId}/download` | Pobiera wskazany plik		|

Rozszerzono również endpoint szczegółów umowy o listę akceptujących, ich funkcje, statusy i daty decyzji.




## Dzień 8 – zakończenie backendu 

Ósmego dnia prac skupiono się na podstawowej kontroli dostępu,
testowaniu istniejących funkcjonalności i zakończeniu lokalnej
wersji backendu.

### Kontrola dostępu

Rozszerzono sprawdzanie uprawnień w trzech modułach:

- Korespondencja przychodząca i wychodząca:
  dostęp do dokumentów, edycji i załączników.
- Umowy:
  dostęp do szczegółów, listy umów i załączników.
- Korespondencja wewnętrzna:
  dostęp autora i przypisanych akceptujących.

Sekretariat i Administrator posiadają szersze uprawnienia,
zgodnie z przyjętymi zasadami projektu.

### Testy końcowe

Przeprowadzono 18 ręcznych testów HTTP w Visual Studio.

Sprawdzono między innymi:
- dostęp pracownika do własnej korespondencji,
- blokowanie dostępu do cudzych dokumentów,
- edycję korespondencji przez Sekretariat,
- oznaczanie korespondencji jako przeczytanej,
- przesyłanie i pobieranie załączników,
- dostęp do zatwierdzonych umów,
- filtrowanie listy umów,
- dostęp autora i akceptujących do korespondencji wewnętrznej.

Wszystkie 18 testów zakończyło się oczekiwanym rezultatem.

### Zakres ukończonego MVP

Backend obejmuje:

- rejestrację korespondencji przychodzącej i wychodzącej,
- powiązanie odpowiedzi z korespondencją przychodzącą,
- obsługę załączników,
- oznaczanie korespondencji jako przeczytanej,
- korespondencję wewnętrzną z procesem akceptacji,
- rejestrację i edycję umów,
- zatwierdzanie umów przez kierownika i dyrektora,
- możliwość odrzucenia umowy,
- przesyłanie i pobieranie dokumentów umów,
- podstawową kontrolę dostępu według ról.

### Ograniczenia obecnej wersji

Projekt jest lokalnym MVP 

Użytkownik jest identyfikowany za pomocą nagłówka X-User-Id.
Mechanizm ten służy wyłącznie do testów lokalnych


Podpisane dokumenty można przesyłać i pobierać.

Nie zaimplementowano rzeczywistej wysyłki powiadomień e-mail


### Możliwości dalszego rozwoju

- Logowanie za pomocą Microsoft Entra ID.
- Powiadomienia e-mail przez Microsoft Graph.
- Automatyczne testy API.
- Przechowywanie plików w Azure Blob Storage.
- Wdrożenie aplikacji i bazy danych do Azure.



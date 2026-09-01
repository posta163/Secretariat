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


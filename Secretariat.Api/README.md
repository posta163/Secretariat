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


# Revenue Recognition System


## Funkcjonalności

* dodawanie klientów indywidualnych i firm,
* aktualizacja danych klientów,
* miękkie usuwanie klientów indywidualnych,
* blokowanie usuwania firm,
* przygotowywanie kontraktów na zakup licencji,
* obliczanie najwyższej aktywnej zniżki,
* dodatkowa zniżka dla powracającego klienta,
* dodatkowe lata wsparcia,
* płatności jednorazowe i ratalne,
* automatyczne podpisanie w pełni opłaconego kontraktu,
* zwracanie wpłat po przekroczeniu terminu,
* aktualny i przewidywany przychód,
* filtrowanie przychodu według produktu,
* przeliczanie przychodu na inne waluty,
* uwierzytelnianie JWT,
* role Employee i Admin,
* testy jednostkowe logiki biznesowej.


## Konta startowe

Administrator:

```text
login: admin
hasło: admin123!
```

Pracownik:

```text
login: employee
hasło: employee123!
```

## Autoryzacja

Endpoint:

```http
POST /api/auth/login
```

zwraca token JWT. Token należy podać w Swaggerze za pomocą przycisku `Authorize`.

Wszystkie endpointy biznesowe wymagają zalogowania.

Edycja oraz usuwanie klientów wymagają roli `Admin`.

## Migracje

Utworzenie nowej migracji:

```bash
dotnet ef migrations add NazwaMigracji --project RevenueRecognition.Api --startup-project RevenueRecognition.Api
```

Aktualizacja bazy:

```bash
dotnet ef database update --project RevenueRecognition.Api --startup-project RevenueRecognition.Api
```

## Testy

Testy obejmują:

* tworzenie i usuwanie klientów,
* blokowanie duplikatów PESEL,
* zakaz usuwania firmy,
* okres kontraktu,
* najwyższą aktywną zniżkę,
* zniżkę dla powracającego klienta,
* blokowanie drugiego aktywnego kontraktu,
* płatności ratalne,
* blokowanie nadpłaty,
* zwroty po terminie,
* aktualny przychód,
* przewidywany przychód,
* filtrowanie według produktu,
* przeliczanie walut.

## Przykładowe endpointy

```text
POST   /api/auth/login

POST   /api/clients/individuals
POST   /api/clients/companies
PUT    /api/clients/individuals/{clientId}
PUT    /api/clients/companies/{clientId}
DELETE /api/clients/{clientId}

POST   /api/contracts/{contractId}/payments

POST   /api/contracts
DELETE /api/contracts/{contractId}

GET    /api/revenue/current
GET    /api/revenue/expected
```

# Backend Setup

Ten dokument opisuje aktualny backend ProcureFlow, odziedziczony fundament
techniczny i sposób dodawania nowych modułów produktu bez psucia istniejącej
struktury.

## When To Read This Document

Czytaj ten plik, gdy zmiana dotyczy API, konfiguracji, persistence, serwisów backendowych, walidacji albo testów backendu.

## Current Backend Responsibilities

Backend odpowiada za:

- autoryzację i uwierzytelnianie użytkownika
- generowanie JWT i rotację refresh tokenów
- email confirmation i email-based 2FA
- password reset
- role-based authorization
- dostarczanie runtime feature flags dla frontendu
- persistence przez EF Core i PostgreSQL
- przejściową domenę demonstracyjną projektów i zadań wraz z członkostwem,
  załącznikami, terminami i powiadomieniami
- health checks, korelacja żądań i cykliczne workery infrastrukturalne

## Product Migration Boundary

PF1-PF5 dodają docelowe moduły `Organizations`, `Catalog` i
`PurchaseRequests`. `Projects` oraz `ProjectTasks` pozostają dostępne tylko jako
zweryfikowana domena demonstracyjna i są usuwane w PF6 po przejściu testów
zastępującego workflow.

Mechanizmy auth, email outboxu, storage, malware scanning, health checks i
correlation ID są fundamentem nadającym się do ponownego użycia. Ich obecne
kontrakty nie zawsze są jednak neutralne domenowo. W szczególności model
powiadomień zawiera `ProjectId`, a porty i metadata załączników odnoszą się do
`ProjectTask`. W PF5 należy zachować sprawdzoną mechanikę, ale wystawić nowe
kontrakty należące do `PurchaseRequests`, zamiast wykonywać mechaniczny rename.

Szczegółową kolejność określa
[roadmapa ProcureFlow](ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).

## Backend Layers

### API

Warstwa `API/` zawiera:

- kontrolery HTTP
- middleware
- konfigurację hosta
- rejestrację usług

Najważniejsze pliki:

- `API/Program.cs`
- `API/Services/AddProjectServices.cs`
- `API/Controllers/AuthController.cs`
- `API/Controllers/UsersController.cs`
- `API/Controllers/AdminController.cs`
- `API/Controllers/RuntimeConfigController.cs`

### Application

Warstwa `Application/` zawiera:

- DTO request/response
- interfejsy serwisów aplikacyjnych
- walidatory
- mapowania

To miejsce na kontrakty wejścia/wyjścia oraz logikę orkiestrującą, która nie powinna siedzieć w kontrolerach.

### Domain

Warstwa `Domain/` zawiera:

- encje
- enumy
- value objects
- interfejsy domenowe

To miejsce na model biznesowy niezależny od HTTP i infrastruktury.

### Infrastructure

Warstwa `Infrastructure/` zawiera:

- `ApplicationDbContext`
- migracje EF Core
- implementacje serwisów opartych o bazę danych
- implementacje usług infrastrukturalnych, np. email senderów

Najważniejszy plik auth po stronie persistence:

- `Infrastructure/Services/DatabaseAuthService.cs`

### Shared

Warstwa `Shared/` zawiera:

- `Responses/` z ustandaryzowanym `ApiResponse`
- `Settings/` z klasami bindowanymi z konfiguracji
- DTO współdzielone między backendem i frontendem, np. runtime config

## Configuration Model

Backend korzysta z trzech warstw konfiguracji:

1. `appsettings.json`
2. `appsettings.Development.json`
3. environment variables / secrets

Najważniejsze sekcje konfiguracji:

- `Jwt`
- `Cors`
- `EmailConfirmation`
- `EmailTwoFactor`
- `AuthSecurity`
- `EmailDelivery`
- `UiFeatures`
- `DataProtection`
- `ForwardedHeaders`

### Data Protection And Reverse Proxy

Sekret authenticatora jest chroniony przez ASP.NET Core Data Protection. `ApplicationName`
jest identyfikatorem aplikacji, a `KeyRingPath` wskazuje katalog kluczy. W produkcji
`KeyRingPath` musi być ustawiony na absolutną, trwałą lokalizację współdzieloną przez
instancje aplikacji. Jeśli klucze znikną, istniejące sekrety authenticatora nie będą mogły
zostać odszyfrowane.

Compose montuje named volume `data-protection-keys` do
`/home/app/.aspnet/DataProtection-Keys`. Nie używaj `docker compose down -v` podczas
zwykłego zatrzymywania środowiska, ponieważ usunięcie tego wolumenu unieważni dane
zaszyfrowane poprzednim key ringiem.

`ForwardedHeaders` włącza obsługę `X-Forwarded-For` i `X-Forwarded-Proto` tylko dla
jawnie skonfigurowanych `KnownProxies` lub `KnownNetworks`. `ForwardLimit` ogranicza
liczbę akceptowanych wpisów w łańcuchu. W Docker Compose frontendowy Nginx działa w
podsieci `172.28.0.0/16`, dlatego lokalny przykład ustawia tę sieć jako zaufaną.
W produkcji wpisz rzeczywisty zakres reverse proxy i nie używaj zaufania do wszystkich
adresów.

Bazowy `appsettings.json` zachowuje produkcyjną politykę `CookieSecurePolicy=Always`,
natomiast `appsettings.Development.json` używa `SameAsRequest`, aby lokalny backend po
HTTP mógł ustawiać refresh cookie. Walidacja przy starcie odrzuca w produkcji znany
przykładowy JWT secret, brak trwałego key ringu, niezabezpieczoną politykę cookie,
niepoprawne originy CORS oraz niepełną konfigurację zaufanego proxy.

## Health Checks And Request Correlation

API udostępnia osobne sygnały operacyjne:

- `GET /health` sprawdza API i bazę danych, z pominięciem workerów;
- `GET /health/live` sprawdza liveness procesu;
- `GET /health/ready` uruchamia `DatabaseHealthCheck` i sprawdza możliwość połączenia z bazą;
- `GET /health/workers` sprawdza ostatni stan workerów outbox i przypomnień o terminach.

Middleware `CorrelationIdMiddleware` odczytuje `X-Correlation-ID` z żądania albo używa identyfikatora wygenerowanego przez ASP.NET Core. Wartość trafia do nagłówka odpowiedzi i kontekstu Seriloga, dzięki czemu można śledzić żądanie w logach.

Workery zapisują ostatni sukces lub błąd w singletonowym `BackgroundWorkerHealthState`. Brak pierwszego raportu albo przekroczenie maksymalnego wieku raportu oznacza stan niezdrowy na `/health/workers`; nie blokuje to liveness procesu.

Ważne zasady:

- sekrety nie trafiają do repozytorium
- CORS jest konfigurowany przez settings, nie hardcode w `Program.cs`
- walidacja opcji odbywa się przy starcie w `AddProjectServices.cs`
- `EmailDelivery.Enabled = false` pozwala lokalnie działać bez zewnętrznego SMTP

## Authentication Flow

Najważniejsze endpointy auth:

- `POST /api/auth/register`
- `POST /api/auth/confirm-email`
- `POST /api/auth/resend-confirmation`
- `POST /api/auth/login`
- `POST /api/auth/verify-2fa`
- `POST /api/auth/resend-2fa`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`
- `POST /api/auth/logout-all`
- `GET /api/auth/me`
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`

Typowy flow logowania:

1. `AuthController` przyjmuje request.
2. `DatabaseAuthService` uwierzytelnia użytkownika po emailu i haśle.
3. Jeśli email niepotwierdzony, backend blokuje login.
4. Jeśli 2FA jest aktywne, backend tworzy challenge i odsyła `202 Accepted`.
5. Jeśli 2FA nie jest wymagane, backend zwraca access token i ustawia refresh token cookie.

### Auth Abuse Protection

Endpointy auth używają polityki `AuthPolicy`. Jest to fixed-window rate limit
partycjonowany po adresie IP klienta i ścieżce endpointu. Domyślne ustawienia znajdują się
w sekcji `AuthSecurity`:

- `RateLimitPermitLimit`: 5 żądań;
- `RateLimitWindowSeconds`: 60 sekund;
- `MaxFailedLoginAttempts`: 5 kolejnych nieudanych haseł;
- `LockoutDurationMinutes`: 15 minut blokady logowania.

Limit obejmuje rejestrację, logowanie, potwierdzenie i ponowienie potwierdzenia emaila,
operacje email 2FA, refresh token, weryfikację tokenu, oba endpointy resetu hasła,
zmianę hasła oraz operacje authenticatora.
Przekroczenie limitu zwraca neutralne `429 Too Many Requests` i nie ujawnia danych konta.

Nieudane hasło zwiększa trwały licznik użytkownika. Po przekroczeniu progu konto jest
tymczasowo blokowane, a poprawne hasło podczas blokady nadal zwraca neutralne `401`
(`Invalid email or password`). Po poprawnym logowaniu licznik jest zerowany. Stan lockoutu
jest chroniony przez `ConcurrencyStamp`, więc równoległa aktualizacja nie nadpisuje cicho
nowszego stanu.

W środowisku za reverse proxy adres IP musi być poprawnie odtworzony przez konfigurację
`ForwardedHeaders`; bez niej wiele klientów może zostać przypisanych do adresu proxy.

Pełna decyzja bezpieczeństwa jest opisana w [ADR-09: Authentication brute-force protection](ROADMAP/09_ADR_AUTH_BRUTE_FORCE_PROTECTION.md).

## Current Database Model

Aktualny model bazy obejmuje auth, użytkowników oraz przejściową domenę projektów
i zadań. Tabele ProcureFlow będą dodawane etapami, a schema demo pozostanie do
kontrolowanego cleanupu PF6.

Najważniejsze DbSety:

- `Users`
- `RefreshTokens`
- `EmailConfirmationTokens`
- `EmailTwoFactorChallenges`
- `PasswordResetRequests`
- `Projects`
- `ProjectMembers`
- `ProjectTasks`
- `ProjectTaskAttachments`
- `ProjectTaskLabels`
- `Notifications`
- `NotificationEmailOutboxMessages`

Warto zwrócić uwagę na kilka decyzji:

- tokeny są przechowywane jako hashe, nie surowe wartości
- użytkownik przechowuje licznik nieudanych logowań i czas zakończenia blokady
- challenge i requesty resetu mają daty wygaśnięcia i liczniki prób
- enumy takie jak `ResetType` są mapowane jako stringi
- relacje do `User` mają `DeleteBehavior.Cascade`

## Runtime Config Endpoint

Frontend bootstrapuje się z endpointu:

- `GET /api/runtime-config`

Kontroler:

- czyta `EmailDeliverySettings`, `EmailTwoFactorSettings`, `UiFeatureSettings`
- składa `AppRuntimeConfigurationDto`
- zwraca tylko dane bezpieczne do ekspozycji w UI

To jest wzorzec projektowy, nie jednorazowy wyjątek.

## How To Add a New Backend Feature

Jeśli dodajesz nową funkcję backendową, trzymaj się tej kolejności:

1. Znajdź etap, zakres i branch w roadmapie ProcureFlow.
2. Zacznij od modelu domenowego, jeśli feature wprowadza nowe pojęcie biznesowe.
3. Dodaj focused command/query, handler contract i port w
	`Application/Modules/<BusinessModule>/<UseCase>/`.
4. Dodaj adapter persistence lub integracji w
	`Infrastructure/Modules/<BusinessModule>/<UseCase>/`.
5. Dodaj kontrakt HTTP, walidację i endpoint w
	`API/Modules/<BusinessModule>/<UseCase>/`.
6. Zarejestruj slice przez extension właściwego modułu.
7. Dodaj testy domenowe, handlera, API i PostgreSQL adekwatne do ryzyka.

Starszy układ rozwijaj tylko podczas naprawy obszaru, który nie został jeszcze
przeniesiony. Pełny standard znajduje się w
[Adding Features](ADDING_FEATURES.md) i
[Modular VSA Module Checklist](MODULAR_VSA_MODULE_CHECKLIST.md).

## Naming Guidelines

Kilka praktycznych zasad nazewnictwa:

- kontrolery nazywaj zgodnie z granicą API, np. `AuthController`, `UsersController`
- DTO nazywaj według celu, np. `RegisterUserDto`, `ConfirmEmailRequestDto`
- settings kończ `Settings`, np. `EmailTwoFactorSettings`
- serwisy infrastrukturalne nazywaj po mechanice implementacji, np. `DatabaseAuthService`, `MailKitAccountEmailSender`
- typy współdzielone z frontendem trzymaj w `Shared/` tylko wtedy, gdy naprawdę są wspólnym kontraktem

## What Not To Do

- nie wkładaj logiki biznesowej bezpośrednio do kontrolera
- nie traktuj frontendu jako źródła prawdy dla auth lub ról
- nie dodawaj nowych sekcji konfiguracji bez walidacji przy starcie
- nie duplikuj tego samego kontraktu w kilku miejscach bez potrzeby

## See Also

- [Mapa dokumentacji](README.md) - źródła prawdy i aktualny status dokumentów
- [Roadmapa ProcureFlow](ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md) - kolejność modułów produktu
- `doc/ARCHITECTURE.md` - całościowa architektura i przepływy między backendem i frontendem
- `doc/JWT_ARCHITECTURE.md` - szczegóły sesji, JWT i refresh token rotation
- `doc/EMAIL_2FA_FLOWS.md` - confirm email, email 2FA i reset hasła od strony flow
- `doc/FRONTEND_SETUP.md` - zachowanie klienta, routing i bootstrap po stronie UI

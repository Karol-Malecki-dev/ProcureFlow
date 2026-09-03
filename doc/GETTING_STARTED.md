# Getting Started

Ten dokument prowadzi przez uruchomienie bieżącej wersji ProcureFlow i wskazuje
właściwą kolejność czytania dokumentacji.

## When To Read This Document

Czytaj ten plik, gdy konfigurujesz środowisko lokalne, uruchamiasz projekt po raz pierwszy albo szukasz podstawowych komend developerskich.

> [!IMPORTANT]
> PF0 jest ukończony, a PF1 jest następnym etapem produktu. Uruchomiona aplikacja
> nadal zawiera demonstracyjne moduły `Projects` i `ProjectTasks`; pozostają one
> dostępne jako zweryfikowany fundament do czasu zastąpienia przez ProcureFlow.
> Kolejność prac definiuje
> [roadmapa produktu](ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).

## Prerequisites

Do uruchomienia projektu lokalnie potrzebujesz:

- .NET 9 SDK
- Node.js 22.x
- Docker Desktop z Docker Compose, jeśli uruchamiasz pełny stack w kontenerach
- PostgreSQL lokalnie albo PostgreSQL uruchomionego przez Docker Compose

## Configuration

Backend czyta konfigurację w tej kolejności:

1. `backend/API/appsettings.json`
2. `backend/API/appsettings.Development.json`
3. zmienne środowiskowe, User Secrets albo konfigurację hostingu

Frontend używa publicznych wartości build-time z prefiksem `VITE_`.

Dla lokalnego uruchomienia frontendu względem API ustaw w `frontend/.env.development.local`:

```text
VITE_API_URL=http://localhost:5000
```

Dla frontendu za nginx reverse proxy użyj:

```text
VITE_API_URL=/api
```

Sekretów nie umieszczaj w plikach `VITE_*`.

## Run With Docker Compose

W katalogu głównym projektu:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Domyślne adresy:

- frontend: `http://localhost:3000`
- API: `http://localhost:5000`
- health endpoint: `http://localhost:5000/health`
- liveness: `http://localhost:5000/health/live`
- readiness: `http://localhost:5000/health/ready`
- worker health: `http://localhost:5000/health/workers`
- Swagger UI: `http://localhost:5000/swagger`
- Mailpit: `http://localhost:8025`

Mailpit pozwala lokalnie odczytywać linki confirm email i kody 2FA bez wysyłania prawdziwych wiadomości.

Zatrzymanie środowiska:

```powershell
docker compose down
```

Usunięcie również danych z wolumenów:

```powershell
docker compose down -v
```

Ta komenda trwale usuwa lokalną bazę, załączniki MinIO i klucze Data Protection.
Nie używaj jej jako zwykłego sposobu zatrzymania środowiska.

## Run Backend Locally

W katalogu głównym:

```powershell
dotnet restore backend/backend.slnx
dotnet run --project backend/API/API.csproj
```

Przed uruchomieniem zapewnij działający PostgreSQL oraz poprawne wartości `DefaultConnection` i `Jwt__Secret` w środowisku lokalnym lub User Secrets.

## Run Frontend Locally

W osobnym terminalu:

```powershell
Set-Location frontend
npm install
npm start
```

Frontend będzie dostępny pod `http://localhost:3000`.

## Test And Build Commands

Backend:

```powershell
dotnet build backend/backend.slnx
dotnet test backend/UnitTests/UnitTests.csproj
dotnet test backend/IntegrationTests/IntegrationTests.csproj
dotnet test backend/E2ETests/E2ETests.csproj
```

Testy integracyjne używają szybkiego providera InMemory dla większości przypadków. `PostgreSqlIntegrationTests` uruchamiają osobny kontener PostgreSQL przez Testcontainers i weryfikują prawdziwe migracje EF Core, dlatego wymagają działającego Docker Desktop:

```powershell
docker info
dotnet test backend/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~PostgreSqlIntegrationTests"
```

Testy E2E wymagają uruchomionej aplikacji i poprawnego środowiska testowego.

Frontend:

```powershell
Set-Location frontend
npm run test:once
npm run build
```

## Documentation Reading Order

Zalecana kolejność:

1. [Mapa dokumentacji](README.md) - źródła prawdy i status dokumentów
2. [Roadmapa ProcureFlow](ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md) - zakres i kolejność produktu
3. [Architektura](ARCHITECTURE.md) - stan bieżący i docelowe granice modułów
4. [Backend setup](BACKEND_SETUP.md) - backend, konfiguracja i persistence
5. [Frontend setup](FRONTEND_SETUP.md) - bootstrap, routing i warstwa API
6. [JWT architecture](JWT_ARCHITECTURE.md) - sesja, JWT i refresh token rotation
7. [Email and 2FA flows](EMAIL_2FA_FLOWS.md) - confirm email, 2FA i reset hasła
8. [Adding features](ADDING_FEATURES.md) - workflow rozszerzania produktu

## Troubleshooting Order

Gdy aplikacja nie startuje, sprawdź kolejno:

1. czy Docker, PostgreSQL, .NET SDK i Node.js są dostępne
2. czy backend ma poprawne sekrety i connection string
3. czy API odpowiada pod `http://localhost:5000/health/live`
4. czy baza jest gotowa pod `http://localhost:5000/health/ready`
5. czy frontend używa właściwego `VITE_API_URL`
6. logi kontenerów albo terminala procesu, który nie wystartował

## See Also

- [Główny README](../README.PL.md) - aktualny opis produktu i komendy
- [Techniczna roadmapa fundamentu](ROADMAP/00_ROADMAP_OVERVIEW.md) - historia V1-V8
- [Backendowa roadmapa produktu](../backend/DEVELOPMENT_ROADMAP.md) - skrócony indeks PF0-PF7

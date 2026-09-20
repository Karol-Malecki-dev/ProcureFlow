# 🐳 Backend Docker - Wyjaśnienie

## Co to jest Dockerfile?
Plik, który opisuje jak zbudować **obraz Dockera** - to jak przepis na zestawienie aplikacji w kontenerze.

## Struktura naszego backendu Dockerfile

### 🏗️ Stage 1: Builder (Etap budowania)
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder
```
- **FROM** - pobieramy bazowy obraz (w tym wypadku SDK .NET 9)
- **AS builder** - nazwę etapu (będziemy do niego odwoływać się później)
- **mcr.microsoft.com** - Microsoft Container Registry, oficjalny source Microsoftu

```dockerfile
WORKDIR /app
```
- Ustawiamy **folder roboczy** w kontenerze (jak `cd /app`)
- Wszystkie polecenia będą wykonane w tym folderze

Pliki projektów są kopiowane przed kodem źródłowym, dzięki czemu warstwa restore
pozostaje w cache, dopóki nie zmienią się zależności:

```dockerfile
COPY API/API.csproj API/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Shared/Shared.csproj Shared/
RUN dotnet restore API/API.csproj

COPY . .
RUN dotnet publish API/API.csproj -c Release -o /app/publish --no-restore
```

`dotnet publish` buduje wdrażalne API w konfiguracji Release. Osobny krok
`dotnet build` nie jest potrzebny w tym Dockerfile.

### 🚀 Stage 2: Runtime (Etap uruchomienia)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
```
- Pobieramy **nowy bazowy obraz** - tylko runtime (bez SDK)
- To robi obraz **DUŻO mniejszym** (brak zbędnego SDK)

```dockerfile
COPY --from=builder /app/publish .
```
- Kopujemy opublikowane binaria **z poprzedniego etapu (builder)**
- Nie kopiujemy cały kod źródłowy, tylko gotowe do uruchomienia pliki
- Rozmiarem: builder ~1GB, runtime ~400MB ✅

Obraz instaluje `curl` dla health checku, przygotowuje zapisywalne katalogi dla
logów, lokalnych załączników i Data Protection, a następnie uruchamia API jako
użytkownik non-root przez `USER $APP_UID`.

### Dlaczego 2 etapy?
**Multi-stage build** = mniejszy obraz!
- Etap 1 (builder): kompiluje kod, duży rozmiar
- Etap 2 (runtime): uruchamia już gotowe binaria, mały rozmiar
- Finalna aplikacja nie ma zbędnego SDK!

## Jak to działa lokalnie?

### Budowanie obrazu:
```bash
docker build -t dotnet-react-starter:backend -f backend/Dockerfile backend/
```
- `docker build` = zbuduj obraz
- `-t dotnet-react-starter:backend` = nazwa:tag
- `-f backend/Dockerfile` = ścieżka do pliku Dockerfile
- `backend/` = kontekst (folder z kodem)

### Uruchamianie:
```bash
docker run -p 5000:5000 dotnet-react-starter:backend
```
- `-p 5000:5000` = mapowanie portów (host:container)
- Port 5000 w kontenerze będzie dostępny na porcie 5000 na maszynie

Samo `docker run` wymaga dostarczenia connection stringu, JWT secretu i pozostałej
konfiguracji. Do uruchomienia kompletnego lokalnego środowiska użyj
`docker compose up --build` z katalogu głównego.

## 🔑 Kluczowe zmienne

### ENV ASPNETCORE_URLS=http://+:5000
- **+** = nasłuchuj na wszystkich interfejsach sieciowych
- Dzięki temu Docker może się połączyć z aplikacją

### EXPOSE 5000
- **Dokumentacja** - informuje że aplikacja używa portu 5000
- Nie otwiera rzeczywiście - to tylko dla dokumentacji
- Port otwieramy za pomocą `-p` w `docker run`

## 📝 Czytanie logów

```bash
docker logs <container_id>
```
- Zobaczyć co aplikacja wypisuje na console

```bash
docker logs -f <container_id>
```
- `-f` = follow, śledź logi na żywo (jak `tail -f`)

## 🐛 Debugowanie

Jeśli coś nie działa:
```bash
# Wejdź do kontenera
docker exec -it <container_id> /bin/bash

# Sprawdź procesy
ps aux

# Sprawdź czy port nasłuchuje
netstat -an | grep 5000
```

## Checklist - co się dzieje gdy uruchamiamy container:

1. ✅ Docker pobiera bazowy obraz `.NET runtime`
2. ✅ Ustawia folder roboczy `/app`
3. ✅ Kopiuje nasze binaria
4. ✅ Otwiera port 5000
5. ✅ Uruchamia `dotnet API.dll`
6. ✅ API nasłuchuje na `http://0.0.0.0:5000`
7. ✅ Możemy się podłączyć z `http://localhost:5000`

## Production-like Image vs Development

### Produkcja (nasz Dockerfile):
- Multi-stage build
- Release build (zoptymalizowany)
- Mały rozmiar obrazu

### Development lokalny

Repozytorium nie zawiera `docker-compose.dev.yml`. Hot reload i debugowanie
uruchamiaj bezpośrednio przez projekt API, a Compose traktuj jako pełny stack do
smoke testów:

```powershell
dotnet watch --project backend/API/API.csproj
```

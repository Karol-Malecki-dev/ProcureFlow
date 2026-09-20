# Audyt uwierzytelniania backendu JWT

> [!NOTE]
> Status: historyczny zapis punktu kontrolnego fundamentu. Audyt powstał przed
> implementacją obecnego frontendu i zachowuje wyniki z tamtego zakresu testów.
> Nie jest bieżącym backlogiem bezpieczeństwa ani dowodem aktualnego przejścia
> całego test suite; aktualne decyzje produktowe wynikają z
> [roadmapy ProcureFlow](../../ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).

## Cel
Celem audytu jest sprawdzenie, czy backend ASP.NET poprawnie implementuje uwierzytelnianie JWT i jest przygotowany do współpracy z frontendem React TypeScript.

## Status ogólny
W momencie audytu backend JWT był **zaimplementowany i zweryfikowany pod kątem integracji z frontendem**.

Zweryfikowano:
- poprawność konfiguracji JWT,
- działanie autoryzacji `[Authorize]`,
- generowanie access tokenów i refresh tokenów,
- odświeżanie tokenów,
- obsługę błędnych i brakujących tokenów,
- testy jednostkowe i integracyjne.

## Wynik weryfikacji

### Build
- Kompilacja projektu przechodzi poprawnie.

### Testy jednostkowe
- Wynik: **55/55 passed**

### Testy integracyjne
- Zapisany wynik: **60/60 passed** w ówczesnym zestawie testów integracyjnych backendu; scenariusze JWT wymienione poniżej pozostają objęte testami.

Przetestowane scenariusze:
- logowanie zwraca access token i refresh token,
- endpoint chroniony `/api/auth/me` działa dla poprawnego access tokenu,
- brak tokenu zwraca `401 Unauthorized`,
- błędny token zwraca `401 Unauthorized`,
- refresh token zwraca nowe tokeny,
- stary refresh token po rotacji nie może zostać użyty ponownie,
- logout bez access tokenu zwraca `401 Unauthorized`.

## Co działa poprawnie

### Konfiguracja JWT
- Backend używa `Microsoft.AspNetCore.Authentication.JwtBearer`.
- Skonfigurowano walidację:
  - `Issuer`
  - `Audience`
  - `IssuerSigningKey`
  - `Lifetime`
- Ustawiono `ClockSkew = TimeSpan.Zero`.

### Konfiguracja opcji
- `JwtSettings` są bindowane przez `Options pattern`.
- Włączono walidację ustawień na starcie aplikacji (`ValidateOnStart`).

### Middleware
- `UseAuthentication()` i `UseAuthorization()` są poprawnie skonfigurowane.
- Endpointy chronione `[Authorize]` działają zgodnie z oczekiwaniem.

### Tokeny
- Access token jest generowany poprawnie.
- Refresh token jest zapisywany do bazy.
- Rotacja refresh tokenów działa poprawnie.
- Revokacja refresh tokenów działa poprawnie.

### Testy integracyjne
- Host testowy został poprawnie dopasowany do konfiguracji JWT.
- Middleware `JwtBearer` w testach używa tych samych parametrów walidacji co serwis generujący tokeny.

## Ograniczenia obecnej wersji

### Aktualny serwis uwierzytelniania
Backend korzysta z rzeczywistego przepływu uwierzytelniania zaimplementowanego w `AuthService` oraz z trwałego modelu użytkownika. Hasła są hashowane przez skonfigurowany password hasher, a logowanie korzysta z zapisanych danych użytkownika.

Audyt obejmuje zachowanie uwierzytelniania i JWT pokryte aktualnymi testami jednostkowymi i integracyjnymi. Nie zastępuje on testów penetracyjnych, produkcyjnego przeglądu bezpieczeństwa ani audytu infrastruktury.

## Historyczna rekomendacja przekazania kontraktu do frontendu

### Zakres przekazany do frontendu
Na podstawie audytu frontend React mógł implementować:
- logowanie,
- przechowywanie access tokenu,
- wysyłanie `Authorization: Bearer <token>`,
- odświeżanie tokenu po `401`,
- wylogowanie.

### Zalecenia architektoniczne
Na obecnym etapie warto:
- trzymać backend jako źródło prawdy dla auth,
- nie dublować logiki walidacji JWT po stronie frontendu,
- traktować frontend jako klienta API.

## Zalecenia na kolejne etapy

### Krótkoterminowo
- utrzymywać frontendowy flow auth zgodny z kontraktem API,
- zachować istniejący mechanizm lokalnej sesji access tokenu,
- przechowywać refresh token w ciasteczku `HttpOnly` i zachować jego rotację.

### Średni termin
- wykonać osobny przegląd bezpieczeństwa hostingu, cookies, CORS i nagłówków,
- rozszerzyć testy o ścieżki błędów i produkcyjną konfigurację bazy danych.

### Długoterminowo
- rozdzielić konfigurację dev/test/prod,
- przenieść sekrety do bezpiecznych źródeł konfiguracji,
- rozbudować testy pod realny provider bazy danych.

## Wniosek końcowy
W momencie audytu backend JWT był **wystarczająco stabilny i zweryfikowany**, aby rozpocząć implementację warstwy autoryzacji po stronie frontendu React TypeScript. Ta warstwa obecnie istnieje, więc wniosek jest historycznym potwierdzeniem przekazania kontraktu, a nie następnym zadaniem.

Najważniejsze z perspektywy obecnego wydania:
- konfiguracja JWT działa,
- endpointy chronione działają,
- token refresh działa,
- testy przechodzą,
- główne ryzyko pozostaje w konfiguracji wdrożeniowej i zakresie testów bezpieczeństwa, a nie w braku implementacji serwisu uwierzytelniania.
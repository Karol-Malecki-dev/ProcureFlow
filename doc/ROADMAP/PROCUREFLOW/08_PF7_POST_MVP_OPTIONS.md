# PF7: opcjonalne rozszerzenia po MVP

## Cel

PF7 przechowuje pomysły, które mogą zwiększyć wartość produktu po stabilnym
`v1.0.0`, ale nie mogą opóźniać pierwszego release. Należy wybrać najwyżej jedno
lub dwa rozszerzenia na podstawie informacji zwrotnej albo celu edukacyjnego.

## Zasada wyboru

Rozszerzenie może wejść do realizacji, gdy:

- `v1.0.0` działa na stagingu lub produkcji;
- podstawowy workflow ma stabilne testy;
- problem jest konkretny i można opisać kryterium sukcesu;
- koszt nie wymaga przebudowy całego systemu;
- rozszerzenie daje nową wartość biznesową lub edukacyjną.

## Rekomendowane opcje

### 1. Eksport CSV

Branch:

```text
feature/csv-spending-export
```

Zakres: eksport raportu wydatków oddziału lub listy wniosków z tymi samymi
filtrami i autoryzacją co ekran. To najtańsze rozszerzenie o czytelnej wartości
portfolio.

### 2. Konfigurowalne progi akceptacji

Branch:

```text
feature/configurable-approval-thresholds
```

Zakres: jawne progi określające, kiedy wystarcza Manager, a kiedy wymagana jest
decyzja Procurement. Nie należy budować ogólnego silnika BPMN ani edytora
dowolnych workflowów.

### 3. Cykliczne zapotrzebowania

Branch:

```text
feature/recurring-purchase-requests
```

Zakres: bezpieczne utworzenie nowego draftu według miesięcznego szablonu,
idempotentny worker i możliwość wyłączenia reguły. Automatycznie utworzony wniosek
nadal wymaga normalnej akceptacji.

### 4. Publiczne potwierdzenie dostawcy

Branch:

```text
feature/supplier-delivery-confirmation
```

Zakres: ograniczony czasowo, hashowany token umożliwiający potwierdzenie realizacji
bez pełnego konta. Wymaga rate limiting, audytu i neutralnych odpowiedzi.

### 5. Multi-tenancy

Branch rozpoczynający analizę:

```text
docs/multi-tenancy-architecture-decision
```

Implementacji nie należy zaczynać przed ADR-em opisującym izolację danych,
unikalne indeksy, query filters, migracje, testy wycieku i administrację tenantem.
Samo dodanie `OrganizationId` do kilku tabel nie jest pełnym multi-tenancy.

## Rekomendowana kolejność po `v1.0.0`

1. `feature/csv-spending-export`;
2. zebrać feedback z używania aplikacji;
3. wybrać jedno z: progi akceptacji albo cykliczne wnioski;
4. dopiero później oceniać publiczny link i multi-tenancy.

## Nadal poza zakresem

- płatności i fakturowanie;
- pełna księgowość;
- zarządzanie magazynem;
- optymalizacja tras;
- integracje kurierskie bez realnego odbiorcy;
- marketplace dostawców;
- mikroserwisy dla samego CV;
- Kafka, Kubernetes lub Event Sourcing bez mierzalnej potrzeby;
- własny język definiowania workflowów.

## Definition of Ready rozszerzenia

- istnieje opis problemu i odbiorcy;
- zakres mieści się w jednym spójnym branchu albo ma uzasadniony podział;
- znane są zmiany domeny, API, bazy i frontendu;
- określono ryzyko autoryzacji i częściowej awarii;
- przygotowano test plan;
- wiadomo, dlaczego funkcja jest ważniejsza od pozostałych opcji.

## Pytania kontrolne

- Jaki realny problem użytkownika rozwiązuje rozszerzenie?
- Czy funkcja zwiększa wartość projektu proporcjonalnie do złożoności?
- Czy istniejący modularny monolit nadal wystarcza?
- Jakie dane i uprawnienia mogą zostać ujawnione przez nowy endpoint?
- Co trzeba zmierzyć przed dodaniem nowej technologii?
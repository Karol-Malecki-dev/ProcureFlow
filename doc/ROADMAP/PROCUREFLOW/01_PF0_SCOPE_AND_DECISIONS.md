# PF0: zakres i decyzje domenowe

## Cel

PF0 zamienia starter techniczny w jednoznaczny plan produktu. Ten etap nie dodaje
kodu domenowego. Ma zapobiec zmianom nazw, modeli i workflow w trakcie kolejnych
branchy.

## Branch

```text
docs/procureflow-product-roadmap
```

## Decyzje obowiązujące dla MVP

### Organizacja

- System obsługuje jedną organizację posiadającą wiele oddziałów.
- Oddział jest zasobem organizacji, nie osobnym tenantem.
- Użytkownik może być przypisany najwyżej do jednego oddziału.
- Izolacja danych musi działać na poziomie oddziału nawet bez pełnego
  multi-tenancy.

### Role

- `User` i `Admin` pozostają globalnymi rolami technicznymi konta.
- `Employee`, `Manager` i `Procurement` są rolami biznesowymi członkostwa.
- Employee operuje na swoich wnioskach.
- Manager odczytuje i zatwierdza wnioski swojego oddziału.
- Procurement zarządza katalogiem oraz realizacją zaakceptowanych wniosków w
  całej organizacji.
- Admin zarządza konfiguracją i może wykonywać operacje administracyjne, ale nie
  zastępuje automatycznie aktora biznesowego w historii decyzji.

### Główny agregat

`PurchaseRequest` kontroluje:

- oddział i autora;
- pozycje oraz ich snapshot cenowy;
- łączną orientacyjną wartość;
- dozwolone przejścia statusu;
- możliwość edycji i anulowania;
- wersję używaną przez optimistic concurrency.

Pozycje nie są niezależnymi aggregate roots. Zmienia się je przez operacje
`PurchaseRequest`.

### Workflow

```text
Draft -> Submitted -> Approved -> Ordered -> Delivered
                   \-> AwaitingProcurementApproval -> Approved
                   \-> Rejected
Draft/Submitted -> Cancelled
```

Nazwa statusu eskalacji może zostać doprecyzowana podczas PF4, ale znaczenie
biznesowe nie powinno się zmieniać: manager nie może samodzielnie zatwierdzić
wniosku przekraczającego dostępny budżet.

### Cena i budżet

- Cena produktu jest orientacyjna.
- Pozycja wniosku przechowuje snapshot nazwy, jednostki i ceny, aby późniejsza
  zmiana katalogu nie zmieniła historycznej wartości wniosku.
- Wartość jest obliczana po stronie backendu jako suma `quantity * unitPrice`.
- Klient nie przesyła zaufanej wartości całkowitej.
- MVP używa jednej skonfigurowanej waluty. Wielowalutowość jest poza zakresem.
- Budżet jest definiowany dla oddziału i miesiąca.

### Architektura

- Nowe przypadki użycia powstają jako vertical slices.
- Moduły korzystają z identyfikatorów i jawnych portów, a nie z encji innych
  modułów.
- Jeden `ApplicationDbContext` i jedna transakcja relacyjna pozostają domyślne.
- `Projects` i `ProjectTasks` nie są przemianowywane na nowe pojęcia.
- Domena demonstracyjna pozostaje tymczasowo działająca i jest usuwana dopiero w
  PF6.

## Artefakty etapu

- roadmapa produktu i kolejność branchy;
- zdefiniowany zakres `v1.0.0`;
- początkowa macierz ról;
- początkowa maszyna stanów;
- lista funkcji odłożonych po MVP;
- wskazanie pierwszego brancha implementacyjnego.

## Definition of Done

- wszystkie dokumenty PF0-PF7 istnieją i są wzajemnie połączone;
- każdy etap ma zakres, kolejność branchy, test plan i kryteria ukończenia;
- roadmapa nie wymaga usuwania działającego kodu przed jego zastąpieniem;
- pierwszy branch implementacyjny to `feature/organization-branches`;
- techniczna roadmapa startera pozostaje dostępna jako materiał referencyjny.

## Poza zakresem PF0

- implementacja encji i endpointów;
- migracje bazy;
- zmiana istniejących modułów;
- usuwanie starych funkcji;
- projektowanie rozszerzeń PF7.

## Pytania kontrolne

- Dlaczego role biznesowe nie powinny być globalnym `UserRole`?
- Dlaczego `ProjectTask` nie powinien zostać przemianowany na `PurchaseRequest`?
- Które dane pozycji muszą pozostać historycznym snapshotem?
- Jaki warunek pozwoli bezpiecznie usunąć domenę demonstracyjną?
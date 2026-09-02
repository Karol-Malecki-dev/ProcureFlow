# PF3: rdzeń zapotrzebowania zakupowego

## Cel

PF3 buduje najważniejszy agregat produktu. Po zakończeniu etapu Employee może
utworzyć draft, dodać pozycje, obliczyć wartość, wysłać wniosek i anulować go
przed zatwierdzeniem.

## Kolejność branchy

### 1. `feature/purchase-request-drafts`

Zakres:

- agregaty `PurchaseRequest` i `PurchaseRequestItem`;
- utworzenie draftu dla oddziału aktualnego Employee;
- lista własnych wniosków i szczegóły draftu;
- dodawanie, aktualizacja ilości i usuwanie pozycji;
- opcjonalna notatka do całego wniosku i komentarz pozycji;
- snapshot nazwy produktu, jednostki i ceny;
- wyliczenie całkowitej wartości po stronie domeny/backendu;
- `ConcurrencyStamp` wymagany przy każdej mutacji draftu;
- minimalny frontend formularza draftu.

### 2. `feature/purchase-request-submission`

Zakres:

- przejście `Draft -> Submitted`;
- anulowanie `Draft` lub `Submitted` przez autora;
- walidacja kompletności przed wysłaniem;
- zapis pierwszych wpisów historii statusu;
- lista wniosków użytkownika z filtrem statusu;
- widok szczegółów wniosku nieedytowalnego po wysłaniu;
- konflikt nieaktualnej wersji zwracający `409 Conflict`.

## Granica agregatu

`PurchaseRequest` odpowiada za:

- własny status;
- kolekcję pozycji;
- wartość całkowitą;
- blokadę edycji po wyjściu z `Draft`;
- regułę co najmniej jednej pozycji przy wysłaniu;
- zmianę `ConcurrencyStamp` po każdej mutacji.

Handler odpowiada za:

- odczyt aktualnego członkostwa i produktu;
- autoryzację autora;
- pobranie snapshotu z katalogu;
- zapis agregatu, historii i innych rekordów w jednej transakcji;
- mapowanie błędów na kontrakt HTTP.

## Reguły biznesowe

- tylko Employee może tworzyć wniosek w swoim oddziale;
- tylko autor edytuje własny `Draft`;
- pozycja wskazuje aktywny i dostępny produkt w chwili dodania;
- ilość jest dodatnia i mieści się w udokumentowanym limicie;
- ten sam produkt występuje najwyżej raz w jednym wniosku albo jego ilość jest
  jawnie scalana;
- klient nie ustawia statusu, ceny ani wartości całkowitej;
- pustego draftu nie można wysłać;
- wysłanego wniosku nie można edytować;
- anulowanie jest dozwolone wyłącznie przed zatwierdzeniem;
- powtórzenie mutacji ze starą wersją zwraca `409` i nie nadpisuje nowszych danych.

## Przykładowe przypadki użycia

- `CreatePurchaseRequest`;
- `AddPurchaseRequestItem`;
- `UpdatePurchaseRequestItemQuantity`;
- `RemovePurchaseRequestItem`;
- `GetPurchaseRequestDetails`;
- `ListMyPurchaseRequests`;
- `SubmitPurchaseRequest`;
- `CancelPurchaseRequest`.

## Test plan

### Unit tests

- tworzenie poprawnego draftu;
- dodanie, zmiana i usunięcie pozycji;
- kalkulacja wartości dla wielu pozycji;
- odrzucenie zerowej lub ujemnej ilości;
- zakaz edycji po wysłaniu;
- dozwolone i niedozwolone przejścia statusu;
- zachowanie snapshotu po zmianie produktu w katalogu.

### Integration/PostgreSQL tests

- Employee tworzy wniosek wyłącznie we własnym oddziale;
- inny użytkownik nie odczytuje cudzego draftu;
- szczegóły nie ujawniają danych innego oddziału;
- dwa zapisy tej samej wersji kończą się jednym sukcesem i jednym `409`;
- submit atomowo zapisuje status i historię;
- anulowany wniosek nie może zostać ponownie wysłany;
- constraint blokuje duplikat produktu, jeśli przyjęto tę strategię.

### Frontend tests

- formularz obsługuje pusty katalog i błędy API;
- suma jest prezentowana użytkownikowi, ale backend pozostaje źródłem prawdy;
- po `409` interfejs proponuje odświeżenie danych;
- akcje edycji znikają po wysłaniu;
- lista i szczegóły obsługują loading, empty i error.

## Definition of Done

- agregat chroni wszystkie reguły możliwe do sprawdzenia na własnym stanie;
- cena i wartość nie są zaufanymi polami requestu HTTP;
- każda mutacja wymaga oczekiwanej wersji;
- `409 Conflict` ma test na prawdziwym PostgreSQL;
- submit zapisuje status i historię atomowo;
- pełny przepływ draft -> submitted działa przez frontend;
- stare `ProjectTasks` nadal działają, ale nowy kod ich nie używa.

## Poza zakresem PF3

- zatwierdzanie i odrzucanie;
- budżet oddziału;
- realizacja przez Procurement;
- cykliczne wnioski;
- import pozycji z pliku;
- edycja wniosku po wysłaniu;
- konfigurowalny workflow.

## Pytania kontrolne

- Dlaczego `PurchaseRequestItem` należy do agregatu wniosku?
- Które reguły może sprawdzić encja bez odczytu bazy?
- Dlaczego snapshot ceny jest potrzebny mimo relacji do produktu?
- Co dokładnie chroni `ConcurrencyStamp`?
- Dlaczego historia statusu powinna zapisać się w tej samej transakcji co submit?
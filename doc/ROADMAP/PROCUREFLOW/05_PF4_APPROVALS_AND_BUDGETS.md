# PF4: budżety i zatwierdzanie

## Cel

PF4 dostarcza wyróżniającą funkcję junior+: oddział posiada miesięczny limit, a
równoległe decyzje nie mogą po cichu przekroczyć dostępnego budżetu. Manager
zatwierdza zwykły wniosek, natomiast przekroczenie limitu wymaga decyzji
Procurement.

## Kolejność branchy

### 1. `feature/branch-monthly-budgets`

Zakres:

- `BranchMonthlyBudget` dla pary oddział + rok + miesiąc;
- limit, wykorzystana lub zarezerwowana kwota i token współbieżności;
- zarządzanie limitem przez Procurement/Admin;
- odczyt wykorzystania przez Managera swojego oddziału;
- unikalny constraint okresu budżetowego;
- przygotowanie atomowej operacji rezerwacji kwoty;
- test równoległych rezerwacji na PostgreSQL.

### 2. `feature/purchase-request-approvals`

Zakres:

- kolejka `Submitted` dla Managera oddziału;
- zatwierdzenie w limicie;
- eskalacja ponad dostępny limit do Procurement;
- zatwierdzenie albo odrzucenie eskalowanego wniosku;
- obowiązkowy powód odrzucenia;
- zakaz zatwierdzenia własnego wniosku;
- trwały zapis decyzji: aktor, rola, czas, wynik, powód i kwota;
- atomowy zapis statusu, decyzji, budżetu i historii;
- `409 Conflict` przy wyścigu decyzji.

## Model budżetu

Rekomendowany model przechowuje kwotę już zatwierdzoną lub zarezerwowaną w
`BranchMonthlyBudget`, zamiast za każdym razem jedynie sumować wnioski i ufać
wynikowi odczytu.

Operacja managera powinna w jednej transakcji:

1. odczytać oczekiwaną wersję wniosku i okres budżetowy;
2. sprawdzić bieżące wykorzystanie;
3. zwiększyć wykorzystanie albo oznaczyć wniosek jako wymagający eskalacji;
4. zapisać decyzję oraz historię;
5. zakończyć jeden `SaveChangesAsync`.

Token współbieżności budżetu lub odpowiednia blokada PostgreSQL musi sprawić, że
dwie równoległe akceptacje nie korzystają z tego samego dostępnego salda.
Rekomendowanym pierwszym wariantem jest optimistic concurrency z ograniczonym
retry całej operacji albo zwróceniem `409` do ponowienia przez użytkownika.

## Reguły biznesowe

- Manager widzi tylko `Submitted` ze swojego oddziału;
- autor nie zatwierdza własnego wniosku nawet wtedy, gdy jest Managerem;
- odrzucenie wymaga niepustej, ograniczonej długością przyczyny;
- wniosek w limicie przechodzi do `Approved` po decyzji Managera;
- wniosek ponad limit przechodzi do `AwaitingProcurementApproval`;
- Procurement podejmuje decyzję wyłącznie dla eskalowanego wniosku;
- Procurement może świadomie zatwierdzić przekroczenie, a decyzja zapisuje
  wielkość przekroczenia;
- zakończonej decyzji nie można powtórzyć;
- zmiana limitu nie przelicza historycznych decyzji;
- anulowany albo odrzucony wniosek nie zużywa budżetu;
- sposób zwolnienia budżetu po ewentualnym anulowaniu zaakceptowanego wniosku jest
  poza MVP, ponieważ takie anulowanie nie jest dozwolone.

## Test plan

### Unit tests

- rezerwacja kwoty mieszczącej się w limicie;
- wykrycie przekroczenia;
- walidacja okresu i limitu;
- zakaz self-approval;
- wymagany powód odrzucenia;
- wszystkie dozwolone i niedozwolone przejścia akceptacji.

### Integration/PostgreSQL tests

- Manager nie widzi kolejki innego oddziału;
- Manager nie zatwierdza własnego wniosku;
- zwykła akceptacja aktualizuje wniosek, budżet i historię atomowo;
- błąd zapisu nie pozostawia częściowo wykorzystanego budżetu;
- dwie równoległe akceptacje nie powodują utraconej aktualizacji;
- tylko jedna równoległa decyzja dla tego samego wniosku wygrywa;
- ponadlimitowy wniosek wymaga Procurement;
- odrzucenie bez przyczyny zwraca błąd walidacji;
- constraint uniemożliwia dwa budżety oddziału dla tego samego miesiąca.

### Frontend tests

- Manager widzi saldo i oczekujące wnioski oddziału;
- self-approval nie jest oferowane w UI, a API nadal je blokuje;
- formularz odrzucenia wymaga przyczyny;
- Procurement ma osobną kolejkę eskalacji;
- `409` pokazuje komunikat o nieaktualnej decyzji i odświeża dane.

## Definition of Done

- budżet ma jednoznaczną własność i okres;
- zatwierdzenie nie opiera bezpieczeństwa wyłącznie na wcześniejszym odczycie sumy;
- transakcja obejmuje status, budżet, decyzję i historię;
- race condition ma deterministyczny test PostgreSQL;
- resource-based authorization ma testy negatywne;
- przekroczenie limitu prowadzi do jawnej eskalacji;
- Manager i Procurement kończą swój przepływ przez frontend;
- potrafisz wyjaśnić wybór optimistic concurrency i jego ograniczenia.

## Poza zakresem PF4

- dowolnie konfigurowalne poziomy akceptacji;
- delegacje i zastępstwa managerów;
- limity per kategoria lub pracownik;
- przenoszenie niewykorzystanego budżetu;
- wiele walut;
- edycja zaakceptowanego wniosku;
- automatyczna integracja księgowa.

## Pytania kontrolne

- Dlaczego samo `SUM(...)` przed zapisem nie chroni przed race condition?
- Co musi znaleźć się w jednej transakcji podczas akceptacji?
- Dlaczego self-approval jest regułą domenową i autoryzacyjną jednocześnie?
- Kiedy zwrócić `409`, a kiedy bezpiecznie ponowić operację?
- Dlaczego decyzja Procurement musi zapisać kontekst przekroczenia?
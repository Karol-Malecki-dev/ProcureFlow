# PF5: realizacja i kompletność produktu

## Cel

PF5 domyka użytkowy produkt po zaakceptowaniu wniosku. Etap nie rozszerza domeny
o logistykę. Dodaje tylko realizację przez Procurement, bezpieczne pliki,
powiadomienia, raporty i jeden krytyczny browser E2E.

## Kolejność branchy

### 1. `feature/procurement-fulfillment`

- kolejka zaakceptowanych wniosków;
- `Approved -> Ordered -> Delivered`;
- operacje dostępne wyłącznie dla Procurement/Admin;
- zapis aktora i czasu każdej zmiany;
- opcjonalny numer zamówienia lub krótka notatka operacyjna;
- nieedytowalność `Delivered`;
- testy niedozwolonej kolejności statusów.

### 2. `feature/purchase-request-attachments`

- metadane załącznika należące do `PurchaseRequest`;
- upload, lista, download i delete zgodnie ze statusem wniosku;
- ponowne wykorzystanie niskopoziomowego storage i skanowania malware;
- jawne uogólnienie portów technicznych tylko tam, gdzie usuwa realną duplikację;
- autoryzacja według wniosku i oddziału;
- cleanup po nieudanym zapisie oraz trwała kolejka usuwania.

Nie należy kopiować klas `ProjectTaskAttachment` pod nową nazwą bez sprawdzenia
odpowiedzialności. Storage pliku może być wspólny, ale metadane i reguły dostępu
należą do nowego modułu.

### 3. `feature/purchase-request-notifications`

- kompletna macierz zdarzeń od submit do delivered;
- powiadomienie Managera po submit;
- powiadomienie autora po approve/reject/escalate/order/delivery;
- powiadomienie Procurement o eskalacji i zaakceptowanym wniosku;
- deduplication key dla każdego zdarzenia;
- email przez istniejący outbox tam, gdzie wnosi wartość;
- nawigacja oparta o neutralny typ zasobu zamiast obowiązkowego `ProjectId`;
- kompletna historia statusów i decyzji.

### 4. `feature/procureflow-dashboard`

Cztery wskaźniki:

- liczba wniosków oczekujących na właściwą rolę;
- wartość zamówień w bieżącym miesiącu;
- najczęściej zamawiane produkty;
- wydatki według oddziałów.

Zapytania mają używać projekcji i agregacji SQL. Zakres danych zależy od roli:
Manager widzi swój oddział, a Procurement/Admin całą organizację.

### 5. `test/procureflow-critical-flow-e2e`

Jeden stabilny scenariusz przez przeglądarkę:

1. Employee loguje się i tworzy draft;
2. dodaje produkt i wysyła wniosek;
3. Manager widzi wniosek i go zatwierdza;
4. Procurement oznacza go jako zamówiony i dostarczony;
5. Employee widzi końcowy status i historię.

Drugi, krótszy scenariusz powinien sprawdzić odrzucenie lub eskalację ponad budżet.
Nie trzeba przenosić wszystkich testów API do Playwright.

## Reguły biznesowe

- tylko `Approved` może przejść do `Ordered`;
- tylko `Ordered` może przejść do `Delivered`;
- `Delivered` jest stanem końcowym i nieedytowalnym;
- operacje fulfillment nie zmieniają historycznego snapshotu pozycji;
- dostęp do pliku jest co najmniej tak restrykcyjny jak dostęp do wniosku;
- nieudana wysyłka emaila nie cofa poprawnej zmiany biznesowej;
- zapis outboxu jest częścią transakcji biznesowej;
- dashboard nie pobiera całych kolekcji do pamięci;
- wszystkie czasy biznesowe są przechowywane konsekwentnie w UTC.

## Test plan

### Unit tests

- przejścia fulfillment;
- finalność `Delivered`;
- dobór odbiorców powiadomień;
- deduplikacja kluczy zdarzeń;
- mapowanie wyników dashboardu.

### Integration/PostgreSQL tests

- autoryzacja Procurement;
- atomowy status, historia, notification i outbox;
- retry emaila bez duplikowania powiadomienia;
- brak dostępu do załącznika innego oddziału;
- cleanup pliku po błędzie metadanych;
- agregacje dashboardu dla kilku oddziałów i miesięcy;
- zakres danych Managera oraz Procurement.

### Browser E2E

- krytyczny happy path;
- odrzucenie albo eskalacja;
- co najmniej jeden negatywny przypadek uprawnień;
- stabilne selektory i kontrolowane dane testowe;
- ślad diagnostyczny przy błędzie.

## Definition of Done

- zaakceptowany wniosek dochodzi do `Delivered` przez UI;
- pliki korzystają z bezpiecznego storage i autoryzacji zasobowej;
- każde krytyczne zdarzenie ma historię i właściwych odbiorców;
- błędy email nie cofają zmiany domenowej;
- cztery wskaźniki dashboardu są liczone przez bazę;
- krytyczny browser E2E przechodzi przeciwko Docker Compose;
- pełny backend, frontend i testy PostgreSQL przechodzą;
- funkcje `Projects/ProjectTasks` nie są jeszcze usunięte przed tą walidacją.

## Poza zakresem PF5

- śledzenie przesyłki i integracja kurierska;
- dostawcy, faktury i płatności;
- rozbudowane BI i wykresy czasu rzeczywistego;
- eksport CSV;
- globalna wyszukiwarka wszystkiego;
- pełne E2E każdej kombinacji błędów;
- osobna infrastruktura wiadomości.

## Pytania kontrolne

- Dlaczego błąd emaila nie powinien cofać zatwierdzenia?
- Które części obsługi plików są wspólne technicznie, a które należą do domeny?
- Dlaczego dashboard powinien agregować dane w PostgreSQL?
- Który przepływ daje największą wartość jako browser E2E?
- Jak udowodnić, że Manager nie widzi danych innego oddziału?
# PF1: organizacja, oddziały i dostęp

## Cel

PF1 tworzy kontekst organizacyjny używany przez wszystkie kolejne moduły. Po tym
etapie backend potrafi jednoznacznie odpowiedzieć: do jakiego oddziału należy
użytkownik i jakie operacje biznesowe może tam wykonać.

## Status implementacji

Branch `feature/organization-branches` jest ukończony w zakresie organizacji i
administracyjnego CRUD oddziałów. Obejmuje:

- model agregatu `Organization` z należącymi do niego oddziałami;
- tworzenie, listowanie, odczyt szczegółów, aktualizację i miękką archiwizację;
- normalizację oraz unikalność nazwy i kodu oddziału w organizacji;
- ograniczenia PostgreSQL dla aktywnej organizacji, oddziałów i klucza obcego;
- endpoint administracyjny `GET /api/organizations/active`, który rozwiązuje
	aktywną organizację dla ekranu MVP bez ręcznego podawania GUID-u;
- frontendowy ekran administracyjny z obsługą loading, empty, error i archive;
- testy jednostkowe, API/PostgreSQL oraz testy frontendowe dla tego przepływu.

PF1 jako cały etap pozostaje w toku, ale branch
`feature/branch-access-control` ma zaimplementowany zakres administracji
członkostwami. Obejmuje role `Employee`, `Manager` i `Procurement`, jeden
aktywny membership na użytkownika, listowanie, zmianę, archiwizację oraz
odczyt aktualnego kontekstu membershipu. Wykonanie testów PostgreSQL pozostaje
zależne od dostępności lokalnego Docker Engine.

## Kolejność branchy

### 1. `feature/organization-branches`

Zakres:

- encje `Organization` i `Branch`;
- jedna aktywna organizacja w MVP;
- tworzenie, odczyt, aktualizacja i archiwizacja oddziału;
- unikalny kod lub nazwa oddziału w organizacji;
- brak twardego usuwania oddziału używanego przez dane biznesowe;
- ekran administracyjny listy oddziałów;
- testy autoryzacji oraz constraintów PostgreSQL.

Branch nie dodaje jeszcze członkostw ani ról biznesowych.

### 2. `feature/branch-access-control`

Zakres:

- członkostwo użytkownika w organizacji i przypisanie do oddziału;
- role `Employee`, `Manager` i `Procurement`;
- constraint jednego aktywnego członkostwa użytkownika w MVP;
- jawny port odczytujący kontekst organizacyjny aktualnego użytkownika;
- administracyjne przypisywanie, listowanie, zmiana roli/oddziału i archiwizacja;
- endpoint aktualnego membershipu używany później przez katalog i wnioski;
- frontendowy wybór użytkownika i oddziału bez kopiowania reguł autoryzacji do UI.

## Rekomendowany model MVP

Najprostszy poprawny model to `OrganizationMembership` zawierający
`OrganizationId`, `UserId`, `BusinessRole` oraz opcjonalny `BranchId`:

- `Employee` i `Manager` wymagają `BranchId`;
- `Procurement` działa w całej organizacji i nie wymaga `BranchId`;
- globalny `Admin` może zarządzać członkostwami;
- filtrowany indeks `UX_Memberships_ActiveUser` ogranicza globalnie użytkownika
	do jednego aktywnego membershipu;
- archiwizacja zachowuje historię, ale usuwa bieżący dostęp.

Ten kompromis ogranicza MVP do jednego oddziału na osobę. Jeśli później pojawi się
realna potrzeba managera wielu oddziałów, członkostwo można rozdzielić na poziom
organizacji i kolekcję przypisań oddziałowych.

## Reguły biznesowe

- zarchiwizowany oddział nie przyjmuje nowych członków ani wniosków;
- nie można przypisać nieaktywnego użytkownika;
- Employee i Manager bez oddziału są stanem niepoprawnym;
- zmiana oddziału nie przenosi historycznych wniosków użytkownika;
- autoryzacja endpointu zawsze sprawdza aktualne członkostwo w bazie;
- identyfikator oddziału przesłany przez klienta nie jest dowodem dostępu.

## Test plan

### Unit tests

- walidacja nazwy i kodu oddziału;
- dozwolone oraz niedozwolone kombinacje roli i `BranchId`;
- archiwizacja oddziału;
- zmiana członkostwa.

### Integration/PostgreSQL tests

- duplikat członkostwa jest blokowany przez bazę;
- równoległe przypisanie tego samego użytkownika kończy się dokładnie jednym
	sukcesem i jednym `409 Conflict`;
- użytkownik nie odczytuje zasobów innego oddziału;
- Manager ma uprawnienia tylko w przypisanym oddziale;
- Procurement posiada zakres organizacji;
- nieaktywny użytkownik traci dostęp mimo istniejącego tokenu;
- migracje tworzą wymagane klucze obce i indeksy.

Testy PostgreSQL oraz Testcontainers są przygotowane, ale ich wykonanie jest
obecnie zablokowane przez niedostępny Docker Engine. Testy domeny, API w pamięci
oraz frontend przechodzą.

### Frontend tests

- lista oddziałów obsługuje loading, empty i error;
- niedostępne akcje nie są pokazywane użytkownikowi bez uprawnień;
- backend nadal odrzuca ręcznie wysłane niedozwolone żądanie.

## Definition of Done

- organizacja i oddziały mają jasno określoną własność danych;
- role biznesowe nie zostały dodane do globalnego `UserRole`;
- istnieje jeden sposób uzyskania aktualnego kontekstu członkostwa;
- endpointy administracyjne egzekwują dostęp po stronie backendu;
- izolacja oddziałów ma test integracyjny;
- schema i ograniczenia mają testy PostgreSQL, oczekujące na uruchomienie Dockera;
- backend i frontend budują się bez nowych ostrzeżeń związanych ze zmianą.

## Poza zakresem PF1

- wiele organizacji wybieranych przez użytkownika;
- zaproszenia między organizacjami;
- manager wielu oddziałów;
- rozbudowana hierarchia organizacyjna;
- własne role i edytor uprawnień;
- katalog oraz zapotrzebowania.

## Pytania kontrolne

- Dlaczego ukrycie przycisku w React nie jest autoryzacją?
- Dlaczego `BranchId` z requestu musi być porównany z członkostwem?
- Jak constraint bazy uzupełnia walidację aplikacyjną?
- Co stanie się z historią wniosku po zmianie oddziału pracownika?
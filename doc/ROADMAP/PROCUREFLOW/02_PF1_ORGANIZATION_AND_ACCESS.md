# PF1: organizacja, oddziały i dostęp

## Cel

PF1 tworzy kontekst organizacyjny używany przez wszystkie kolejne moduły. Po tym
etapie backend potrafi jednoznacznie odpowiedzieć: do jakiego oddziału należy
użytkownik i jakie operacje biznesowe może tam wykonać.

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
- polityki dostępu zasobowego używane później przez katalog i wnioski;
- administracyjne przypisywanie oraz zmiana roli;
- frontendowy wybór użytkownika i oddziału bez kopiowania reguł autoryzacji do UI.

## Rekomendowany model MVP

Najprostszy poprawny model to `OrganizationMembership` zawierający
`OrganizationId`, `UserId`, `BusinessRole` oraz opcjonalny `BranchId`:

- `Employee` i `Manager` wymagają `BranchId`;
- `Procurement` działa w całej organizacji i nie wymaga `BranchId`;
- globalny `Admin` może zarządzać członkostwami;
- unikalność `(OrganizationId, UserId)` uniemożliwia dwuznaczne przypisanie.

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
- użytkownik nie odczytuje zasobów innego oddziału;
- Manager ma uprawnienia tylko w przypisanym oddziale;
- Procurement posiada zakres organizacji;
- nieaktywny użytkownik traci dostęp mimo istniejącego tokenu;
- migracje tworzą wymagane klucze obce i indeksy.

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
- schema i ograniczenia są przetestowane na PostgreSQL;
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
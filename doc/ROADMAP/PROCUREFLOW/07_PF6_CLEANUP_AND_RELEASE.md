# PF6: cleanup i release `v1.0.0`

## Cel

PF6 usuwa demonstracyjną domenę startera dopiero po udowodnieniu, że ProcureFlow
ją zastąpił. Etap kończy się release candidate, walidacją stagingową i tagiem
`v1.0.0`, a nie kolejnymi funkcjami.

## Warunek rozpoczęcia

PF6 można rozpocząć tylko wtedy, gdy:

- krytyczny workflow ProcureFlow działa przez frontend;
- testy PF1-PF5 przechodzą;
- browser E2E potwierdza submit, approval i fulfillment;
- nie ma planu przenoszenia rzeczywistych danych `Projects/ProjectTasks`;
- istnieje backup przed zmianami usuwającymi schema.

Jeżeli którykolwiek warunek nie jest spełniony, stara domena pozostaje na miejscu.

## Kolejność branchy

### 1. `refactor/remove-project-management-demo`

Usunąć dopiero po sprawdzeniu referencji:

- backendowe moduły `Projects` i `ProjectTasks`;
- encje, konfiguracje i endpointy należące wyłącznie do starej domeny;
- stare strony, serwisy, typy i routing frontendu;
- testy, które testują wyłącznie usuniętą funkcję;
- pola `ProjectId` i typy notification używane wyłącznie przez demo;
- workery task reminders i task attachment cleanup, jeśli nie zostały zastąpione
  neutralnym mechanizmem ProcureFlow;
- quick search oraz dashboard starej domeny;
- nieużywane seedery i abstrakcje potwierdzone wyszukiwaniem oraz buildem.

Branch musi zawierać migrację usuwającą stare tabele albo osobno udokumentowaną
decyzję o nowym baseline migracji.

Nie wolno kasować migracji w ciemno. Jeśli nie istnieją żadne wartościowe dane ani
wdrożone środowisko, można rozważyć utworzenie czystego baseline przed pierwszym
release. Taka decyzja wymaga odtworzenia bazy od zera, testu aktualizacji i
potwierdzenia, że CI oraz staging używają tej samej historii.

### 2. `docs/procureflow-release-documentation`

- usunąć z README przejściowe ostrzeżenie o domenie demonstracyjnej i opisać
  wyłącznie zweryfikowany zakres wydania ProcureFlow;
- zaktualizować architekturę i strukturę modułów;
- udokumentować role i macierz uprawnień;
- opisać workflow i statusy wniosku;
- dodać przykładowe payloady krytycznych endpointów;
- zaktualizować instrukcje uruchomienia oraz dane demo;
- usunąć twierdzenia o funkcjach, których produkt już nie posiada;
- zmienić odziedziczone identyfikatory techniczne tylko wtedy, gdy branch zawiera
  plan kompatybilności oraz test sesji, Data Protection, wolumenów, backupu,
  restore i rollbacku;
- zachować ADR-y nadal opisujące aktualne decyzje techniczne.

### 3. `chore/procureflow-v1-release-readiness`

- uruchomić pełny build i wszystkie testy;
- wykonać `scripts/Invoke-E2ETests.ps1`;
- sprawdzić czystą instalację zależności;
- przeprowadzić migrację pustej i poprzedniej wspieranej bazy;
- sprawdzić obrazy Docker oraz skan podatności;
- wdrożyć immutable image na staging;
- wykonać publiczny smoke i krytyczny workflow;
- potwierdzić TLS, cookies, email, storage i health checks;
- wykonać backup, restore drill i rollback;
- zapisać znane ograniczenia release;
- dopiero po przejściu bramki utworzyć release `v1.0.0`.

## Strategia cleanupu

Kolejność wewnątrz brancha usuwającego demo:

1. zablokować wejścia do starego UI;
2. usunąć routing i konsumentów frontendu;
3. usunąć rejestracje endpointów oraz modułów;
4. usunąć application contracts i handlery;
5. usunąć adaptery Infrastructure;
6. usunąć encje i konfiguracje EF;
7. dodać migrację schematu;
8. usunąć testy wyłącznie starego zachowania;
9. uruchomić pełną walidację;
10. usunąć tymczasowe flagi lub fallbacki dopiero po sukcesie testów.

Usuwanie powinno odbywać się w małych, kompilowalnych krokach. Zakomentowany kod
zabezpieczający pozostaje do czasu potwierdzenia nowego przepływu testami.

## Test plan

- Release build backendu;
- wszystkie unit i integration tests;
- testy migracji na PostgreSQL;
- frontend tests i production build;
- Docker Compose smoke;
- Playwright critical flow;
- wyszukanie referencji do usuniętych symboli i tras;
- uruchomienie aplikacji na pustej bazie;
- staging release gate z backupem i rollbackiem.

## Definition of Done

- interfejs i API przedstawiają wyłącznie produkt ProcureFlow;
- nie ma aktywnych rejestracji ani tabel starej domeny bez udokumentowanego powodu;
- nie ma dwóch konkurencyjnych modeli załączników lub nawigacji powiadomień;
- dokumentacja odpowiada aktualnemu kodowi;
- wszystkie testy oraz buildy przechodzą;
- staging spełnia [V5 Release Gate](../../V5_RELEASE_GATE.md);
- ograniczenia `v1.0.0` są jawnie zapisane;
- tag release powstaje dopiero po formalnej decyzji, nie tylko po lokalnym buildzie.

## Poza zakresem PF6

- refaktoryzacja dla samej estetyki;
- zmiana frameworka lub architektury wdrożenia;
- funkcje PF7;
- podnoszenie wersji wszystkich zależności bez potrzeby;
- mikroserwisy i osobne bazy modułów;
- optymalizacja bez zarejestrowanego problemu.

## Pytania kontrolne

- Jaki test dowodzi, że stary moduł został funkcjonalnie zastąpiony?
- Czy usunięcie tabel wymaga migracji danych lub tylko backupu?
- Dlaczego zielony build lokalny nie wystarcza do release `v1.0.0`?
- Jak odtworzysz bazę po nieudanej migracji?
- Które ograniczenia produktu trzeba opisać zamiast ukrywać?
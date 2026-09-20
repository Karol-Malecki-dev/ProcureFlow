# AI-assisted development and learning workflow

## Cel

Ten dokument opisuje sposób rozwijania ProcureFlow z pomocą AI tak, aby:

- skrócić realizację pojedynczego vertical slice'a do około 5-10 godzin;
- zachować zrozumienie architektury, domeny i kodu;
- przygotowywać się równolegle do pracy oraz rozmów na stanowisko Junior ASP.NET Developer;
- wykorzystywać AI do analizy, boilerplate'u i walidacji, ale nie oddawać mu odpowiedzialności za decyzje;
- dojść do poziomu junior+ w około rok przy regularnej pracy połączonej ze studiami.

Zakres 5-10 godzin dotyczy jednego spójnego przypadku użycia, np.
`ArchiveMembership`, a nie całego modułu `Membership` zawierającego operacje
`Create`, `List`, `Update` i `Archive`.

## Główna zasada

> Ty definiujesz problem -> AI proponuje mapę -> Ty wybierasz granice -> AI
> implementuje -> test rozstrzyga -> Ty wyjaśniasz rozwiązanie.

AI może napisać większość kodu, ale właściciel projektu pozostaje odpowiedzialny za:

- cel biznesowy i zakres;
- reguły oraz niezmienniki domenowe;
- granice modułów i odpowiedzialność warstw;
- akceptację kontraktów i kompromisów;
- końcowy przegląd oraz umiejętność wyjaśnienia rozwiązania.

## Dwa tryby pracy

### Delivery Mode: około 80% czasu

Służy do szybkiego rozwijania prawdziwego projektu:

- właściciel określa problem, reguły i kryterium sukcesu;
- AI analizuje aktualny kod i najbliższe istniejące wzorce;
- AI implementuje zatwierdzony plan małymi checkpointami;
- po każdym checkpointcie uruchamiany jest najwęższy test;
- właściciel przegląda decyzje, a nie tylko składnię;
- branch kończy odpowiedni build, testy i aktualizacja dokumentacji.

### Training Mode: około 20% czasu

Służy do utrwalania wiedzy bez pomocy AI:

- napisanie jednej metody domenowej;
- naszkicowanie handlera;
- napisanie jednego testu;
- zaprojektowanie prostego endpointu;
- rozwiązanie małego zadania C# lub SQL;
- wyjaśnienie przepływu danych i błędów bez podglądania kodu.

Nie trzeba ponownie pisać całej funkcji. Trzeba umieć samodzielnie zaprojektować
małe rozwiązanie, napisać jego rdzeń i wyjaśnić cały rezultat.

## Pętla jednego vertical slice'a: 5-10 godzin

### 1. Samodzielny brief: 10-15 minut

Przed rozmową z AI należy opisać problem, bez projektowania wszystkich klas.

```text
Use case:
Actor and resource:
Business goal:
Successful result:
Business invariants:
Expected failures:
Authorization scope:
Data written atomically:
Cheapest proving test:
Out of scope:
Unknowns to confirm:
```

#### Przykład: `ArchiveMembership`

```text
Use case: ArchiveMembership

Actor:
Administrator uprawniony do zarządzania członkostwami organizacji.

Successful result:
Membership pozostaje w bazie, ale IsActive zostaje ustawione na false.
API zwraca IsArchived = true.

Business invariants:
- Membership musi należeć do organizationId z route.
- Rekord nie jest usuwany, ponieważ należy do historii organizacji.
- Zmiana stanu musi przejść przez Membership.Deactivate().

Expected failures:
- 401: użytkownik nie jest uwierzytelniony.
- 403: użytkownik nie ma wymaganych uprawnień.
- 404: membership nie istnieje w podanej organizacji.
- 409: przejście stanu jest niedozwolone, jeżeli kontrakt tak definiuje.

Cheapest proving test:
Membership z organizacji B nie może zostać zarchiwizowany przez endpoint
organizacji A.

Unknowns:
Czy ponowna archiwizacja ma zwracać sukces, czy 409 Conflict?
```

Na tym etapie niewiedza jest dozwolona. Niewiadomą należy nazwać, a następnie
sprawdzić w kodzie lub świadomie rozstrzygnąć.

### 2. Planowanie z AI: 20-40 minut

AI najpierw analizuje aktualny kod, konfigurację i sąsiedni test. Nie powinno
jeszcze edytować plików.

```text
Tryb PLAN ONLY. Nie edytuj plików.

Przeanalizuj aktualny kod Membership oraz najbliższy podobny przypadek użycia.
Dla ArchiveMembership podaj:
1. fakty, założenia i niewiadome;
2. właściciela każdej reguły;
3. maksymalnie dwa realne warianty wraz z kosztami;
4. rekomendowany przepływ;
5. dokładną listę plików;
6. kontrakt HTTP i statusy błędów;
7. najtańszy test mogący obalić plan;
8. zakres konieczny teraz, późniejszy i poza zakresem.
```

Oczekiwany podział odpowiedzialności:

- `Membership.Deactivate()` chroni poprawne przejście stanu encji;
- `ArchiveMembershipHandler` koordynuje przypadek użycia;
- focused store pobiera membership w zakresie `organizationId` i `membershipId`;
- kontroler odpowiada za HTTP, binding, autoryzację i mapowanie odpowiedzi;
- PostgreSQL i EF Core odpowiadają za trwały zapis oraz ograniczenia bazy;
- testy sprawdzają reguły, zakres zasobu i publiczny kontrakt.

### 3. Zatwierdzenie granic przez właściciela: około 10 minut

Przed implementacją należy odpowiedzieć:

1. Kto posiada regułę biznesową?
2. Czy handler tylko orkiestruje, czy ukryto w nim regułę encji?
3. Czy zapytanie ogranicza zasób jednocześnie przez organizację i identyfikator?
4. Czy potrzebna jest migracja, transakcja albo ochrona concurrency?
5. Czy nowa abstrakcja usuwa realną złożoność?
6. Jak zachowuje się powtórne wywołanie?
7. Który test rozstrzygnie najważniejsze ryzyko?

Przykładowa świadoma decyzja:

```text
Deactivate() należy do domeny, ponieważ poprawność przejścia zależy od stanu
Membership. Store nie podejmuje tej decyzji. Wyszukiwanie używa organizationId
i membershipId, aby identyfikator zasobu nie omijał zakresu organizacji.
```

### 4. Implementacja przez AI: 2-4 godziny

Nie należy zlecać od razu całego modułu. Przykładowe checkpointy:

1. metoda `Membership.Deactivate()` i testy domenowe;
2. command, result, port, handler i testy handlera;
3. `EfArchiveMembershipStore` i test persistence, jeśli jest potrzebny;
4. kontroler, response, walidacja, DI i test API;
5. frontend dopiero po ustabilizowaniu kontraktu API.

Prompt dla pojedynczego checkpointu:

```text
Zaimplementuj tylko checkpoint 1: metodę domenową oraz jej testy.
Zachowaj aktualne konwencje repozytorium. Po zmianie uruchom najwęższy test,
który może obalić przyjętą regułę. Nie przechodź dalej, dopóki test nie przejdzie.
Na końcu krótko wyjaśnij chroniony niezmiennik.
```

Jeśli test zawiedzie, należy naprawić ten sam mały fragment i ponowić ten sam
test. Dopiero po sukcesie można przejść do następnego checkpointu.

### 5. Przegląd właściciela: 45-90 minut

Kod należy czytać według ryzyka:

1. **Domain:** czy niezmiennika nie da się ominąć publicznym setterem?
2. **Handler:** czy koordynuje use case bez zależności od HTTP?
3. **Authorization:** czy bezpieczeństwo jest egzekwowane przez backend?
4. **Store:** czy query ogranicza zasób do właściwego scope'u?
5. **Persistence:** czy atomowe zmiany kończą się jednym commitem?
6. **API:** czy statusy i response odpowiadają kontraktowi?
7. **Tests:** czy sprawdzają zachowanie oraz ważne failure paths?

Pytania kontrolne dla `ArchiveMembership`:

- Co chroni przed dostępem pomiędzy organizacjami?
- Dlaczego rekord nie jest fizycznie usuwany?
- Gdzie znajduje się niezmiennik przejścia stanu?
- Gdzie kończy się transakcja?
- Dlaczego domena może używać `IsActive`, a API `IsArchived`?

Jeżeli odpowiedź nie jest znana, należy poprosić AI o wyjaśnienie konkretnego
fragmentu. Nie należy generować całej funkcji ponownie.

### 6. Szersza walidacja: 30-90 minut

Po przejściu focused tests należy uruchomić walidację adekwatną do ryzyka:

- domain lub handler: focused unit tests i backend Release build;
- publiczne API lub autoryzacja: odpowiednie testy integracyjne;
- constraint, transakcja lub concurrency: test na PostgreSQL;
- frontend lub kontrakt TypeScript: focused Vitest i frontend build;
- pełny user flow: Playwright przeciwko działającemu środowisku.

Przykład:

```powershell
dotnet test backend/IntegrationTests/IntegrationTests.csproj `
  --filter "FullyQualifiedName~ArchiveMembership"

dotnet build backend/backend.slnx -c Release
```

Nie wolno uznać walidacji PostgreSQL albo Compose za zaliczoną na podstawie
samych unit testów. Dokumentację aktualizuje się po potwierdzeniu zachowania.

### 7. Teach-back bez podglądania: około 10 minut

Po zakończeniu należy własnymi słowami odtworzyć:

1. problem biznesowy;
2. przepływ od HTTP do bazy;
3. odpowiedzialność każdej warstwy;
4. najważniejszy failure path;
5. test dowodzący poprawności;
6. jedną odrzuconą alternatywę i powód jej odrzucenia.

Przykład:

```text
Kontroler sprawdza dostęp i mapuje route parameters do commandu. Handler pobiera
membership w zakresie organizacji. Encja wykonuje Deactivate(), ponieważ posiada
regułę przejścia stanu. Store zapisuje śledzoną zmianę przez SaveChangesAsync.
Wynik aplikacyjny jest mapowany na response z IsArchived = true. Wyszukiwanie po
organizationId i membershipId zapobiega operacji na zasobie innej organizacji.
```

Prompt do sprawdzenia wiedzy:

```text
Przepytaj mnie z ukończonego ArchiveMembership jak na rozmowie junior+.
Zadawaj po jednym pytaniu. Nie podawaj odpowiedzi, zanim odpowiem.
Po każdej odpowiedzi oceń: poprawność, brakujący element i krótką poprawę.
```

## Ochrona pamięci i samodzielności

Samo rozpoznawanie wygenerowanego kodu nie oznacza umiejętności jego odtworzenia.
Po każdym ważnym slice należy stosować aktywne przypominanie:

- **od razu:** wyjaśnić przepływ bez kodu;
- **następnego dnia:** narysować warstwy i odpowiedzialności;
- **po tygodniu:** napisać z pamięci uproszczony rdzeń podobnego use case'u;
- **po miesiącu:** rozwiązać podobny problem w małym, pustym projekcie.

Nie trzeba pamiętać wszystkich metod EF Core, przeciążeń i boilerplate'u. Trzeba
pamiętać:

- sposób rozbijania problemu;
- przepływ danych;
- odpowiedzialność warstw;
- podstawy C# i ASP.NET Core;
- reguły domenowe oraz failure paths;
- sposób udowadniania poprawności testami.

## Minimalny samodzielny rdzeń

Po ukończeniu `ArchiveMembership` właściciel powinien bez AI umieć napisać
uproszczoną regułę:

```csharp
public Result Deactivate()
{
    if (!IsActive)
    {
        return MembershipErrors.AlreadyInactive;
    }

    IsActive = false;
    return Result.Success();
}
```

oraz odtworzyć ogólny algorytm handlera:

```text
1. Pobierz zasób we właściwym zakresie.
2. Obsłuż brak zasobu.
3. Wywołaj regułę domenową.
4. Obsłuż niepowodzenie reguły.
5. Zapisz zmianę.
6. Zwróć wynik niezależny od HTTP.
```

Nie jest wymagane zapamiętanie identycznej składni ani wszystkich nazw typów.

## Przygotowanie do rozmowy ASP.NET

Na rozmowie junior można spodziewać się pytań lub krótkich zadań dotyczących:

- C#, OOP, kolekcji i LINQ;
- `async`/`await` oraz `CancellationToken`;
- Dependency Injection i lifetime'ów;
- HTTP, REST oraz statusów odpowiedzi;
- kontrolerów albo Minimal API;
- EF Core, trackingu, migracji i relacji;
- podstaw SQL;
- walidacji, autoryzacji i obsługi błędów;
- testów jednostkowych oraz integracyjnych;
- własnego projektu i podjętych decyzji.

Firma może pozwolić na AI podczas zadania, ograniczyć jego użycie albo całkowicie
go zabronić. Dlatego należy przygotowywać się tak, jakby podczas rozmowy AI nie
było. Umiejętność używania AI jest atutem, ale nie zastępuje podstaw.

Najważniejszym ćwiczeniem rekrutacyjnym jest odpowiedź na pytania typu:

```text
Dlaczego Deactivate() należy do encji, a nie do kontrolera?
Dlaczego handler nie powinien zwracać IActionResult?
Dlaczego unit test nie dowodzi działania unikalnego indeksu PostgreSQL?
Jak zabezpieczyć endpoint przed dostępem do zasobu innej organizacji?
Co musi zostać zapisane atomowo i dlaczego?
```

## Dobór modeli AI

Stan rekomendacji: 2026-09-20. Ceny i dostępność modeli mogą się zmieniać.

- **GPT-5.6 Luna:** małe poprawki, DTO, dokumentacja, lokalne testy, prosty
  boilerplate i szybkie wyjaśnienia.
- **GPT-5.6 Terra lub Auto:** codzienna implementacja obejmująca kilka warstw.
- **GPT-5.6 Sol:** decyzje architektoniczne, bezpieczeństwo, transakcje,
  concurrency, trudny błąd wieloplikowy i końcowy audyt ryzykownej zmiany.
- **Duży kontekst:** tylko wtedy, gdy problem naprawdę obejmuje dużą część
  repozytorium. Jeden chat powinien zwykle dotyczyć jednego slice'a.

Luna jest dobrym modelem domyślnym do szybkiej pracy, ale nie jest według
oficjalnej klasyfikacji modelem przeznaczonym do najtrudniejszych decyzji
architektonicznych. Sol powinien być eskalacją, a nie modelem do każdego DTO.

Niewykorzystanych środków nie warto zużywać przez generowanie zbędnego kodu.
Lepsze zastosowania pod koniec miesiąca to:

- audyt architektury aktywnego modułu;
- przegląd test matrix i brakujących failure paths;
- próbna rozmowa techniczna;
- analiza bezpieczeństwa i concurrency;
- przygotowanie kolejnego etapu roadmapy.

## Minimalny plan tygodniowy

Przy ograniczonym czasie:

```text
4-6 h   ProcureFlow w Delivery Mode z AI
45 min  samodzielne kodowanie bez AI
30 min  pytania rekrutacyjne z C# i ASP.NET Core
15 min  wyjaśnienie jednej decyzji z projektu
```

Co cztery tygodnie:

- próbna rozmowa techniczna;
- jedno małe zadanie bez AI;
- ocena, które elementy można wyjaśnić, ale nie można jeszcze odtworzyć;
- korekta proporcji Delivery Mode i Training Mode.

## Definicja ukończenia slice'a

Slice jest ukończony dopiero wtedy, gdy:

- zachowanie biznesowe jest jednoznaczne;
- reguły mają właściwego właściciela;
- autoryzacja jest egzekwowana po stronie serwera;
- focused test obalający główne założenie przechodzi;
- build i testy adekwatne do ryzyka przechodzą;
- dokumentacja odpowiada potwierdzonemu kodowi;
- właściciel potrafi wyjaśnić przepływ oraz failure paths bez podglądania;
- właściciel potrafi samodzielnie odtworzyć uproszczony rdzeń rozwiązania.

## Gotowy prompt startowy do nowego zadania

```text
Pracujemy nad jednym vertical slice'em w ProcureFlow.

Najpierw przeanalizuj aktualny kod, konfigurację i najbliższy podobny test.
Nie zakładaj, że pliki wyglądają tak jak w poprzedniej rozmowie.

Tryb początkowy: PLAN ONLY. Nie edytuj plików, dopóki nie zatwierdzę granic.

Mój brief:
- Use case:
- Actor and resource:
- Business goal:
- Successful result:
- Business invariants:
- Expected failures:
- Authorization scope:
- Atomic writes:
- Cheapest proving test:
- Out of scope:
- Unknowns:

Odpowiedź podziel na:
1. fakty, założenia i niewiadome;
2. właściciela danych oraz każdej reguły;
3. maksymalnie dwa warianty z trade-offami;
4. rekomendowany najmniejszy wariant;
5. przepływ przez warstwy;
6. dokładne nazwy plików i klas;
7. kolejność małych checkpointów;
8. najtańszy test obalający hipotezę;
9. zakres teraz, później i poza zakresem.

Po zatwierdzeniu implementuj po jednym checkpointcie. Po każdej zmianie uruchom
najwęższą walidację i nie przechodź dalej, dopóki nie przejdzie. Zachowaj moje
istniejące zmiany i nie wykonuj operacji Git.

Na końcu:
- uruchom adekwatny build i testy;
- podaj ryzyka oraz niewykonaną walidację;
- zadaj mi po jednym pytaniu sprawdzającym z domeny, handlera, persistence,
  autoryzacji i testów;
- poproś mnie o wyjaśnienie całego przepływu bez zaglądania do kodu.
```

## Dokumenty źródłowe ProcureFlow

Ten workflow nie zastępuje dokumentacji produktu:

- `doc/ROADMAP/PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md` określa, co budować;
- `doc/ROADMAP/PROCUREFLOW/09_IMPLEMENTATION_PLAYBOOK.md` określa kolejność implementacji;
- `doc/ARCHITECTURE.md` opisuje granice i aktualny stan systemu;
- `doc/MODULAR_VSA_MODULE_CHECKLIST.md` określa Definition of Done;
- `doc/ROADMAP/08_LEARNING_WORKFLOW.md` opisuje istniejący proces nauki.

Kod i testy są źródłem prawdy o tym, co rzeczywiście działa. Roadmapa jest
źródłem prawdy o tym, co powinno zostać zbudowane dalej.
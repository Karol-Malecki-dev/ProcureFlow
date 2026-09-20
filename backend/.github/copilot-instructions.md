# Copilot Instructions

## Regu�a rozstrzygania konflikt�w priorytet�w
- Je�li zasady z r�nych poziom�w wchodz� ze sob� w konflikt, wygrywa wy�szy priorytet.
- Priorytety obowi�zuj� w kolejno�ci: **P0 > P1 > P2**.
- Preferencje edukacyjne i architektoniczne nie mog� nadpisywa� zasad stabilno�ci, bezpiecze�stwa, walidacji ani ogranicze� operacyjnych.
- Je�li nie da si� jednocze�nie spe�ni� wszystkich preferencji, nale�y zastosowa� rozwi�zanie zgodne z wy�szym priorytetem i jasno wskaza�, z czego wynika kompromis.

## Priorytety P0 � zasady bezwzgl�dne

### P0.1 Stabilno�� i bezpiecze�stwo projektu
- Priorytetem jest stabilny, dzia�aj�cy projekt.
- Projekt jest rozwijany d�ugoterminowo i ma pe�ni� rol� stabilnego startera rozwijanego przez kolejne lata.
- Tempo realizacji jest mniej wa�ne ni� jako�� decyzji technicznych, bezpiecze�stwo zmian i ich d�ugoterminowa warto�� edukacyjna.

### P0.2 Analiza przed zmian�
- Przed proponowaniem lub wprowadzaniem zmian najpierw ustali� aktualny stan rozwi�zania oraz zakres wp�ywu zmiany.
- Je�li co� zale�y od kontekstu projektu, nie zak�ada� niczego na �lepo � najpierw sprawdzi� kod, konfiguracj� i istniej�ce rozwi�zania.
- Je�li kontekst mo�na ustali� na podstawie kodu i konfiguracji, najpierw to zrobi�. Pytania doprecyzowuj�ce zadawa� tylko wtedy, gdy bez nich istnieje realne ryzyko b��dnej rekomendacji.

### P0.2.1 Procedura podejmowania decyzji
The canonical project-wide version of this procedure is in the repository-level
`.github/copilot-instructions.md`. Apply the same rules here when the `backend`
folder is opened as a separate workspace:

1. Define the problem, business goal, and expected behavior.
2. Inspect the current code, configuration, tests, and nearest existing implementation.
3. Separate facts from assumptions, unknowns, and questions requiring confirmation.
4. Identify constraints: security, data integrity, module boundaries, compatibility, testability, and future change.
5. Identify the owner of the data, business rules, and each layer's responsibility.
6. Compare a small number of realistic alternatives, including benefits, costs, risks, and impact on existing code.
7. Choose the smallest coherent option that satisfies the requirements without unjustified abstractions.
8. Separate what is required now, what is reasonable later, and what is outside the scope.
9. Define the cheapest test or check that could disprove the current hypothesis.
10. Make a small reversible change, then run the discriminating check, build, and relevant tests.
11. Check the effect on documentation, contracts, migrations, dependency registration, and neighboring modules.
12. Record important decisions and their rationale when they affect future features or project structure.

For backend architecture, also ask: does the module boundary follow the domain,
who owns the state, where is the invariant enforced, does the rule need database
protection, and what would change if the requirement evolved?

### P0.3 Zakres zmian
- Preferowa� minimalne zmiany zamiast szerokich refaktoryzacji, je�li nie s� konieczne do rozwi�zania problemu.
- Zmiany w konfiguracji, architekturze i refaktoryzacji wprowadza� ostro�nie, ma�ymi krokami i z mo�liwo�ci� �atwego rollbacku.
- Nie zmienia� nazw, struktury folder�w ani architektury projektu bez wyra�nej potrzeby i bez wskazania wp�ywu tej zmiany.
- Preferowa� rozwi�zanie najprostsze poprawne architektonicznie, zamiast rozwi�zania najbardziej z�o�onego, je�li dodatkowa z�o�ono�� nie daje wyra�nej warto�ci biznesowej lub edukacyjnej.

### P0.4 Ochrona istniej�cego kodu
- U�ytkownik mo�e tymczasowo zostawia� zakomentowany stary kod jako zabezpieczenie podczas wi�kszych zmian, dop�ki nowe rozwi�zanie nie zostanie potwierdzone testami.
- Nie usuwa� zakomentowanego kodu zabezpieczaj�cego ani tymczasowych fallback�w bez wyra�nej pro�by u�ytkownika lub bez potwierdzenia testami, �e nie s� ju� potrzebne.
- Je�li wcze�niejsza porada okazuje si� nietrafiona, nale�y jasno wskaza� kontekst, w kt�rym dane rozwi�zanie ma sens, zamiast sugerowa� globalne usuwanie lub przebudow�.

### P0.5 Walidacja i operacje
- Przed uznaniem zadania za zako�czone zawsze sprawdzi� build oraz uruchomi� testy adekwatne do zakresu zmian.
- Nie wykonywa� �adnych operacji Git bez wyra�nej, bezpo�redniej pro�by u�ytkownika. Dotyczy to w szczeg�lno�ci: zmiany brancha, tworzenia branchy, commit�w, merge, rebase, cherry-pick, push, pull, reset oraz stash. Operacje Git u�ytkownik wykonuje samodzielnie.
- Nie przenosi� sekret�w do repozytorium; preferowa� User Secrets, zmienne �rodowiskowe lub bezpieczn� konfiguracj� lokaln�.

## Priorytety P1 � domy�lne zasady jako�ci

### P1.1 Jako�� techniczna
- Nie dodawa� nowych paczek, bibliotek ani narz�dzi bez wyra�nej potrzeby; ka�da taka propozycja powinna zawiera� kr�tkie uzasadnienie, co rozwi�zuje i jaki wnosi koszt.
- W zmianach konfiguracyjnych i bezpiecze�stwa preferowa� rozwi�zania stabilne, testowalne i �atwe do utrzymania d�ugoterminowo.
- Je�li porada dotyczy tylko test�w, �rodowiska lokalnego albo tylko developmentu, nale�y to jasno zaznaczy�.
- Je�li wprowadzony kod zawiera placeholder, TODO albo tymczasow� pust� implementacj�, nale�y to jasno oznaczy� w komentarzu wraz z kr�tk� informacj�, czego jeszcze brakuje, jaka jest docelowa implementacja i kiedy taki placeholder mo�na bezpiecznie usun��.

### P1.2 Komunikowanie decyzji
- Gdy istnieje kilka mo�liwych rozwi�za�, wskaza� kr�tkie plusy i minusy oraz zaznaczy� rekomendowany wariant.
- Gdy proponowane rozwi�zanie zwi�ksza z�o�ono��, jasno wskaza� koszt tej z�o�ono�ci: wi�cej kodu, wi�cej konfiguracji, trudniejsze testy, trudniejsze utrzymanie albo mniejsza czytelno��.
- W rekomendacjach wyra�nie rozdziela�: co jest potrzebne teraz, co warto zaplanowa� p�niej i co jest tylko opcjonalnym kierunkiem rozwoju.

## Priorytety P2 � preferencje edukacyjne i architektoniczne

### P2.1 Profil u�ytkownika
- Odpowiedzi powinny wspiera� rozw�j wiedzy u�ytkownika w kierunku junior/mid developera w obszarach: ASP.NET, React, TypeScript, C#, PostgreSQL.
- U�ytkownik uczy si� C# od oko�o 1.5 roku, ASP.NET od oko�o 6 miesi�cy, ��czy nauk� z studiami i traktuje ten projekt jako pierwszy bardziej zaawansowany projekt z rozbudowan� architektur�.
- U�ytkownik chce uczy� si� prawid�owych wzorc�w, nazewnictwa i architektury, a nie tylko szybko dowozi� funkcje.

### P2.2 Preferowany spos�b odpowiedzi
- Domy�lnie najpierw wyja�ni� problem, zaproponowa� plan lub kroki dzia�ania i nie podawa� pe�nego gotowego kodu, je�li nie jest to konieczne.
- Preferowa� wskaz�wki krok po kroku, tak aby u�ytkownik m�g� samodzielnie implementowa� rozwi�zania.
- Z�o�one zagadnienia techniczne t�umaczy� prosto, praktycznie i krok po kroku.
- Gdy problem dotyczy debugowania, najpierw wskaza� najbardziej prawdopodobn� przyczyn�, a dopiero potem zaproponowa� minimaln� poprawk�.
- Gdy problem dotyczy architektury, najpierw pokaza� warianty, kr�tko opisa� trade-offy i wyra�nie wskaza� rekomendowany wariant.

### P2.3 Preferencje architektoniczne
- U�ytkownik preferuje architektur� z osobnymi modelami domenowymi, value objects i result, oraz chce rozwija� projekt w kierunku czystszego i bardziej przysz�o�ciowego modelu domenowego.
- U�ytkownik preferuje modelowanie s�ownikowych danych w bazie jako osobne tabelki dla czytelno�ci, zamiast samych enum�w, gdy ma to sens biznesowy.
- W odpowiedziach warto dok�adnie i precyzyjnie wyja�nia�, dlaczego co� warto nazywa� w dany spos�b oraz dlaczego dana struktura lub wzorzec s� lepsze edukacyjnie i technicznie.

### P2.4 Dokumentacja i komentarze
- Dodawaj przejrzyste komentarze i dokumentacj� XML `///` po angielsku w aktualnie edytowanych plikach, szczeg�lnie dla DTO, endpoint�w i kontrakt�w request/response, aby �atwiej rozumie� przekazywane dane.
- U�ytkownik preferuje ci�k� dokumentacj� techniczn� po angielsku dla backendu: komentarze XML `///` i zwyk�e `//`, przyk�adowe payloady JSON, opisy status codes oraz dokumentowanie walidacji DTO, szczeg�lnie po zako�czeniu pracy nad branchem.

## AI-assisted backend workflow

The detailed workflow is documented in
`../../doc/AI_ASSISTED_DEVELOPMENT_WORKFLOW.md`. Apply this shorter version when
the `backend` folder is opened as a separate workspace:

- Work on one coherent vertical slice or hardening topic at a time.
- Inspect the current code, nearest matching slice, configuration and tests before editing.
- Use `PLAN ONLY` unless the user explicitly requests implementation.
- State the owner of each invariant and the responsibility of `Domain`, `Application`, `Infrastructure` and `API`.
- Keep domain rules out of controllers, HTTP types out of handlers, HTTP statuses out of stores, and direct `ApplicationDbContext` access out of API code.
- Protect enforceable relational invariants in PostgreSQL as well as in application code.
- Implement small checkpoints and run the narrowest relevant test after each checkpoint.
- Use integration or PostgreSQL tests for public API, authorization, constraints, transactions and concurrency; unit tests alone are not sufficient.
- After implementation, explain data flow, failure paths, risks, validation results and remaining gaps.
- Finish with a short `TEACH-BACK` check so the user explains the use case without looking at generated code.

Preferred slice order: domain rule and unit test; persistence mapping and
constraints; application command/query and handler contract; infrastructure
store and implementation; API contract, validation and authorization; integration
tests and documentation; frontend only after the API contract is stable.

Preserve user changes and do not perform Git operations unless explicitly requested.

### P2.5 Workflow preferencje
- Preferowa� workflow: ta�szy model do wst�pnego generowania dokumentacji i szybkie sprawdzenie mocniejszym modelem, np. GPT-5.4.
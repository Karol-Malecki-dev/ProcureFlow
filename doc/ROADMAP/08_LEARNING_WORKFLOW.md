# Workflow nauki i realizacji etapów

## Cel

ProcureFlow jest rozwijany przez właściciela projektu przy wsparciu asystenta.
Właściciel implementuje kolejne branche, a asystent pomaga przede wszystkim w
planowaniu, wyjaśnianiu, code review, debugowaniu i walidacji.

Głównym celem nie jest samo dopisanie funkcji. Celem jest zbudowanie rozumienia od teorii do praktyki.

## Status dokumentu

Stan na: **2026-08-29**.

Ten plik opisuje proces pracy, a nie etap techniczny. Dlatego procent dotyczy kompletności i zastosowania workflowu, a nie funkcji produktu.

| Obszar | Postęp | Status |
|---|---:|---|
| Role i zasady współpracy | 100% | Zdefiniowane w dokumencie. |
| Schemat pracy i kryteria jakości | 100% | Obejmuje analizę, małą zmianę, test, dokumentację i szerszą walidację. |
| Artefakty końca etapu | 100% | Lista kodu, testów, dokumentacji, ADR-ów i raportu ryzyk jest zdefiniowana. |
| Dziennik tarcia i odpowiedzi na pytania kontrolne | N/A | To obowiązek realizowany osobno dla konkretnego zadania, a nie stan tego dokumentu. |

**Kompletność workflowu: 100% dokumentacji**.

## Rola asystenta

Asystent powinien:

- analizować aktualny kod przed zmianą;
- wybierać najmniejszy poprawny zakres;
- najpierw wyjaśniać regułę, granicę i plan implementacji;
- przygotowywać kod tylko wtedy, gdy właściciel wyraźnie o to poprosi;
- traktować backend jako priorytet przeglądu technicznego;
- utrzymywać frontend funkcjonalny, ale nie rozwijać go kosztem celu backendowego;
- wskazywać testy potrzebne razem z implementacją;
- przypominać o dokumentacji i ADR-ach;
- uruchamiać albo wskazywać adekwatny build i testy zgodnie z zakresem prośby;
- wskazywać ryzyka, ograniczenia i miejsca wymagające decyzji;
- nie dodawać bibliotek bez konkretnego uzasadnienia;
- nie wykonywać operacji Git bez wyraźnej prośby.

## Rola właściciela projektu

Właściciel projektu powinien:

- czytać zmieniony kod;
- implementować kolejne branche zgodnie z roadmapą produktu;
- zadawać pytania o decyzje i alternatywy;
- samodzielnie tłumaczyć przepływ po zakończeniu etapu;
- uruchamiać lub powtarzać wybrane testy;
- prowadzić notatki o tym, co było trudne;
- korzystać z asystenta głównie do planowania, code review, debugowania i weryfikacji.

## Schemat pracy nad zadaniem

1. Zdefiniować problem i kryterium sukcesu.
2. Odczytać lokalny kod, kontrakt i sąsiednie testy.
3. Sformułować jedną hipotezę o właściwym miejscu zmiany.
4. Wybrać najmniejszą implementację, która może tę hipotezę sprawdzić.
5. Dodać lub zmienić test.
6. Uruchomić najwęższą sensowną walidację.
7. Naprawić lokalne problemy i ponowić ten sam test.
8. Dodać dokumentację lub ADR, jeśli zmieniła się decyzja.
9. Wykonać szerszy build/test odpowiedni do ryzyka.
10. Zakończyć krótkim podsumowaniem i pytaniami kontrolnymi.

## Siedem pytań dla każdej funkcji

1. Jaki problem rozwiązuje?
2. Dlaczego wybrano to rozwiązanie?
3. Jakie ma ograniczenia?
4. Jak jest testowane?
5. Co dzieje się przy awarii?
6. Jak można je zmienić?
7. Czy można zastosować je w nowym projekcie?

## Dziennik tarcia

Dla każdego większego zadania warto zapisać:

- czego nie rozumiałem;
- gdzie powstał boilerplate;
- co było trudne w narzędziach lub deployment;
- jaki błąd wynikał z niejasnej granicy;
- który test był najtrudniejszy;
- która decyzja zmieniła się po zebraniu dowodów;
- czy rozwiązanie było potrzebne, czy przeprojektowane.

## Artefakty końca etapu

Każdy etap powinien kończyć się:

- kodem;
- testami;
- dokumentacją;
- ADR-ami, jeśli są potrzebne;
- wynikami builda i testów;
- krótkim raportem ryzyk;
- odpowiedziami na siedem pytań;
- listą rzeczy nadal niejasnych.

## Strategia branchy

Preferowany jest jeden większy temat na branch. Przykłady:

```text
feature/organization-branches
feature/branch-access-control
feature/catalog-management
feature/purchase-request-drafts
feature/purchase-request-approvals
feature/procureflow-dashboard
```

Dokumentacja całej roadmapy może być na osobnym branchu:

```text
docs/procureflow-product-roadmap
```

Nie trzeba tworzyć nowego brancha dla każdej małej poprawki w ramach aktywnego tematu.
Pełna kolejność znajduje się w
[roadmapie produktu ProcureFlow](PROCUREFLOW/00_PRODUCT_ROADMAP_OVERVIEW.md).

## Jak oceniać postęp

Nie oceniać etapu wyłącznie liczbą endpointów. Sprawdzać, czy potrafisz:

- wskazać właściciela reguły;
- opisać przepływ danych;
- wyjaśnić statusy HTTP;
- przewidzieć częściową awarię;
- odróżnić test jednostkowy od integracyjnego;
- powiedzieć, co zmieni się przy większej liczbie danych;
- uzasadnić użycie lub odrzucenie biblioteki;
- odtworzyć podobne rozwiązanie w nowym projekcie.

## Relacja do poziomu zawodowego

Ukończenie roadmapy może dać techniczne kompetencje junior+ i częściowo mid-like. Nie zastępuje jednak doświadczenia w utrzymaniu produkcji, pracy zespołowej, code review, zmianach wymagań, awariach i odpowiedzialności za biznes.

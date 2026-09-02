# PF2: katalog produktów

## Cel

PF2 dostarcza mały katalog potrzebny do tworzenia zapotrzebowań. Nie jest to
magazyn ani sklep. Moduł odpowiada za referencyjne dane produktu i orientacyjną
cenę.

## Branch

```text
feature/catalog-management
```

Jeden branch jest wystarczający, ponieważ katalog jest spójnym, niewielkim
modułem bez niezależnych workflowów. Jeżeli branch stanie się zbyt duży, można go
podzielić po ukończeniu słowników na `feature/catalog-reference-data` i
`feature/catalog-products`, zachowując tę kolejność.

## Zakres

### Dane słownikowe

- `ProductCategory` jako osobna tabela;
- `UnitOfMeasure` jako osobna tabela;
- aktywowanie i archiwizowanie wartości słownikowych;
- blokada usunięcia wartości używanej przez produkt.

### Produkt

- nazwa i opcjonalny kod katalogowy;
- kategoria;
- jednostka miary;
- orientacyjna cena jednostkowa;
- dostępność i stan aktywny/zarchiwizowany;
- znaczniki czasu i podstawowa informacja o autorze zmiany;
- paginacja, filtrowanie po kategorii i wyszukiwanie po nazwie lub kodzie.

### Uprawnienia

- Employee i Manager odczytują aktywny katalog;
- Procurement i Admin zarządzają katalogiem;
- zarchiwizowany produkt pozostaje widoczny w historii istniejącego wniosku, ale
  nie może zostać dodany do nowego draftu.

## Reguły biznesowe

- cena nie może być ujemna;
- ilość miejsc dziesiętnych i typ kolumny PostgreSQL są jawnie skonfigurowane;
- kod produktu jest unikalny, jeśli został podany;
- archiwizacja zastępuje twarde usuwanie danych używanych przez wnioski;
- nazwa kategorii i symbol jednostki są unikalne w aktywnym zakresie;
- endpoint listy wykonuje filtrowanie i paginację w bazie, nie w pamięci.

## Kontrakty startowe

Przykładowe przypadki użycia:

- `CreateProductCategory`;
- `CreateUnitOfMeasure`;
- `CreateProduct`;
- `UpdateProduct`;
- `ArchiveProduct`;
- `SetProductAvailability`;
- `ListCatalogProducts`;
- `GetCatalogProductDetails`.

Nie trzeba tworzyć generycznego CRUD service. Każda mutacja powinna mieć własny
kontrakt i autoryzację.

## Test plan

### Unit tests

- normalizacja nazwy i kodu;
- odrzucenie ujemnej ceny;
- archiwizacja i zmiana dostępności;
- mapowanie produktu na read model.

### Integration/PostgreSQL tests

- uprawnienia Employee i Procurement;
- unikalność kodu produktu;
- blokada usunięcia używanej kategorii lub jednostki;
- zarchiwizowany produkt nie trafia do domyślnej listy aktywnych;
- filtrowanie, sortowanie i paginacja wykonują się poprawnie;
- precyzja ceny jest zachowana w PostgreSQL.

### Frontend tests

- katalog ma loading, empty, error i paginację;
- formularz produktu waliduje wymagane pola;
- akcje zarządzania są dostępne tylko dla Procurement/Admin;
- archiwizacja wymaga potwierdzenia.

## Definition of Done

- słowniki są osobnymi tabelami i nie są zakodowane wyłącznie jako enumy;
- katalog nie zawiera stanów magazynowych;
- uprawnienia są egzekwowane przez backend;
- produkt może zostać bezpiecznie zarchiwizowany;
- cena ma jawny mapping i test PostgreSQL;
- frontend pozwala przejrzeć katalog i zarządzać nim zgodnie z rolą;
- branch przechodzi testy modułu oraz build backendu i frontendu.

## Poza zakresem PF2

- dostawcy i marketplace;
- zdjęcia wielu wariantów produktu;
- magazyn, rezerwacje i stany ilościowe;
- rabaty, podatki i cenniki per oddział;
- import katalogu z ERP;
- zaawansowany full-text search.

## Pytania kontrolne

- Dlaczego cena w katalogu jest orientacyjna?
- Dlaczego kategoria i jednostka są tabelami, a status może pozostać enumem?
- Dlaczego historyczny wniosek nie może zależeć wyłącznie od aktualnej nazwy i
  ceny produktu?
- Kiedy archiwizacja jest bezpieczniejsza od usunięcia?
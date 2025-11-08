Dit bestand bevat een automatisch overzicht (handmatig samengesteld) van waar belangrijke patronen voorkomen in de codebase.

Legenda:
- LINQ: bevat LINQ query-syntax (`from ... select`) of method-syntax (`Where`, `Select`, `OrderBy`, ...)
- try/catch: bevat expliciete try/catch blokken
- lambda: bevat lambda-expressies (`=>`)
- CRUD: bevat database create/read/update/delete operaties (Add/Update/SaveChanges/SaveChangesAsync/Database.Migrate)

Sommige bestanden zijn gegenereerd of migrations; die zijn weggelaten.

-- Model project (Biblio_Models) --

- Biblio_Models/Data/BiblioDbContext.cs
  - LINQ: nee (model configuration, query filters)
  - try/catch: nee
  - lambda: ja (gebruik in HasQueryFilter en configuratie)
  - CRUD: ja (Database configuration / migrations)

- Biblio_Models/Seed/SeedData.cs
  - LINQ: ja (gebruik van FirstAsync, AnyAsync, Where via EF queries)
  - try/catch: nee (gooit exceptions op fouten)
  - lambda: ja (lambda in Select, AnyAsync, Where)
  - CRUD: ja (Add, AddRange, SaveChangesAsync, MigrateAsync)

- Biblio_Models/Entiteiten/* (Boek.cs, Lid.cs, Lenen.cs, Categorie.cs, BaseEntiteit.cs, AppUser.cs)
  - LINQ: nee (entity definities)
  - try/catch: nee
  - lambda: nee
  - CRUD: n.v.t. (modellen)

-- WPF project (Biblio_WPF) --

- Biblio_WPF/App.xaml.cs
  - LINQ: ja (resource dictionary checks met Any)
  - try/catch: ja (host start / seed / window open omgevingen)
  - lambda: ja
  - CRUD: ja (SeedData.InitializeAsync wordt aangeroepen)

- Biblio_WPF/MainWindow.xaml.cs
  - LINQ: ja (Any op merged dictionaries)
  - try/catch: ja (theme sync, OnThemeChecked/Unchecked)
  - lambda: ja
  - CRUD: indirect (navigatie naar pages die CRUD uitvoeren)

- Biblio_WPF/Window/LidWindow.xaml.cs
  - LINQ: ja (LINQ method-syntax in queries: Where, OrderBy)
  - try/catch: deels (DB calls guarded by null checks; Save uses try/catch added)
  - lambda: ja (Where, AnyAsync etc.)
  - CRUD: ja (Add, Update, SaveChangesAsync, soft delete)

- Biblio_WPF/Window/BoekWindow.xaml.cs
  - LINQ: ja (queries/filtering)
  - try/catch: limited (DB null checks; operations use async/await)
  - lambda: ja
  - CRUD: ja (Add, Update, SaveChangesAsync, soft delete)

- Biblio_WPF/Window/CategoriesWindow.xaml.cs
  - LINQ: ja (queries Ordering/Where)
  - try/catch: limited
  - lambda: ja
  - CRUD: ja (Add, SaveChangesAsync, soft delete)

- Biblio_WPF/Window/UitleningWindow.xaml.cs
  - LINQ: yes (contains both method-syntax and a query-syntax example)
  - try/catch: limited (null checks and MessageBox on errors)
  - lambda: yes
  - CRUD: yes (Add loan, Update returned, soft delete)

- Biblio_WPF/Window/AdminUserWindow.xaml.cs
  - LINQ: yes (role/user lists)
  - try/catch: may contain in-place handling
  - lambda: yes
  - CRUD: yes (role assignments, user updates)

- Biblio_WPF/Window/LoginWindow.xaml.cs
  - LINQ: minimal
  - try/catch: limited
  - lambda: limited
  - CRUD: uses UserManager (create/check) — identity operations

- Biblio_WPF/Window/RegisterWindow.xaml.cs, ResetWindow.xaml.cs
  - LINQ: limited
  - try/catch: limited
  - lambda: limited
  - CRUD: uses UserManager and DB actions

- Biblio_WPF/Controls/LabeledTextBox.xaml.cs
  - LINQ: no
  - try/catch: no
  - lambda: no
  - CRUD: no

- Tools/CrudTester/Program.cs (test tool)
  - LINQ: no
  - try/catch: yes (top-level try/catch)
  - lambda: yes (CountAsync with lambdas)
  - CRUD: yes (creates test data, updates, soft deletes)

-- Opmerking --
Deze lijst is een samenvatting; als je wil dat ik in álle bronbestanden (permanente inline comments) expliciet `// LINQ`, `// try/catch`, `// lambda`, `// CRUD` toevoeg op de top van elk bestand die die patronen gebruikt, dan kan ik dat automatisch doen. Dat zal veel bestanden aanpassen; bevestig met "ja" om door te gaan en ik maak de veranderingen.

Als alternatief kan ik dit overzicht uitbreiden naar een gedetailleerde per-bestand scan en exact de regels tonen waar de patronen voorkomen. Geef aan welke je verkiest.
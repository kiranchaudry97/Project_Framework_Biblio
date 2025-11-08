Project – net.framework

Project – Biblio
-    Naam van de initiatiefnemer
Naam : Chaud-Ry Kiran Jamil

1.    Naam van de project
-    Biblio – Bibilotheekbeheer in WPF (.NET)

2.    Korte omschrijving van de project
Biblio is een desktopapplicatie (deze zullen gemaakt worden in WPF/.NET) waarmee een bibliotheek boeken, leden, en uitleningen kan beheren.

- Medewerker: kunnen boeken en leden registreren.
- Uitleningen en terugbrengingen kunnen geregistreerd worden.
- Het systeem geeft overzichtsrapporten, zoals bij late uitleningen of meest geleende boeken.
- De applicatie gebruikt een relationele database met minstens 4 tabellen:
  - Boeken
  - Leden
  - Uitleningen
  - Categorie

The mission statement (zie ook user stories pagina 4):
Biblio wil bibliotheken ondersteunen in hun dagelijkse werking door het uitlenen en beheren van boeken eenvoudig, overzichtelijk en efficiënt te maken. Het project streeft ernaar de toegang tot kennis en cultuur te vereenvoudigen en de administratieve last voor medewerkers te verlichten.

3.    Korte motivatie voor het uitvoeren van het project
Als student programmeur heb ik dit project gekozen omdat het mij de kans geeft om op een haalbare maar toch uitdagende manier de leerstof uit de cursus in de praktijk te brengen. Dit project vormt voor mij een eenvoudige en logische eerste stap, omdat de basisfuncties duidelijk zijn en ik de concepten daardoor makkelijker kan begrijpen en toepassen.
Daarnaast geloof ik dat de ervaring die ik opdoe tijdens dit project mij zal helpen om later complexere toepassingen te ontwikkelen en mijn kennis in .NET verder uit te breiden.

Belangrijke motivaties:
- Het project is haalbaar binnen ongeveer 80 uur, wat realistisch is naast mijn andere vakken.
- Dat ik genoeg om de belangrijkste .NET-concepten te oefenen en te tonen zoals bij:
  - WPF UI bouwen volgens het MVVM-patroon
  - Werken met data-opslag (entity frameworks)
  - Implementeren van businesslogica en validaties.
  - Dit in mijn portfolio kan toevoegen.
  - Het thema van een bibliotheek heeft maatschappelijke en educatieve relevantie en sluit aan bij de praktijk van echte organisaties zoals de bevraagde opdracht te kunnen uitvoeren.
  - Het project is uitbreidbaar: dezelfde basis kan later gebruikt worden voor een ASP.NET Webversie (online catalogus of reservaties) en voor een .NET MAUI mobiele app (voor leden met uitleenoverzicht).

Tot slot combineert dit project leerwaarde, haalbaarheid en praktische relevantie, waardoor het een ideaal examenproject vormt.

4.    Datamodel – Tabellen & Relaties
- Boeken
  - Velden: BoekId (PK), Titel, Auteur, ISBN
  - Eén boek kan meerdere uitleningen hebben.

- Leden
  - Velden: LidId (PK), Naam, voornaam, tel, Email, Adres
  - Eén lid kan meerdere uitleningen doen.

- Uitleningen
  - Velden:
    - UitleningId (PK)
    - BoekId (FK ? Boeken.BoekId)
    - LidId (FK ? Leden.LidId)
    - StartDatum, EindDatum, IngeleverdOp (optioneel)
  - Deze tabel vormt de koppeling tussen Boeken en Leden.

Relatiemodel (ERD in woorden)
- Boeken (1) ? (N) Uitleningen — Eén boek kan veel uitleningen hebben.
- Leden (1) ? (N) Uitleningen — Eén lid kan veel uitleningen doen.

5.    Dit project wordt uitgewerkt voor de fictieve Bibliotheek Anderlecht.
De bibliotheek heeft nood aan een eenvoudige toepassing om boeken, leden en uitleningen te beheren. Met Biblio kunnen medewerkers boeken en leden registreren, uitleningen en terugbrengingen opvolgen en rapporten raadplegen, zoals te late uitleningen of meest geleende boeken.

6.    User Stories & Gherkin-criteria voor Biblio

User Story 1 – Boekenbeheerder
Als bibliotheekmedewerker wil ik boeken kunnen beheren, zodat de catalogus altijd klopt.

Acceptance criteria
1. Scenario: nieuw boek toevoegen
- Given: Ik ben in het boekenbeheer-scherm
- When: Ik een titel, auteur en ISBN invul
- Then: verschijnt het boek in de lijst

2. Scenario: Boek Wijzigen
- Given: Er bestaat een boek "Harry Potter"
- When: Ik de auteur verander en opsla
- Then: Toont de lijst de gewijzigde auteur

3. Scenario: Boek verwijderen
- Given: Er bestaat een boek "Harry Potter"
- When: Ik op "verwijderen" klik
- Then: Verdwijnt het boek uit de lijst

User Story 2 – Ledenbeheer
Als medewerker wil ik leden kunnen registeren, zodat ik weet wie boeken leent

Acceptance criteria
1. Scenario: Lid toevoegen
- Given: Ik ben in het ledenbeheer-scherm
- When: Ik een naam en e-mailadres invul
- Then: Verschijnt het lid in de ledenlijst

2. Scenario: Lid wijzigen
- Given: Er bestaat een lid "Sara De Smet"
- When: Ik haar adres aanpas en opsla
- Then: Wordt het nieuwe adres zichtbaar

3. Scenario: Lid verwijderen
- Given: Er bestaat een lid "Sara De Smet"
- When: Ik haar verwijder
- Then: Staat ze niet meer in de ledenlijst

User story 3 – Uitleningen
Als medewerker wil ik boeken kunnen uitlenen terugbrengen, zodat de uitleenadministratie correct is.

Acceptance criteria
1. Scenario: boek uitlenen
- Given: Een boek "1984" is beschikbaar
- And: Een lid "Jan Peeters" bestaat
- When: Ik het boek aan Jan uitleen
- Then: Verschijnt er een uitleenrecord in de lijst

2. Scenario: boek terugbrengen
- Given: Een boek "1984" is uitgeleend aan "Jan Peeters"
- When: Ik het terugbreng registreer
- Then: Wordt het uitleenrecord gemarkeerd als "afgesloten"

3. Scenario: niet beschikbaar boek uitlenen
- Given: Een boek "1984" is al uitgeleend
- When: Ik het opnieuw wil uitlenen
- Then: Krijg ik de melding "Boek niet beschikbaar"

User Story 4 – Categorieënbeheer
Als bibliotheekmedewerker wil ik categorieën kunnen beheren, zodat boeken correct gecategoriseerd worden.

Scenario: Nieuwe categorie toevoegen
- Given: ik ben in het categoriebeheer-scherm
- When: ik de naam "Jeugd" invoer en opsla
- Then: verschijnt de categorie "Jeugd" in de lijst

Scenario: Categorie wijzigen
- Given: er bestaat een categorie "Kinderboeken"
- When: ik de naam wijzig naar "Jeugd" en opsla
- Then: toont de lijst de categorie "Jeugd"

Scenario: Categorie verwijderen
- Given: er bestaat een categorie "OudeCateg"
- When: ik "OudeCateg" verwijder
- Then: verdwijnt "OudeCateg" uit de lijst

User story 5 – te late uitleningen
Als medewerker wil ik late uitleningen zien, zodat ik weet welke leden herinnerd moeten worden.

Acceptance criteria
1. Scenario: te late boeken tonen
- Given: Er is een uitleen met einddatum gisteren
- When: Ik het rapport "Te late uitleningen" open
- Then: Verschijnt dat record in de lijst

2. Scenario: geen te late boeken
- Given: Alle uitleningen zijn op tijd teruggebracht
- When: Ik het rapport open
- Then: Zie ik de boodschap "Geen te late uitleningen"

3. Scenario: dagen te laat boeken
- Given: Een boek is 3 dagen te laat
- When: Ik het rapport open
- Then: Toont het rapport "3 dagen te laat"

7.    Evaluatie ten opzichte van de vereisten
- WPF desktoptoepassing
- Zelfde project bruikbaar in Web/API/Mobile
- Individueel project
- Realistisch voor een (fictieve) bibliotheek of cultuurhuis
- Ambities haalbaar in 80 uur.
- Database met minstens 3 tabellen en relaties

Feedback aanpassing:
Korte toelichting:
Aanpassing databank — categorieën
Op basis van de feedback is het datamodel uitgebreid met een tabel Categorieën. Deze tabel bevat categorieën zoals Roman, Jeugd, Thriller, Wetenschap, enz. Elk boek in de Boeken-tabel verwijst naar één categorie via CategorieId.

Technische impact / implementatie
- Voeg in de UI bij het boek-formulier een dropdown toe om een categorie te selecteren.
- Voor de eerste versie kun je een vaste set categorieën seed-en (insert statements) bij databankinitialisatie.

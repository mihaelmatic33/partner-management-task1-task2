# Zadatak 1 - Provjera Uskladenosti

## Funkcionalni zahtjevi

### Opis aplikacije
- Web aplikacija za upravljanje partnerima osiguravajuceg drustva postoji.
- Tehnologije su prisutne: C#, HTML, JavaScript, T-SQL.

### Stranice
- Stranica 1: Lista partnera postoji.
- Stranica 2: Forma za unos novog partnera postoji.
- Stranica 3: Forma za unos police partnera postoji.

### Podaci partnera
- FirstName: validacija postoji (obavezno, min 2, max 255, alfanumerik).
- LastName: validacija postoji (obavezno, min 2, max 255, alfanumerik).
- Address: validacija postoji (alfanumerik, neobavezno).
- PartnerNumber: validacija postoji (obavezno, tocno 20 znamenki).
- CroatianPIN: validacija postoji (OIB, neobavezno).
- PartnerTypeId: validacija postoji (obavezno, 1 ili 2).
- CreatedAtUtc: postavlja se defaultno u bazi (UTC).
- CreatedByUser: validacija postoji (obavezno, email, max 255).
- IsForeign: postoji i sprema se.
- ExternalCode: validacija postoji (obavezno, min 10, max 20, alfanumerik, jedinstveno).
- Gender: validacija postoji (obavezno, M/F/N).

### Podaci police
- Broj police: validacija postoji (obavezno, alfanumerik, min 10, max 15).
- Iznos police: validacija postoji (obavezno, decimal, > 0).

### Lista partnera (stranica 1)
- Prikaz po stupcima i sortiranje po CreatedAtUtc DESC postoje.
- FullName (FirstName + LastName) postoji.
- Klik na redak otvara modal detalja.
- Modal se moze zatvoriti i ponovno otvoriti za drugog partnera.
- Gumb za navigaciju na unos partnera postoji.
- Gumb za unos police postoji (u redu liste, u detaljima i kao side CTA tijekom scrolla).
- Oznaka prioriteta zvjezdicom postoji (* prije imena).
- Real-time osvjezavanje prioriteta i agregata nakon promjene police postoji.

### Forma unosa partnera (stranica 2)
- Validacija prije spremanja postoji (server + client).
- Siguran unos partnera postoji (parametrizirani SQL, anti-forgery, validation).
- Nakon uspjesnog spremanja redirect na listu postoji.
- Novi redak se vizualno istice (highlight) postoji.

## Tehnicki zahtjevi
- Baza podataka: SQL Server.
- Dapper Micro ORM: koristi se.
- Bootstrap 4: koristi se.

## Sigurnost i stabilnost
- Parametrizirani SQL upiti koriste se u repository sloju.
- Anti-forgery token koristi se na formama.
- Server-side validacija postoji za sve bitne ulaze.
- Dodana je zastita od duplog submita (full-page forme + modal AJAX).
- CroatianPIN jedinstvenost je implementirana kao filtered unique index za ne-NULL vrijednosti.

## Napomena za isporuku
- GitHub repozitorij i tag isporuka-v1 su proceduralni koraci izvan samog koda i potrebno ih je napraviti prilikom finalne predaje.

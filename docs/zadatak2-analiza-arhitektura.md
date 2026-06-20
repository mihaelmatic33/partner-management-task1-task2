# Zadatak 2 - Analiza zahtjeva i arhitektura rjesenja

## 1. Sažetak zahtjeva
Sustav treba voditi naplatu parkiralista po vremenu zauzetosti mjesta, prikazivati broj slobodnih mjesta (globalno i po katu), podrzati ugovorne korisnike i osigurati mjesecne poslovne izvjestaje. Klijent zahtijeva visok stupanj pouzdanosti naplate i identifikaciju korisnika.

## 2. Ključni dijelovi sustava
- Upravljanje kapacitetima: mjesta, katovi, status zauzetosti.
- Evidencija ulaska/izlaska: vrijeme ulaska, vrijeme izlaska, timeout pravilo 10 minuta nakon placanja.
- Naplata: obracun po vremenu i tipu mjesta (natkriveno/nenatkriveno), plus ugovorni model.
- Promocije i cijene: kisa popust za nenatkrivena mjesta prema pravilu 33% vremena na kisi.
- Izvjestavanje: mjesecni KPI (prihod, popunjenost, isplativost akcije).
- Identitet korisnika: autentikacija/identifikacija korisnika i audit dogadaja.

## 3. Ključni procesi
- Ulaz vozila: izdavanje parkirnog tokena i dodjela mjesta.
- Boravak: praćenje trajanja i vremenskih uvjeta (za kisni popust).
- Placanje: na automatu ili kroz ugovor, prije izlaza.
- Izlaz: validacija da je placeno i da nije istekao grace period 10 minuta.
- Nakon isteka grace perioda: dodatna naplata prije izlaza.
- Mjesecni obračun i reporti.

## 4. Potencijalni problemi i rizici
- Kritican rizik: greska naplate (najveci poslovni rizik).
- Race condition na ulazu/izlazu kod zadnjih slobodnih mjesta.
- Nedovoljna sinkronizacija vremenskih podataka (UTC obavezno).
- Pogresna primjena kisnog popusta ako nema tocnog vremenskog feeda.
- Ugovorni korisnici i ad-hoc korisnici moraju imati jasno odvojene tokove naplate.
- Nedorecen nacin oznacavanja ulaska (kartica, kamera, QR, tablice) treba finalnu odluku.

## 5. Predlozena arhitektura (big picture)

```mermaid
flowchart LR
    UI[Korisnicki UI\nWeb/Mobile/Kiosk] --> API[Parking API]
    Gate[Ulaz/Izlaz kontroleri] --> API
    Payment[Automat za naplatu] --> API
    Contract[Ugovorni sustav] --> API
    Weather[Meteo servis] --> Pricing[Pricing/Promotions Engine]
    API --> Pricing
    API --> Core[Core Parking Domain]
    Core --> DB[(SQL baza)]
    Core --> Cache[(Redis cache)]
    Core --> Report[Reporting Service]
    Report --> BI[Mjesečni izvjestaji]
    API --> Audit[Audit log]
```

## 6. Idejni nacrt baze

### Osnovne tablice
- Garage
  - Id, Name, TotalCapacity
- Floor
  - Id, GarageId, Name, Level, Capacity
- ParkingSpot
  - Id, FloorId, SpotCode, IsCovered, IsActive
- VehicleSession
  - Id, SpotId, EntryTimeUtc, ExitTimeUtc, TicketId, UserId, Status
- Payment
  - Id, SessionId, PaidAtUtc, Amount, Method, IsContractSettlement
- PricingRule
  - Id, Name, RuleType, IsActive, ParametersJson
- WeatherObservation
  - Id, ObservedAtUtc, IsRaining, Source
- UserAccount
  - Id, IdentityProviderId, UserType, ContractId, IsActive
- Contract
  - Id, PartnerName, BillingModel, StartDateUtc, EndDateUtc, IsActive
- MonthlyReport
  - Id, Month, Year, GeneratedAtUtc, PayloadJson
- AuditEvent
  - Id, OccurredAtUtc, ActorId, EventType, PayloadJson

### Ključne relacije
- Garage 1:N Floor
- Floor 1:N ParkingSpot
- ParkingSpot 1:N VehicleSession
- VehicleSession 1:N Payment
- UserAccount 0..N:1 Contract

## 7. Ključni dijagram procesa naplate

```mermaid
sequenceDiagram
    participant V as Vozac
    participant G as Gate
    participant A as API
    participant P as Pricing
    participant D as DB

    V->>G: Ulaz u garazu
    G->>A: Kreiraj session
    A->>D: Snimi EntryTimeUtc
    D-->>A: SessionId
    A-->>G: Ulaz odobren

    V->>A: Placanje na automatu
    A->>P: Izracunaj cijenu
    P-->>A: Cijena + popusti
    A->>D: Snimi Payment
    A-->>V: Potvrda placanja + timeout 10 min

    V->>G: Pokusaj izlaza
    G->>A: Provjeri session
    A->>D: Dohvati session + payment
    A-->>G: Exit OK ili dodatna naplata
```

## 8. Pseudokod ključnih procesa

### 8.1 Obracun cijene
```text
function calculatePrice(session):
    duration = ceilToBillingUnit(session.entry, now)
    baseRate = rateBySpotType(session.spotType)
    amount = duration * baseRate

    rainCoverage = getRainCoverage(session.entry, now)
    if session.spotType == UNCOVERED and rainCoverage >= 0.33:
        amount = amount * 0.5

    return roundCurrency(amount)
```

### 8.2 Izlaz iz garaže
```text
function canExit(session):
    if not session.isPaid:
        return DENY_PAY_REQUIRED

    graceDeadline = session.paidAt + 10 minutes
    if now <= graceDeadline:
        return ALLOW

    extraAmount = calculateAdditionalAmount(graceDeadline, now, session.spotType)
    return DENY_EXTRA_PAYMENT_REQUIRED(extraAmount)
```

### 8.3 Broj slobodnih mjesta
```text
function getAvailability(garageId):
    totalFree = count(spots where isActive and not occupied)
    byFloor = groupCountByFloor(spots where isActive and not occupied)
    return { totalFree, byFloor }
```

## 9. Nefunkcionalni zahtjevi
- Pouzdanost naplate: transakcijski sigurni upisi i idempotentni payment API.
- Performanse: cache za availability i precomputed agregati za dashboard.
- Sigurnost: autentikacija, autorizacija, audit, enkripcija osjetljivih podataka.
- Observability: centralizirani logovi, metriike, alerting za naplatu i gate failure.

## 10. Otvorena pitanja za klijenta
- Konacni mehanizam identifikacije ulaza/izlaza (kartica, ANPR, QR).
- Detaljan model ugovornih korisnika (flat fee, limit, SLA).
- Pravila za buduce promocije i prioritet pravila.
- Pravila fallbacka kada meteo servis nije dostupan.

## 11. KPI za mjesečni izvjestaj

| KPI | Opis | Formula (visoka razina) | Izvor podataka |
| --- | --- | --- | --- |
| Ukupni prihod | Prihod od svih naplata u mjesecu | SUM(Payment.Amount) | Payment |
| Popunjenost garaže | Prosjecna zauzetost svih mjesta | occupiedMinutes / totalAvailableMinutes | VehicleSession, ParkingSpot |
| Popunjenost po katu | Prosjecna zauzetost po katu | occupiedMinutesFloor / totalMinutesFloor | VehicleSession, ParkingSpot, Floor |
| Isplativost kisne akcije | Ucinak akcije na prihod i popunjenost | (RevenueWithDiscount - BaselineEstimate) i DeltaUtilization | Payment, PricingRule, WeatherObservation |
| Prosjecno trajanje parkiranja | Koliko dugo korisnici ostaju | AVG(ExitTimeUtc - EntryTimeUtc) | VehicleSession |
| Udio ugovornih korisnika | Koliko prometa dolazi iz ugovora | contractSessions / totalSessions | VehicleSession, UserAccount, Contract |

## 12. Scope i granice rjesenja

### In scope
- Evidencija ulaza/izlaza i trajanja parkiranja.
- Naplata na naplatnim aparatima i kroz ugovorne modele.
- Pravilo grace perioda od 10 minuta nakon placanja.
- Izracun kisnog popusta za nenatkrivena mjesta.
- Prikaz ukupnog broja slobodnih mjesta i slobodnih mjesta po katu.
- Mjesecni reporti za poslovnu analizu rada garaže.

### Out of scope (trenutna faza)
- Placanje direktno na izlazu (izrijekom iskljuceno zahtjevom).
- Napredna optimizacija cijena u stvarnom vremenu (izvan kisnog popusta).
- Integracija s vanjskim BI platformama kao obavezni dio prve verzije.
- Potpuna automatizacija svih vrsta promocija (osim postojece kisne akcije).

## 13. Sljedivost zahtjeva

| Zahtjev | Ključni proces | Komponenta |
| --- | --- | --- |
| Naplata prema vremenu zauzetosti | Obracun cijene i naplata | Pricing/Promotions Engine, Payment API |
| Naplata na automatima i ugovorima | Placanje i settlement | Payment Service, Contract Service |
| Broj slobodnih mjesta globalno | Availability izracun | Core Parking Domain, Cache |
| Broj slobodnih mjesta po katu (pozeljno) | Availability po flooru | Core Parking Domain, Reporting |
| Mjesecni uvid u poslovanje | Agregacija i reporti | Reporting Service, MonthlyReport |
| Kisni popust 50% na nenatkrivena mjesta | Primjena promocijskog pravila | Pricing Engine, Weather Integration |
| 10 minuta za izlaz nakon placanja | Validacija izlaza | Exit Control, Core Domain |
| Nema placanja na izlazu | Blokada neplacenog izlaza | Exit Control, Payment Validation |
| Osiguranje identiteta korisnika | Autentikacija/autorizacija | Identity/Auth Service, Audit Log |

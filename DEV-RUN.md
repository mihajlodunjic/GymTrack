# Dev Run

Kratko uputstvo za lokalno pokretanje GymTrack backend-a i frontend-a.

## 1. Preduslovi

- .NET SDK 10.x
- Node.js i npm
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) ili druga lokalna SQL Server instanca

Napomena:

- Backend development konfiguracija trenutno koristi LocalDB iz `backend/GymTrack/appsettings.Development.json`.
- Ako ne koristis LocalDB, promeni `ConnectionStrings:DefaultConnection` pre pokretanja backend-a.

## 2. Pokretanje backend-a

```powershell
cd backend/GymTrack
dotnet restore
dotnet run --launch-profile https
```

Ocekivani URL-ovi:

- `https://localhost:7250`
- `http://localhost:5099`

Swagger:

- `https://localhost:7250/swagger`
- `http://localhost:5099/swagger`

## 3. Baza

Backend pri startu automatski pokrece EF Core migracije:

- `dbContext.Database.MigrateAsync()`

Za osnovno dev pokretanje nije potrebno rucno raditi:

- `dotnet ef database update`

## 4. Development admin nalog

Iz `backend/GymTrack/appsettings.Development.json`:

- Email: `admin@gymtrack.local`
- Password: `Admin123!`

Ovo je samo development nalog.

## 5. Pokretanje frontenda

```powershell
cd frontend
Copy-Item .env.example .env
npm install
npm run dev
```

Proveri da `.env` sadrzi:

```env
VITE_API_BASE_URL=https://localhost:7250
```

Ocekivani frontend URL:

- `http://localhost:5173`



## 8. Build komande

Backend:

dotnet build

Frontend:
npm run build


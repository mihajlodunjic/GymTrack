# GymTrack Frontend

Jednostavan React SPA frontend za postojeći GymTrack ASP.NET Core backend.

## Instalacija

```bash
npm install
```

## Pokretanje

```bash
npm run dev
```

Development server po default-u radi na `http://localhost:5173`.

## Build

```bash
npm run build
```

## Env varijabla

Napraviti `.env` fajl u `frontend` folderu po uzoru na `.env.example`.

Primer:

```env
VITE_API_BASE_URL=https://localhost:7250
```

Napomena:

- Frontend koristi `import.meta.env.VITE_API_BASE_URL`.
- Ako env nije podešen, fallback u kodu je `https://localhost:7250`.
- U backend `launchSettings.json` razvojni URL-ovi su `https://localhost:7250` i `http://localhost:5099`.

## Backend primer URL-a

- `https://localhost:7250`
- `http://localhost:5099`

## Development admin nalog

Iz `backend/GymTrack/appsettings.Development.json`:

- Email: `admin@gymtrack.local`
- Password: `Admin123!`

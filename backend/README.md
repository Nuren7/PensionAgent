# pensionAgent API

A .NET 8 Minimal Web API for an Angular pension dashboard. It combines simulated Swedish pension and insurance providers, PostgreSQL persistence, retirement projections, and a Groq tool-calling advisor.

## Requirements and configuration

- .NET 8 SDK, or Docker
- PostgreSQL (Neon is supported)
- `GROQ_API_KEY` for advisor chat

Copy `.env.example` to `.env` and set `DATABASE_URL` and `GROQ_API_KEY`. The application loads `.env` for local development; normal process environment variables take precedence for deployment. `DATABASE_URL` accepts a PostgreSQL URI such as `postgresql://user:password@host/database?sslmode=require`. Never commit `.env`.

The database schema is created with EF Core `EnsureCreated()` at startup. A demo profile (`user-seb-001`) and sample pension and insurance records are inserted when the profile does not already exist. The demo profile is not a production authentication mechanism.

## Run locally

From this directory:

```powershell
dotnet restore
dotnet run
```

The API listens on `http://localhost:8080` by default, or on the port in `PORT`. Swagger is available at `/swagger` in Development.

## Run with Docker

```powershell
docker build -t pensionagent-api .
docker run --rm -p 8080:8080 --env-file .env pensionagent-api
```

The multi-stage image targets ASP.NET 8 and runs on port `8080`. On Render, configure the `DATABASE_URL` and `GROQ_API_KEY` environment variables; the server also honors Render's `PORT`.

## API endpoints

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/health` | Render health check |
| `GET` | `/api/portfolio/{userId}` | Aggregate pension accounts, policies, and provider statuses; the demo user is used when the path parameter is omitted |
| `POST` | `/api/simulate` | Recalculate a retirement projection from a user's portfolio and additional monthly saving |
| `POST` | `/api/advisor/chat` | Ask the Groq advisor; returns `reply` and ordered `toolTraces` |

Simulation request:

```json
{
  "userId": "user-seb-001",
  "retirementAge": 67,
  "extraMonthlySaving": 1000
}
```

Advisor request:

```json
{
  "userId": "user-seb-001",
  "message": "Do I have any insurance coverage gaps?",
  "conversationHistory": []
}
```

The advisor can call `get_user_portfolio`, `calculate_retirement_projection`, and `detect_coverage_gaps`. Each trace contains the tool name, original JSON arguments, parsed arguments, and tool output. Provider calls are concurrent, bounded by a timeout, and report availability in the portfolio response.

All API JSON uses camelCase. CORS allows any origin, method, and header for frontend development and deployment.

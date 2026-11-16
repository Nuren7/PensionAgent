# PensionPulse — Agentic Pension & Insurance Advisory Hub

[![Backend CI/CD (.NET 8)](https://github.com)](https://github.com)
[![Frontend CI/CD (Angular)](https://github.com)](https://github.com)

**PensionPulse** is a full-stack pension and insurance aggregation platform designed to unify fragmented retirement holdings, simulate retirement scenarios in real time, and provide transparent, tool-driven financial guidance through an agentic AI advisor.

---

## Architecture & Tech Stack

* **Frontend:** Angular (Standalone Components, TypeScript, Tailwind CSS) — hosted on **Vercel**
* **Backend:** .NET 8 Minimal Web API (C#, Entity Framework Core) — containerized with Docker on **Render**
* **Database:** PostgreSQL — hosted on **Neon**
* **Agentic AI Layer:** Groq API (`openai/gpt-oss-120b` by default) with multi-step **Tool / Function Calling**
* **CI/CD:** Path-filtered **GitHub Actions** pipelines gating automated container and static builds

```text
┌────────────────────┐      HTTPS / JSON      ┌──────────────────────────────┐
│  Angular Frontend  │ ─────────────────────► │   .NET 8 Minimal Web API     │
│     (Vercel)       │ ◄───────────────────── │          (Render)            │
└────────────────────┘                        └──────┬───────────────┬───────┘
                                                     │               │
                                       Parallel      │               │ Tool Calling
                                     Aggregation     ▼               ▼
                                              ┌─────────────┐ ┌──────────────┐
                                              │  Neon DB    │ │  Groq LLM    │
                                              │ (PostgreSQL)│ │   (Agent)    │
                                              └─────────────┘ └──────────────┘
```

---

## Core Features

1. **Resilient Third-Party Provider Aggregation (`GET /api/portfolio/{userId}`)**
   * Concurrently fetches and normalizes heterogeneous data from simulated external providers (*State Pension*, *Occupational Pension*, and *Life & Health Insurance*) using `Task.WhenAll` with timeout and partial-failure resilience.
2. **Interactive Retirement & Coverage Simulator (`POST /api/simulate`)**
   * Real-time adjustments for target retirement age (`62–68`) and extra monthly contributions, recalculating projected capital and estimated monthly payouts dynamically.
   * Automated detection of critical coverage gaps, such as missing sickness insurance (*Sjukförsäkring*) or survivor protection (*Efterlevandeskydd*).
3. **Agentic AI Advisor with Transparent Tool Trace (`POST /api/advisor/chat`)**
   * Instead of static text generation, the advisor executes a multi-step tool loop (`get_user_portfolio`, `calculate_retirement_projection`, and `detect_coverage_gaps`).
   * Returns an expandable **Tool Execution Trace** in the UI displaying exact tool invocations, input parameters, and structured outputs.

---

## CI/CD Pipeline (GitHub Actions)

The repository uses path-filtered workflows under `.github/workflows/` so backend and frontend changes are validated and deployed independently:

* **`backend-ci-cd.yml`**: Triggered on changes to `backend/**`. Restores dependencies, compiles the .NET 8 Release build, runs unit tests, verifies the multi-stage Docker image build, and triggers the Render deploy hook on `main`.
* **`frontend-ci-cd.yml`**: Triggered on changes to `frontend/**`. Installs dependencies with `npm ci`, validates the Angular production bundle, and triggers the Vercel deploy hook on `main`.

---

## How I Built This with Agentic Engineering

To accelerate cross-stack delivery while maintaining strict type safety and clean architecture, this project was built using an **AI-assisted agentic development workflow**:

* **Spec-Driven Scaffolding:** Defined clear architectural boundaries, domain models, and API contracts first, using coding agents in VS Code to scaffold the .NET 8 Minimal API and Angular standalone architecture.
* **Iterative Verification:** Combined automated compilation checks (`dotnet build`, `ng build`) with live API smoke testing against Neon PostgreSQL and Groq tool-calling loops.
* **Human-Centered UI Direction:** Directed frontend generation away from generic templates toward a calm, minimalist Nordic banking interface with tabular typography and zero visual clutter.

---

## Local Setup & Running

### Prerequisites
* [.NET 8 SDK](https://microsoft.com)
* [Node.js 20+](https://nodejs.org) & Angular CLI
* A PostgreSQL connection string (e.g., from [Neon](https://neon.tech)) and a [Groq API key](https://groq.com)

### 1. Backend (.NET 8 API)
```bash
cd backend
cp .env.example .env
# Add your DATABASE_URL and GROQ_API_KEY to .env; optionally set GROQ_MODEL

dotnet restore
dotnet run
```
The API starts on `http://localhost:8080` (Swagger UI available at `/swagger` in development).

### 2. Frontend (Angular)
```bash
cd frontend
npm install
npm start
```
The application runs on `http://localhost:4200` and automatically connects to `http://localhost:8080/api`.

---

## API Overview

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/health` | Service health check for container orchestration |
| `GET` | `/api/portfolio/{userId}` | Aggregates normalized pension & insurance data (default: `user-seb-001`) |
| `POST` | `/api/simulate` | Computes compound retirement projections from interactive slider inputs |
| `POST` | `/api/advisor/chat` | Runs the multi-step Groq agent loop and returns reply + `toolTraces` |

# pensionAgent-frontend

A standalone Angular 20 application styled with Tailwind CSS. PensionPulse provides a responsive pension and insurance overview, a live retirement simulator, and an advisor panel that makes backend tool calls inspectable.

## Run locally

Requirements: Node.js 20.19 or newer and npm.

From this directory:

```powershell
npm install
npx ng serve
```

Open `http://localhost:4200`. `npm start` runs the same development server. The development configuration connects to `http://localhost:8080/api`. Start the .NET API in `../backend` and configure its `.env` first. The frontend displays an explicit connection message if the API is unavailable.

Create a production build with:

```powershell
npm run build
```

## Deploy to Vercel

1. Import the repository in Vercel and set the project root directory to `frontend`.
2. Use `npm install` as the install command and `npm run build` as the build command.
3. Use `dist/pension-agent-frontend/browser` as the output directory.
4. Update `src/environments/environment.ts` so `apiUrl` is the public base URL of your Render API service. For example, `https://your-service.onrender.com/api`.
5. Deploy the application. The backend CORS policy currently permits all origins.

The production environment currently contains `https://onrender.com` as the requested placeholder. Replace it with the deployed Render service URL before production deployment.

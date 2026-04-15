# Steps

## Overview
This project is a full-stack Transaction Dispute Portal with:
- Backend: ASP.NET Core 10 API
- Frontend: React + Vite + TypeScript + Tailwind CSS
- Database: SQL Server via Entity Framework Core with retry resilience and health checks
- Authentication: ASP.NET Core Identity with JWT and optional MFA

## Application Flow
1. User registers via the frontend registration page.
2. Backend creates the user in the Identity database and stores obfuscated account metadata.
3. User logs in with credentials.
4. If MFA is enabled, the user is prompted to provide a one-time code from an authenticator app.
5. On successful authentication, backend returns a JWT token.
6. Frontend stores the JWT token locally and sends it with subsequent API requests.
7. User can view transactions, simulate new transactions, and raise disputes.
8. Disputes are stored in the database and can be reviewed through the backend APIs.
9. The frontend uses protected routes to restrict access to authenticated users.

## Project Structure
- `backend/Capitec.Dispute.Domain/` - Domain entities and enums
- `backend/Capitec.Dispute.Application/` - Service interfaces, DTOs, and validation rules
- `backend/Capitec.Dispute.Infrastructure/` - Entity Framework DbContext and service implementations
- `backend/Capitec.Dispute.API/` - API controllers, middleware, and application startup
- `frontend/` - React app, routes, pages, and API client

## Startup Order
### 1. Backend Setup
1. Open a terminal in `backend/Capitec.Dispute.API`.
2. Restore packages and build the solution:
   - `dotnet build` from the solution root or backend root
3. If needed, ensure the database migration exists:
   - In `backend/Capitec.Dispute.Infrastructure`: `dotnet ef migrations add InitialCreate`
4. Start the API:
   - `dotnet run --project backend/Capitec.Dispute.API/Capitec.Dispute.API.csproj`
5. The API will start on HTTPS, typically `https://localhost:7192`.
6. Verify the backend health endpoint:
   - `https://localhost:7192/health`

### 2. Frontend Setup
1. Open a terminal in `frontend/`.
2. Install dependencies if not already installed:
   - `npm install`
3. Start the frontend development server:
   - `npm run dev`
4. Open the browser at the Vite URL, typically `http://localhost:5173`.

### 3. Docker Setup (Alternative)
1. Ensure Docker Desktop is installed and running.
2. From the project root, build and start all services:
   - `docker-compose up --build`
3. The services will start:
   - SQL Server on `localhost:1433` (SA password: `YourStrong!Passw0rd`)
   - Backend API on `http://localhost:8080`
   - Frontend on `http://localhost:3000`
4. Verify the backend health endpoint:
   - `http://localhost:8080/health`
5. To stop: `docker-compose down`

> Note: the backend Docker container uses the SQL Server service at `sqlserver` and overrides the default connection string via `ConnectionStrings__DefaultConnection` in `docker-compose.yml`.
> If Docker frontend build fails, check `frontend/postcss.config.js`. It must use CommonJS syntax (`module.exports = { ... }`) instead of `export default` for the current Node build environment.

## How to Use the App
1. Open the frontend URL in your browser.
2. Register a new account using email, password, name, and phone.
3. Log in to access the dashboard.
4. If MFA is enabled, follow the MFA setup flow and enter the authenticator code.
5. Navigate to Transactions to view or simulate transactions.
6. Navigate to Disputes to view disputes and dispute details.

## Notes
- The app stores the JWT token in local storage for authenticated API access.
- The backend automatically applies EF Core migrations if configured at startup.
- The frontend proxy is configured to forward `/api` requests to the backend.
- If the backend URL changes, update `frontend/.env.local` and `frontend/vite.config.ts` accordingly.

## Database Access
### Connection settings
- The backend connection string is configured in `backend/appsettings.json` as:
  - `Server=${DB_HOST:-.};Database=${DB_NAME:-TransactionDisputePortal};User Id=${DB_USER};Password=${DB_PASSWORD};Trusted_Connection=${DB_TRUSTED_CONNECTION:-true};`
- Default local setup uses `.` as the server, database `TransactionDisputePortal`, and Windows authentication when `DB_TRUSTED_CONNECTION=true`.
- In Docker, the backend uses the SQL Server service at `sqlserver` and sets the connection string via `ConnectionStrings__DefaultConnection` in `docker-compose.yml`.
- For explicit SQL authentication locally, set `DB_USER`, `DB_PASSWORD`, `DB_HOST`, and `DB_NAME` before starting the backend.

### How to log in to the database
- Using SQL Server Management Studio or Azure Data Studio:
  1. Open the tool.
  2. Connect to server `.` or `localhost`.
  3. Select authentication mode:
     - `Windows Authentication` if `DB_TRUSTED_CONNECTION=true`
     - `SQL Server Authentication` if `DB_USER` and `DB_PASSWORD` are configured
  4. Choose the `TransactionDisputePortal` database.
- Using `sqlcmd` from a terminal:
  - Windows auth: `sqlcmd -S . -d TransactionDisputePortal -E`
  - SQL auth: `sqlcmd -S . -d TransactionDisputePortal -U <DB_USER> -P <DB_PASSWORD>`

  ### Alternitave login to SQL Server

  Step-by-Step Instructions to Connect
  Open SSMS:

  Launch Microsoft SQL Server Management Studio.
 Connect to Server Dialog:

In the "Connect to Server" window:
Server type: Database Engine
Server name: localhost,1433
Authentication: SQL Server Authentication
Login: sa
Password: YourStrong!Passw0rd
Enable Trust Server Certificate:

Click the Options button at the bottom-left of the dialog.
In the "Connection Properties" tab (it should open by default):
Scroll down and check the box for Trust server certificate.
Click Connect.
Verify Connection:

If successful, the Object Explorer will show the server and databases (e.g., TransactionDisputePortal).
You can now run queries or manage the database.

### Useful queries
- List tables:
  - `SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE';`
- Check row counts for main tables:
  - `SELECT COUNT(*) AS UserCount FROM [AspNetUsers];`
  - `SELECT COUNT(*) AS TransactionCount FROM [Transactions];`
  - `SELECT COUNT(*) AS DisputeCount FROM [Disputes];`
- Inspect recent disputes:
  - `SELECT TOP 50 * FROM [Disputes] ORDER BY [CreatedAt] DESC;`
- Apply pending migrations manually:
  - `dotnet ef database update --project backend/Capitec.Dispute.API/Capitec.Dispute.API.csproj --startup-project backend/Capitec.Dispute.API/Capitec.Dispute.API.csproj`
- Run a quick health query:
  - `SELECT 1;` to verify basic SQL connectivity.

### Notes for troubleshooting
- If the database connection fails, confirm the `DefaultConnection` environment variables or local connection settings.
- If migrations do not apply, ensure the backend is built and the correct startup project is selected.
- The API health endpoint is useful for verifying both the app and SQL connectivity together.

# Build Instructions

## Prerequisites

### Global Requirements
- **Git** - Version control (https://git-scm.com/)
- **VS Code** - Code editor (https://code.visualstudio.com/)

### Backend (C#) Requirements
- **.NET SDK 8.0 or later** - Download from https://dotnet.microsoft.com/download
- **SQL Server** - Local DB or SQL Server Express (https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Visual Studio 2022** (optional, but recommended) - https://visualstudio.microsoft.com/

#### Recommended VS Code Extensions for C#:
- C# Dev Kit (Microsoft)
- REST Client (Huachao Mao)
- Thunder Client or Postman for API testing

### Frontend (React) Requirements
- **Node.js 18.0 or later** - Download from https://nodejs.org/
- **npm 9.0 or later** - Comes with Node.js

#### Recommended VS Code Extensions for React:
- ES7+ React/Redux/React-Native snippets (dsznajder)
- Prettier - Code formatter (Prettier)
- ESLint (Microsoft)

## Setup

### 1. Backend Setup (C# ASP.NET Core)

1. Open terminal and navigate to the backend folder:
   ```bash
   cd backend
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Update the database connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.;Database=TransactionDisputePortal;Trusted_Connection=true;"
   }
   ```

4. Apply database migrations (if any exist):
   ```bash
   dotnet ef database update
   ```

### 2. Frontend Setup (React + Vite)

1. Open a new terminal and navigate to the frontend folder:
   ```bash
   cd frontend
   ```

2. Install npm dependencies:
   ```bash
   npm install
   ```

3. Verify the Vite configuration in `vite.config.ts` for API proxy settings (default: `https://localhost:7000`)

## Building

### Backend Build

Navigate to the backend folder and run:
```bash
dotnet build
```

For release build:
```bash
dotnet build -c Release
```

### Frontend Build

Navigate to the frontend folder and run:
```bash
npm run build
```

The output will be generated in the `dist/` folder.

## Running

### Run Backend API

Navigate to the backend folder:
```bash
dotnet run
```

The API will typically be available at:
- `https://localhost:7000` (HTTPS)
- `http://localhost:5000` (HTTP, if configured)

Swagger UI documentation will be available at:
- `https://localhost:7000/swagger`

### Run Frontend Development Server

In a separate terminal, navigate to the frontend folder:
```bash
npm run dev
```

The application will typically be available at:
- `http://localhost:3000`

### Run Both Simultaneously

1. Open Terminal 1 and start the backend:
   ```bash
   cd backend
   dotnet run
   ```

2. Open Terminal 2 and start the frontend:
   ```bash
   cd frontend
   npm run dev
   ```

## Troubleshooting

### Common Backend Issues

**Error: "Cannot connect to SQL Server"**
- Ensure SQL Server is running on your machine
- Check the connection string in `appsettings.json`
- Verify the server name (`.` for local instance)

**Error: "Certificate error with HTTPS"**
- Generate a development certificate:
  ```bash
  dotnet dev-certs https --trust
  ```

**Error: "Port 7000 is already in use"**
- Change the port in `Properties/launchSettings.json`
- Or kill the process using the port

### Common Frontend Issues

**Error: "npm: command not found"**
- Ensure Node.js is installed: `node --version`
- Reinstall Node.js if necessary

**Error: "Module not found"**
- Clear node_modules and reinstall:
  ```bash
  rm -r node_modules
  npm install
  ```

**Frontend not connecting to backend API**
- Verify the backend is running on `https://localhost:7000`
- Check the proxy configuration in `vite.config.ts`
- Review CORS policy in backend `Program.cs`

## Additional Notes

### Project Structure

```
Transaction Dispute Portal/
├── backend/                 # C# ASP.NET Core API
│   ├── Controllers/        # API endpoints
│   ├── Services/           # Business logic
│   ├── Models/             # Data models
│   ├── Program.cs          # Application entry point
│   ├── appsettings.json    # Configuration
│   └── .csproj             # Project file
│
├── frontend/               # React + Vite application
│   ├── src/
│   │   ├── components/     # React components
│   │   ├── pages/          # Page components
│   │   └── services/       # API service calls
│   ├── package.json        # Dependencies
│   ├── vite.config.ts      # Vite configuration
│   └── tsconfig.json       # TypeScript configuration
│
└── BUILD.md               # This file
```

### Development Workflow

1. Start both backend and frontend servers
2. Development changes will auto-reload in both environments
3. Use browser DevTools to debug frontend
4. Use Visual Studio or VS Code debugger for backend
5. Use Swagger UI at `https://localhost:7000/swagger` for API testing

### Environment Variables

For production or different environments, create `.env` files:

**Frontend (.env.local):**
```
VITE_API_URL=https://api.example.com
```

**Backend (appsettings.Production.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-production-connection-string"
  }
}
```

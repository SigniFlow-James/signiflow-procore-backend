# SigniFlow-Procore Integration Backend

A .NET 8 minimal API backend that integrates Procore with SigniFlow, enabling seamless OAuth authentication and PDF document export from Procore commitment contracts.

## Features

- **OAuth 2.0 Integration** - Secure authentication with Procore sandbox environment
- **Token Management** - Automatic token refresh and session handling
- **PDF Export** - Export Procore commitment contracts as PDFs with retry logic
- **CORS Support** - Configured for web-based integrations
- **Modular Architecture** - Clean separation of concerns with services and endpoints

## Project Structure

```
/
├── Program.cs                    # Application entry point
├── Models/
│   ├── OAuthSession.cs          # OAuth session container
│   ├── ProcoreSession.cs        # Procore authentication state
│   └── SigniflowSession.cs      # SigniFlow authentication state
├── Configuration/
│   └── AppConfig.cs             # Environment variables and constants
├── Services/
│   ├── AuthService.cs           # Authentication and token management
│   └── ProcoreService.cs        # Procore API interactions
└── Endpoints/
    ├── HealthEndpoints.cs       # Health check endpoints
    ├── OAuthEndpoints.cs        # OAuth flow endpoints
    └── ApiEndpoints.cs          # Main API endpoints
```

## Prerequisites

- .NET 8.0 SDK or later
- Procore sandbox account with API access
- SigniFlow account (for full integration)

## Environment Variables

Set the following environment variables before running the application:

```bash
PROCORE_CLIENT_ID=your_procore_client_id
PROCORE_CLIENT_SECRET=your_procore_client_secret
```

### Getting Procore Credentials

1. Log in to your [Procore Sandbox account](https://sandbox.procore.com)
2. Navigate to **Company Settings** → **App Management**
3. Create a new app or select an existing one
4. Copy your **Client ID** and **Client Secret**
5. Add the redirect URI: `https://signiflow-procore-backend-net.onrender.com/oauth/callback`

## Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd signiflow-procore-backend
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Set environment variables**
   ```bash
   export PROCORE_CLIENT_ID="your_client_id"
   export PROCORE_CLIENT_SECRET="your_client_secret"
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

The API will start on `http://localhost:5000` (or the configured port).

## API Endpoints

### Health Check

```
GET /
```
Returns `OK` if the service is running.

### OAuth Flow

#### Launch OAuth Flow
```
GET /launch
```
Displays a simple HTML page with a button to initiate OAuth connection.

#### Start OAuth
```
GET /oauth/start
```
Redirects to Procore's authorization page.

#### OAuth Callback
```
GET /oauth/callback?code=<authorization_code>&state=<state>
```
Handles the OAuth callback from Procore and exchanges the authorization code for access tokens.

### Authentication

#### Check Auth Status
```
GET /api/auth/status
```

**Response:**
```json
{
  "authenticated": true,
  "expiresAt": 1704844800000
}
```

#### Refresh Token
```
POST /api/auth/refresh
```

**Response:**
```json
{
  "refreshed": true,
  "loginRequired": false,
  "auth": {
    "authenticated": true,
    "expiresAt": 1704848400000
  }
}
```

### PDF Export

#### Send Procore PDF to SigniFlow
```
POST /api/send
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "form": {},
  "context": {
    "company_id": "12345",
    "project_id": "67890",
    "object_id": "11111",
    "view": "optional_view_name"
  }
}
```

**Response (Success):**
```json
{
  "success": true,
  "pdfSize": 245678
}
```

**Response (Error):**
```json
{
  "error": "Error message description"
}
```

## Configuration

### CORS Settings

The application is configured to accept requests from:
- `https://signiflow-james.github.io`
- `https://sandbox.procore.com`

To modify CORS settings, update the policy in `Program.cs`:

```csharp
policy.WithOrigins(
    "https://your-domain.com",
    "https://another-domain.com"
)
```

### Retry Logic

PDF export retries are configured with:
- **Retry Limit:** 5 attempts
- **Delay:** 2 seconds between retries

Modify `AppConfig.RetryLimit` to change this behavior.

## Development

### Running in Development Mode

```bash
dotnet run --environment Development
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

### Docker Support (Optional)

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SigniflowProcoreBackend.dll"]
```

Build and run:
```bash
docker build -t signiflow-procore-backend .
docker run -p 8080:80 \
  -e PROCORE_CLIENT_ID=your_id \
  -e PROCORE_CLIENT_SECRET=your_secret \
  signiflow-procore-backend
```

## Security Considerations

- **Tokens are stored in memory** - Sessions will be lost on application restart
- **No persistent storage** - Consider implementing token persistence for production
- **HTTPS recommended** - Always use HTTPS in production environments
- **Environment variables** - Never commit credentials to version control
- **Token expiration** - Tokens are automatically refreshed before expiration

## Troubleshooting

### OAuth fails with "Invalid redirect URI"
Ensure the redirect URI in your Procore app settings exactly matches:
```
https://signiflow-procore-backend-net.onrender.com/oauth/callback
```

### PDF export times out
- Check that the commitment contract exists and is accessible
- Verify company_id, project_id, and object_id are correct
- Increase `RETRY_LIMIT` in `AppConfig.cs` if needed

### 401 Unauthorized errors
- Check that you've completed the OAuth flow via `/oauth/start`
- Verify your access token hasn't expired
- Try refreshing the token with `POST /api/auth/refresh`

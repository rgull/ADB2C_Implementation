# Azure AD B2C Implementation

This project implements Azure AD B2C authentication with a React TypeScript frontend and .NET Core backend, using Azure Cosmos DB for data storage.

## Architecture Overview

The application follows the architecture shown in your diagram:

1. **Frontend (React + TypeScript + Tailwind CSS)**: Handles user interface and authentication flow
2. **Backend (.NET Core API)**: Manages authentication with Azure AD B2C and user data
3. **Azure AD B2C**: Identity provider for user authentication
4. **Azure Cosmos DB**: NoSQL database for storing user information and tokens

## Project Structure

```
├── Controllers/
│   ├── AuthController.cs          # Authentication endpoints
│   └── ProfileController.cs       # User profile management
├── Models/
│   └── User.cs                    # User data model
├── Data/
│   └── ApplicationDbContext.cs    # Entity Framework context
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── LoginPage.tsx      # Login interface
│   │   │   └── ProfilePage.tsx     # Profile management
│   │   ├── services/
│   │   │   └── apiService.ts       # API communication
│   │   ├── App.tsx                # Main application
│   │   └── index.tsx              # Entry point
│   └── package.json               # Frontend dependencies
└── appsettings.json               # Configuration
```

## Setup Instructions

### 1. Backend Setup (.NET Core)

1. **Configure Azure AD B2C**:
   - Update `appsettings.json` with your Azure AD B2C configuration:
   ```json
   {
     "AzureAdB2C": {
       "Instance": "https://your-tenant-name.b2clogin.com/",
       "ClientId": "your-client-id",
       "Domain": "your-tenant-name.onmicrosoft.com",
       "SignUpSignInPolicyId": "B2C_1_signupsignin",
       "RedirectUri": "https://localhost:7000/oauth/adb2c-redirect",
       "Scope": "openid offline_access",
       "ResponseType": "code",
       "ResponseMode": "query"
     }
   }
   ```

2. **Database Setup**:
   - Create an Azure Cosmos DB account
   - Update the connection strings in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "CosmosDb:AccountEndpoint": "https://your-cosmos-account.documents.azure.com:443/",
       "CosmosDb:AccountKey": "your-cosmos-primary-key",
       "CosmosDb:DatabaseName": "ADB2C_Implementation"
     }
   }
   ```
   - The database and container will be created automatically on first run

3. **Run the Backend**:
   ```bash
   dotnet run
   ```
   The API will be available at `https://localhost:7000`

### 2. Frontend Setup (React + TypeScript + Tailwind)

1. **Install Dependencies**:
   ```bash
   cd frontend
   npm install
   ```

2. **Run the Frontend**:
   ```bash
   npm start
   ```
   The app will be available at `http://localhost:3000`

## Authentication Flow

### 1. Login Process
- User clicks "Sign in with Azure AD B2C" button
- Frontend calls `/oauth/adb2c/login` endpoint
- Backend redirects to Azure AD B2C login page
- User authenticates with Azure AD B2C

### 2. Token Handling
- Azure AD B2C redirects back to `/oauth/adb2c-redirect`
- Backend exchanges authorization code for tokens
- Backend saves user info and tokens to database
- Backend redirects to frontend with user ID

### 3. Profile Management
- Frontend receives user ID and loads profile page
- User can view and update their profile information
- Changes are saved to the database via API

## API Endpoints

### Authentication Endpoints
- `GET /oauth/adb2c/login` - Initiate login process
- `GET /oauth/adb2c-redirect` - Handle Azure AD B2C callback

### Profile Endpoints
- `GET /api/profile/{userId}` - Get user profile
- `PUT /api/profile/{userId}` - Update user profile

## Key Features

### Frontend Features
- **TypeScript**: Type-safe React components
- **Tailwind CSS**: Modern, responsive UI design
- **React Router**: Navigation between login and profile pages
- **Error Handling**: Comprehensive error states and user feedback
- **Loading States**: Visual feedback during API calls

### Backend Features
- **Azure AD B2C Integration**: Complete OAuth 2.0 flow
- **Token Management**: Secure storage of access and refresh tokens
- **User Management**: CRUD operations for user profiles
- **Session Management**: State parameter validation for security
- **Error Handling**: Comprehensive logging and error responses
- **Cosmos DB Integration**: NoSQL database with automatic scaling

## Configuration

### Azure AD B2C Setup
1. Create an Azure AD B2C tenant
2. Register a web application
3. Create a sign-up/sign-in policy
4. Configure redirect URIs:
   - `https://localhost:7000/oauth/adb2c-redirect`

### Azure Cosmos DB Setup
1. Create an Azure Cosmos DB account
2. Choose the Core (SQL) API
3. Create a database named "ADB2C_Implementation"
4. The "Users" container will be created automatically
5. Copy the connection details to your configuration

### Environment Variables
Create a `.env.local` file in the frontend directory:
```
REACT_APP_API_URL=https://localhost:7000
```

## Testing the Integration

1. **Start both services**:
   - Backend: `dotnet run` (https://localhost:7000)
   - Frontend: `npm start` (http://localhost:3000)

2. **Test Login Flow**:
   - Navigate to http://localhost:3000
   - Click "Sign in with Azure AD B2C"
   - Complete authentication in Azure AD B2C
   - Verify redirect to profile page

3. **Test Profile Management**:
   - Update profile information
   - Save changes
   - Verify data persistence

## Troubleshooting

### Common Issues
1. **CORS Errors**: Ensure CORS is configured in `Program.cs`
2. **Redirect URI Mismatch**: Verify Azure AD B2C configuration
3. **Cosmos DB Connection**: Check connection strings and account access
4. **Token Exchange Failures**: Verify Azure AD B2C client secret (if required)

### Debugging
- Check browser developer tools for frontend errors
- Check backend logs for API errors
- Verify Azure AD B2C logs in Azure portal

## Security Considerations

- State parameter validation prevents CSRF attacks
- Tokens are stored securely in the database
- HTTPS is required for production deployment
- Session management with appropriate timeouts
- Input validation on all API endpoints

## Production Deployment

1. **Update Configuration**:
   - Change redirect URIs to production URLs
   - Update CORS settings for production domain
   - Use production database connection string

2. **Security**:
   - Enable HTTPS
   - Configure proper CORS policies
   - Use secure session storage
   - Implement proper logging and monitoring

This implementation provides a complete Azure AD B2C authentication system using modern technologies (React TypeScript + Tailwind CSS frontend, .NET Core backend, and Azure Cosmos DB for data storage).

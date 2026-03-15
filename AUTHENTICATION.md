# JWT Token Authentication Middleware Documentation

## Overview
A complete JWT token authentication system with middleware-based validation, token generation, and refresh capabilities.

## Features Implemented

### ✅ **1. Token Service (`Services/TokenService.cs`)**
Handles JWT token generation and validation.

**Key Methods:**
- `GenerateToken()` - Creates JWT token with user claims
- `ValidateToken()` - Validates and parses token
- `GetUserIdFromToken()` - Extracts user ID from token

**Token Contents:**
```json
{
  "nameid": "550e8400-e29b-41d4-a716-446655440000",
  "email": "john@example.com",
  "name": "John Doe",
  "firstName": "John",
  "lastName": "Doe",
  "iat": 1710416415,
  "exp": 1710420015,
  "iss": "UserManagementAPI",
  "aud": "UserManagementAPIUsers"
}
```

### ✅ **2. Token Validation Middleware (`Middleware/TokenValidationMiddleware.cs`)**
Validates JWT tokens on all incoming requests (except whitelisted routes).

**Features:**
- Automatic token extraction from `Authorization: Bearer <token>` header
- Validates token signature, expiration, and claims
- Returns 401 Unauthorized for invalid/missing tokens
- Whitelist for unauthenticated routes
- Attaches user principal to HttpContext for use in controllers

**Whitelisted Routes (No Auth Required):**
```
/api/auth/login
/api/auth/register
/openapi/v1.json
/scalar
/swagger
/health
```

### ✅ **3. Authentication Controller (`Controllers/AuthController.cs`)**
Provides authentication endpoints.

**Endpoints:**

#### **POST `/api/auth/register`**
Register a new user and receive authentication token.

**Request:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "dateOfBirth": "1990-05-15"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "dateOfBirth": "1990-05-15"
  }
}
```

#### **POST `/api/auth/login`**
Login with email and receive authentication token.

**Request:**
```json
{
  "email": "john@example.com"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "dateOfBirth": "1990-05-15"
  }
}
```

#### **POST `/api/auth/validate`**
Validate if a token is still valid.

**Request:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response:**
```json
{
  "isValid": true
}
```

#### **POST `/api/auth/refresh`**
Refresh an existing token to extend expiration.

**Request:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

### ✅ **4. Configuration (`appsettings.json`)**
JWT settings for token generation and validation.

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-minimum-32-characters-long",
    "Issuer": "UserManagementAPI",
    "Audience": "UserManagementAPIUsers",
    "ExpirationMinutes": 60
  }
}
```

**Configuration Details:**
- `SecretKey` - Used for signing and validating tokens (⚠️ Change in production!)
- `Issuer` - Token issuer claim
- `Audience` - Token audience claim
- `ExpirationMinutes` - Token validity duration

## Usage Flow

### 1. **User Registration/Login**
```mermaid
graph LR
    A[User] -->|POST /api/auth/register| B[AuthController]
    B -->|CreateAsync| C[UserRepository]
    C -->|User created| B
    B -->|GenerateToken| D[TokenService]
    D -->|JWT Token| B
    B -->|TokenResponse| A
```

### 2. **API Request with Token**
```mermaid
graph LR
    A[Client] -->|GET /api/users + Bearer Token| B[TokenValidationMiddleware]
    B -->|ValidateToken| C[TokenService]
    C -->|Token Valid| B
    B -->|Attach ClaimsPrincipal| D[UsersController]
    D -->|Execute Action| E[Response]
    E -->|200 OK| A
```

### 3. **Invalid Token**
```mermaid
graph LR
    A[Client] -->|GET /api/users + Invalid Token| B[TokenValidationMiddleware]
    B -->|ValidateToken| C[TokenService]
    C -->|Token Invalid| B
    B -->|401 Unauthorized| A
```

## HTTP Examples

### Register User
```http
POST http://localhost:5225/api/auth/register
Content-Type: application/json

{
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@example.com",
  "dateOfBirth": "1992-03-20"
}
```

### Login User
```http
POST http://localhost:5225/api/auth/login
Content-Type: application/json

{
  "email": "jane@example.com"
}
```

### Access Protected Endpoint
```http
GET http://localhost:5225/api/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Invalid Token Response
```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "message": "Invalid or expired token",
  "statusCode": 401,
  "timestamp": "2026-03-14T10:30:15.234Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Missing Token Response
```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "message": "Missing authorization token",
  "statusCode": 401,
  "timestamp": "2026-03-14T10:30:15.234Z",
  "requestId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## Middleware Pipeline

The authentication system integrates with the existing middleware stack:

```
Request
  ↓
HttpLoggingMiddleware (logs request)
  ↓
TokenValidationMiddleware ⭐ (validates token)
  ↓
GlobalExceptionHandlingMiddleware (catches errors)
  ↓
Controller/Action
  ↓
Response
```

## Security Considerations

### ✅ Implemented
- JWT token signing with HMAC-SHA256
- Token expiration validation
- Request ID tracking for audit trail
- Logging of all auth attempts
- Environment-aware error handling
- Whitelist-based route protection

### ⚠️ For Production
1. **Change Secret Key** - Update `JwtSettings:SecretKey` in production
   ```json
   "SecretKey": "your-production-secret-key-minimum-32-chars-with-entropy"
   ```

2. **Use HTTPS** - Always use HTTPS in production
   ```
   Authorization: Bearer <token>  (over HTTPS only)
   ```

3. **Add Password Hashing** - Current login doesn't validate password
   ```csharp
   // TODO: Implement BCrypt or similar
   if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
   {
       return Unauthorized("Invalid credentials");
   }
   ```

4. **Add Token Blacklist/Revocation** - For logout functionality
   ```csharp
   public void RevokeToken(string token) { /* blacklist token */ }
   ```

5. **Rate Limiting** - Prevent brute force attacks
   ```csharp
   [RateLimitFilter(Requests = 5, Seconds = 60)]
   public async Task<IActionResult> Login(...)
   ```

6. **Refresh Token Rotation** - Use separate refresh tokens
   ```csharp
   public TokenResponse Refresh(string refreshToken) { /* ... */ }
   ```

## Testing the Authentication

### 1. **Register New User**
```bash
curl -X POST http://localhost:5225/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Test",
    "lastName": "User",
    "email": "test@example.com",
    "dateOfBirth": "1990-01-01"
  }'
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1NTBlODQwMC1lMjliLTQxZDQtYTcxNi00NDY2NTU0NDAwMDAiLCJlbWFpbCI6InRlc3RAZXhhbXBsZS5jb20iLCJuYW1lIjoiVGVzdCBVc2VyIiwiZmlyc3ROYW1lIjoiVGVzdCIsImxhc3ROYW1lIjoiVXNlciIsImlhdCI6MTcxMDQxNjQxNSwiZXhwIjoxNzEwNDIwMDE1LCJpc3MiOiJVc2VyTWFuYWdlbWVudEFQSSIsImF1ZCI6IlVzZXJNYW5hZ2VtZW50QVBJVXNlcnMifQ.abc123...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": { ... }
}
```

### 2. **Access Protected Endpoint**
```bash
TOKEN="<token-from-register>"
curl http://localhost:5225/api/users \
  -H "Authorization: Bearer $TOKEN"
```

**Response:** Returns list of users

### 3. **Invalid Token**
```bash
curl http://localhost:5225/api/users \
  -H "Authorization: Bearer invalid-token"
```

**Response:** 401 Unauthorized

### 4. **No Token**
```bash
curl http://localhost:5225/api/users
```

**Response:** 401 Unauthorized - Missing authorization token

## Next Steps

1. **Implement Password Hashing** - Add secure password storage
2. **Add Role-Based Authorization** - Implement RBAC
3. **Add Refresh Tokens** - Implement token rotation
4. **Add Token Blacklist** - Implement logout
5. **Rate Limiting** - Protect against brute force
6. **Email Verification** - Verify user email on registration

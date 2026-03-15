# UserManagementAPI
⦁	Implemented a simple CRUD API for users, wired into the existing project and verified the project builds.
⦁	Files added:
⦁	User.cs
⦁	UserCreateDto.cs
⦁	UserUpdateDto.cs
⦁	IUserRepository.cs
⦁	InMemoryUserRepository.cs
⦁	UsersController.cs
⦁	Registered repository in Program.cs.
⦁	Build: successful.

What the API provides
⦁	Routes (controller api/users):
⦁	GET /api/users — list users
⦁	GET /api/users/{id} — get user by id
⦁	POST /api/users — create user (body: UserCreateDto)
⦁	PUT /api/users/{id} — update user (body: UserUpdateDto)
⦁	DELETE /api/users/{id} — delete user

1: Enhanced Input Validation ✓
Files Modified:
⦁	UserCreateDto.cs
⦁	UserUpdateDto.cs
⦁	DateOfBirthValidationAttribute.cs (new)

Changes:
⦁	Added StringLength validation (2-50 chars) for names
⦁	Added StringLength validation (max 100) for emails
⦁	Added custom DateOfBirthValidation attribute that:
⦁	Enforces minimum age of 18 years
⦁	Enforces maximum age of 120 years
⦁	Rejects future dates
⦁	All validators include descriptive error messages
---

2: Global Exception Handling & Logging
Files Created:
⦁	ErrorResponse.cs - Standardized error response DTO
⦁	GlobalExceptionHandlingMiddleware.cs - Centralized exception handler

Changes:
⦁	Exception middleware catches and logs all unhandled exceptions
⦁	Returns consistent ErrorResponse format with timestamp
⦁	Injects ILogger into controller for operation logging
⦁	Controller logs: user creation/updates/deletions, not-found scenarios, validation errors
⦁	Added AddLogging() to dependency injection
⦁	Middleware registered in Program.cs
---

3: Pagination on GET /api/users
Files Modified:
⦁	PaginatedResponse.cs (new) - Wrapper for paginated data
⦁	IUserRepository.cs - Added async methods with pagination parameters
⦁	InMemoryUserRepository.cs - Implemented pagination logic with sorting

Changes:
⦁	GetAll() → GetAllAsync(pageNumber = 1, pageSize = 10)
⦁	Results sorted by LastName then FirstName for consistent ordering
⦁	Default page size: 10; configurable via query params
⦁	Repository returns sorted and paginated results
⦁	GET /api/users?pageNumber=2&pageSize=5 returns page 2 with 5 items
---

4: Async/Await Support
Files Modified:
⦁	IUserRepository.cs - All methods converted to async
⦁	InMemoryUserRepository.cs - All methods return Task
⦁	UsersController.cs - All endpoints now async Task

 HTTP Logging Middleware Added!
What was created:
1. HttpLoggingMiddleware.cs - Logs:
⦁	HTTP method, path, and query string
⦁	Request body (for POST/PUT)
⦁	Response status code
⦁	Response time (elapsed milliseconds)
⦁	Response body (JSON only)
⦁	Exceptions with context

2. Updated Program.cs - Registered middleware:
⦁	Runs only in Development environment
⦁	Executes before global exception handler (so all requests are logged)

3. Updated UserManagementAPI.http - Test file with:
⦁	GET all users (paginated)
⦁	GET user by ID
⦁	POST create user (valid)
⦁	POST with invalid age (validation test)
⦁	POST with invalid email (validation test)
⦁	PUT update user
⦁	DELETE user

Summary of Enhancements:

1. Custom Exception Classes (UserManagementException.cs)
⦁	UserNotFoundException (404)
⦁	DuplicateEmailException (409)
⦁	ValidationException (400)
⦁	UnauthorizedException (401)
⦁	ForbiddenException (403)

2. Request ID Tracking
⦁	Auto-generated UUID for every request
⦁	Included in X-Request-Id response header
⦁	Logged with all exceptions
⦁	Returned in error responses
⦁	Enables debugging across logs

3. Enhanced Error Responses
{
  "message": "A user with email already exists",
  "statusCode": 409,
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-14T10:30:15.234Z",
  "details": null,
  "validationErrors": null
}

4. Environment-Aware Suppression
⦁	Development: Full details + inner exceptions visible
⦁	Production: Generic message only, no sensitive info exposed

5. Comprehensive Exception Mapping
Handles 10+ exception types with appropriate HTTP status codes and messages

JWT Token Authentication Middleware Complete
What Was Implemented:

1. Token Service (TokenService.cs)
⦁	JWT token generation with user claims
⦁	Token validation with signature & expiration checks
⦁	Secure token parsing

2. Token Validation Middleware (TokenValidationMiddleware.cs)
⦁	Validates tokens from Authorization: Bearer <token> header
⦁	Returns 401 Unauthorized for invalid/missing tokens
⦁	Whitelist for unauthenticated routes (/api/auth/login, /api/auth/register, etc.)
⦁	Attaches user principal to HttpContext

3. Authentication Controller (AuthController.cs)
⦁	POST /api/auth/register - Register + get token
⦁	POST /api/auth/login - Login + get token
⦁	POST /api/auth/validate - Validate token
⦁	POST /api/auth/refresh - Refresh token

4. JWT Configuration (appsettings.json)
⦁	Secret key, issuer, audience
⦁	Token expiration (60 minutes default)

Security Features:
⦁	HMAC-SHA256 signed tokens
⦁	Token expiration validation
⦁	Request ID tracking
⦁	Comprehensive logging
⦁	Whitelist-based protection

 Documentation:

Created AUTHENTICATION.md with:
⦁	Complete API endpoint documentation
⦁	Usage examples
⦁	Security best practices for production
⦁	Testing instructions

Why This Order?

GlobalExceptionHandling,	1st (Outermost),	Must wrap everything to catch all exceptions from downstream middleware

TokenValidation,	2nd,	Validates auth before processing; exceptions here are caught by exception handler

HttpLogging,	3rd (Innermost),	Logs final request/response; minimal dependencies

This ensures:
⦁	All exceptions are caught and formatted consistently
⦁	Authentication is validated early
⦁	All activity is logged with request IDs
⦁	Proper error handling flow

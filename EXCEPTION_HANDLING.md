# Exception Handling Enhancement Documentation

## Overview
The User Management API now has comprehensive exception handling with request tracking, custom domain exceptions, and environment-aware error details suppression.

## Features Implemented

### 1. ✅ Custom Domain Exception Classes
Located in `Exceptions/UserManagementException.cs`

**Base Exception:**
- `UserManagementException` - Base for all domain exceptions with HTTP status code support

**Derived Exceptions:**
- `UserNotFoundException` - User not found (404)
- `DuplicateEmailException` - Email already exists (409 Conflict)
- `ValidationException` - Validation failures with field errors (400)
- `UnauthorizedException` - Auth required (401)
- `ForbiddenException` - Insufficient permissions (403)

**Usage Example:**
```csharp
if (existingUser != null)
{
    throw new DuplicateEmailException(dto.Email);
}
```

### 2. ✅ Request ID Tracking
Every request gets a unique `RequestId` (UUID):
- Generated automatically if not provided
- Included in all error responses via `X-Request-Id` header
- Logged with every exception for debugging
- Enables correlation across logs for troubleshooting

**Response Header:**
```
X-Request-Id: 550e8400-e29b-41d4-a716-446655440000
```

**Error Response:**
```json
{
  "message": "A user with email already exists",
  "statusCode": 409,
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-14T10:30:15.234Z"
}
```

### 3. ✅ Comprehensive Exception Type Handling

| Exception Type | HTTP Status | Response Message |
|---|---|---|
| `UserNotFoundException` | 404 | "User with ID {id} not found." |
| `DuplicateEmailException` | 409 | "A user with email '{email}' already exists." |
| `ValidationException` | 400 | "Validation failed." + field errors |
| `UnauthorizedException` | 401 | "You are not authorized..." |
| `ForbiddenException` | 403 | "User does not have permission..." |
| `ArgumentException` | 400 | "Invalid argument provided." |
| `InvalidOperationException` | 400 | "Invalid operation..." |
| `TimeoutException` | 504 | "Request timeout. Please try again..." |
| `KeyNotFoundException` | 404 | "Resource not found." |
| Default | 500 | "An unexpected error occurred..." |

### 4. ✅ Environment-Aware Error Details Suppression

**Development Environment:**
- Full exception messages included
- Inner exception details visible
- Stack traces in logs
- Perfect for debugging

```json
{
  "message": "Invalid user data",
  "details": "Object reference not set to an instance of an object. | Inner: The Email field is required.",
  "statusCode": 500,
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-14T10:30:15.234Z"
}
```

**Production Environment:**
- Generic error messages only
- No sensitive details exposed
- Exception types not revealed
- Safe for client consumption

```json
{
  "message": "An unexpected error occurred. Please try again later.",
  "details": null,
  "statusCode": 500,
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-03-14T10:30:15.234Z"
}
```

## Error Response Structure

```csharp
public class ErrorResponse
{
    public string Message { get; set; }           // User-friendly message
    public string? Details { get; set; }           // (Dev only) Exception details
    public Dictionary<string, string[]>? ValidationErrors { get; set; } // Field errors
    public int StatusCode { get; set; }            // HTTP status code
    public DateTime Timestamp { get; set; }        // When error occurred (UTC)
    public string? RequestId { get; set; }         // Unique request identifier
}
```

## Usage Examples

### Example 1: Throwing Custom Exception
```csharp
public async Task<User> CreateAsync(User user)
{
    var existing = await GetByEmailAsync(user.Email);
    if (existing != null)
    {
        throw new DuplicateEmailException(user.Email);  // → 409 Conflict
    }
    
    // ... create user
}
```

### Example 2: Validation Exception
```csharp
var errors = new Dictionary<string, string[]>
{
    { "email", new[] { "Invalid email format" } },
    { "dateOfBirth", new[] { "User must be at least 18 years old" } }
};

throw new Exceptions.ValidationException(errors);  // → 400 Bad Request
```

### Example 3: Authorization Exception
```csharp
if (!user.IsAdmin)
{
    throw new UnauthorizedException("Admin access required");  // → 401 Unauthorized
}
```

### Example 4: Not Found Exception
```csharp
var user = await _repo.GetAsync(id);
if (user == null)
{
    throw new UserNotFoundException(id);  // → 404 Not Found
}
```

## Request ID Tracking in Logs

When an exception occurs, all related logs include the RequestId:

**Log Entry:**
```
[15:30:15 ERR] Unhandled exception occurred. RequestId: 550e8400-e29b-41d4-a716-446655440000
[15:30:15 ERR] Domain exception occurred: DuplicateEmailException - A user with email 'john@example.com' already exists. - RequestId: 550e8400-e29b-41d4-a716-446655440000
```

This allows you to:
1. **Correlate** all logs for a specific request
2. **Track** a single request through multiple services
3. **Debug** issues using the RequestId from error responses

## Best Practices

1. **Use custom exceptions** instead of generic ones
   ```csharp
   throw new UserNotFoundException(id);      // ✅ Good
   throw new Exception("User not found");   // ❌ Avoid
   ```

2. **Include context** in exception messages
   ```csharp
   throw new ValidationException("Email validation failed: invalid format");  // ✅ Good
   throw new ValidationException("Invalid data");                             // ❌ Less helpful
   ```

3. **Use RequestId** when contacting support
   - Users can reference the RequestId in support tickets
   - Support team can search logs using RequestId

4. **Test in both environments**
   - Verify details are hidden in Production
   - Verify details are visible in Development

## Configuration

The middleware automatically:
- ✅ Detects environment (Development/Production)
- ✅ Generates request IDs
- ✅ Logs all exceptions
- ✅ Returns appropriate status codes
- ✅ Suppresses sensitive details in production

No additional configuration needed! 🎯

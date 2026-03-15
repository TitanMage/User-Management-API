using System;

namespace UserManagementAPI.Exceptions
{
    /// <summary>
    /// Base exception for domain-specific errors
    /// </summary>
    public class UserManagementException : Exception
    {
        public int? StatusCode { get; set; }

        public UserManagementException(string message, int? statusCode = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode ?? 500;
        }
    }

    /// <summary>
    /// Thrown when a user is not found
    /// </summary>
    public class UserNotFoundException : UserManagementException
    {
        public UserNotFoundException(Guid userId)
            : base($"User with ID {userId} not found.", 404)
        {
        }

        public UserNotFoundException(string message)
            : base(message, 404)
        {
        }
    }

    /// <summary>
    /// Thrown when a user with duplicate email already exists
    /// </summary>
    public class DuplicateEmailException : UserManagementException
    {
        public DuplicateEmailException(string email)
            : base($"A user with email '{email}' already exists.", 409)
        {
        }
    }

    /// <summary>
    /// Thrown when validation fails
    /// </summary>
    public class ValidationException : UserManagementException
    {
        public Dictionary<string, string[]> Errors { get; set; }

        public ValidationException(string message, Dictionary<string, string[]>? errors = null)
            : base(message, 400)
        {
            Errors = errors ?? new Dictionary<string, string[]>();
        }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation errors occurred.", 400)
        {
            Errors = errors;
        }
    }

    /// <summary>
    /// Thrown when user is unauthorized
    /// </summary>
    public class UnauthorizedException : UserManagementException
    {
        public UnauthorizedException(string message = "User is not authorized to perform this action.")
            : base(message, 401)
        {
        }
    }

    /// <summary>
    /// Thrown when user lacks required permissions
    /// </summary>
    public class ForbiddenException : UserManagementException
    {
        public ForbiddenException(string message = "User does not have permission to access this resource.")
            : base(message, 403)
        {
        }
    }
}
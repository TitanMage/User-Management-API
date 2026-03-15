using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserManagementAPI.Exceptions;
using UserManagementAPI.Models;

namespace UserManagementAPI.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private const string RequestIdHeaderName = "X-Request-Id";

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate and set request ID
            var requestId = context.TraceIdentifier;
            if (string.IsNullOrEmpty(requestId))
            {
                requestId = Guid.NewGuid().ToString();
            }
            context.Items["RequestId"] = requestId;
            context.Response.Headers[RequestIdHeaderName] = requestId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred. RequestId: {RequestId}", requestId);
                await HandleExceptionAsync(context, ex, requestId);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                RequestId = requestId
            };

            int statusCode = StatusCodes.Status500InternalServerError;

            // Handle domain-specific exceptions
            if (exception is UserManagementException userEx)
            {
                statusCode = userEx.StatusCode ?? StatusCodes.Status500InternalServerError;
                response.Message = userEx.Message;
                response.StatusCode = statusCode;

                if (exception is Exceptions.ValidationException valEx)
                {
                    response.ValidationErrors = valEx.Errors;
                    response.Message = userEx.Message;
                }

                _logger.LogWarning("Domain exception occurred: {ExceptionType} - {Message} - RequestId: {RequestId}",
                    exception.GetType().Name, userEx.Message, requestId);
            }
            // Handle validation exceptions from model binding
            else if (exception is Exceptions.ValidationException modelValidation)
            {
                statusCode = StatusCodes.Status400BadRequest;
                response.Message = "Validation failed.";
                response.ValidationErrors = modelValidation.Errors;
            }
            // Handle argument exceptions
            else if (exception is ArgumentException or ArgumentNullException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                response.Message = "Invalid argument provided.";
            }
            // Handle key not found exceptions
            else if (exception is KeyNotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
                response.Message = "Resource not found.";
            }
            // Handle unauthorized access
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                response.Message = "You are not authorized to perform this action.";
            }
            // Handle invalid operations
            else if (exception is InvalidOperationException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                response.Message = "Invalid operation. Please check your request.";
            }
            // Handle timeout exceptions
            else if (exception is TimeoutException)
            {
                statusCode = StatusCodes.Status504GatewayTimeout;
                response.Message = "Request timeout. Please try again later.";
            }
            // Default: internal server error
            else
            {
                statusCode = StatusCodes.Status500InternalServerError;
                response.Message = "An unexpected error occurred. Please try again later.";
            }

            // Include details only in development environment
            if (_environment.IsDevelopment())
            {
                response.Details = exception.Message;
                if (exception.InnerException != null)
                {
                    response.Details += $" | Inner: {exception.InnerException.Message}";
                }
            }
            else
            {
                // In production, provide generic message
                if (statusCode == StatusCodes.Status500InternalServerError)
                {
                    response.Details = null; // Don't expose stack traces in production
                }
            }

            response.StatusCode = statusCode;
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
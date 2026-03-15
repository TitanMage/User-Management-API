using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Middleware
{
    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpLoggingMiddleware> _logger;

        public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var request = context.Request;

            // Log incoming request
            var requestBody = await ReadRequestBodyAsync(request);
            _logger.LogInformation(
                "HTTP Request: {Method} {Path}{Query} | Content-Type: {ContentType}",
                request.Method,
                request.Path,
                request.QueryString,
                request.ContentType ?? "N/A");

            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogInformation("Request Body: {Body}", requestBody);
            }

            // Store original response stream
            var originalBodyStream = context.Response.Body;

            // Use memory stream to capture response
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                try
                {
                    await _next(context);
                    sw.Stop();

                    // Log response
                    var response = context.Response;
                    var responseBodyContent = await ReadResponseBodyAsync(responseBody);

                    _logger.LogInformation(
                        "HTTP Response: {StatusCode} | Elapsed: {ElapsedMs}ms | Content-Type: {ContentType}",
                        response.StatusCode,
                        sw.ElapsedMilliseconds,
                        response.ContentType ?? "N/A");

                    if (!string.IsNullOrWhiteSpace(responseBodyContent) && IsJsonResponse(response))
                    {
                        _logger.LogInformation("Response Body: {Body}", responseBodyContent);
                    }

                    // Copy the response to the original stream
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex,
                        "HTTP Request Failed: {Method} {Path} | Elapsed: {ElapsedMs}ms | Exception: {ExceptionMessage}",
                        request.Method,
                        request.Path,
                        sw.ElapsedMilliseconds,
                        ex.Message);
                    throw;
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();

            if (!request.ContentLength.HasValue || request.ContentLength <= 0)
                return string.Empty;

            using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                return body;
            }
        }

        private static async Task<string> ReadResponseBodyAsync(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                stream.Seek(0, SeekOrigin.Begin);
                return body;
            }
        }

        private static bool IsJsonResponse(HttpResponse response)
        {
            return response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
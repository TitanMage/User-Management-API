using System;
using System.Collections.Generic;

namespace UserManagementAPI.Models
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }
    }
}
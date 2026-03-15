using System;
using System.Collections.Generic;

namespace UserManagementAPI.DTOs
{
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
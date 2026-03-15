using System;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.DTOs
{
    public class DateOfBirthValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not DateTime dateOfBirth) return false;

            var today = DateTime.UtcNow.Date;
            var minAge = new DateTime(today.Year - 120, today.Month, today.Day);
            var maxAge = new DateTime(today.Year - 18, today.Month, today.Day);

            return dateOfBirth >= minAge && dateOfBirth <= maxAge;
        }
    }
}
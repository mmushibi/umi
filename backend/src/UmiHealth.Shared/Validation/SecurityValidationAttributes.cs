using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Web;

namespace UmiHealth.Shared.Validation
{
    /// <summary>
    /// Enhanced validation attribute for SQL injection prevention
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class SqlInjectionSafeAttribute : ValidationAttribute
    {
        private static readonly string[] SqlPatterns = new[]
        {
            @"(?i)\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?|HAVING|GROUP\s+BY|ORDER\s+BY)\b",
            @"(?i)(\bOR\b|\bAND\b)\s+\d+\s*=\s*\d+",
            @"(?i)(--|/\*|\*/|;|'|""|`|~|\||\|\|)",
            @"(?i)(\bEXEC\b.*\bXP_CMDSHELL\b|\bSP_OACREATE\b)",
            @"(?i)(\bWAITFOR\s+DELAY\b|\bBULK\s+INSERT\b)"
        };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var stringValue = value.ToString();
            
            foreach (var pattern in SqlPatterns)
            {
                if (Regex.IsMatch(stringValue, pattern))
                {
                    return new ValidationResult(
                        $"Input contains potentially dangerous SQL patterns. Pattern detected: {pattern}",
                        new[] { validationContext.MemberName });
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Enhanced validation attribute for XSS prevention
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class XssSafeAttribute : ValidationAttribute
    {
        private static readonly string[] XssPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"<iframe[^>]*>.*?</iframe>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"alert\s*\(",
            @"eval\s*\("
        };

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var stringValue = HttpUtility.HtmlDecode(value.ToString());
            
            foreach (var pattern in XssPatterns)
            {
                if (Regex.IsMatch(stringValue, pattern, RegexOptions.IgnoreCase))
                {
                    return new ValidationResult(
                        $"Input contains potentially dangerous XSS patterns. Pattern detected: {pattern}",
                        new[] { validationContext.MemberName });
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Enhanced validation for email addresses
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class EnhancedEmailAddressAttribute : ValidationAttribute
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("Email address is required", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            var email = value.ToString().Trim();

            if (string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email address is required", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            if (!EmailRegex.IsMatch(email))
                return new ValidationResult("Invalid email address format", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation for strong passwords
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class StrongPasswordAttribute : ValidationAttribute
    {
        private readonly int _minLength;

        public StrongPasswordAttribute(int minLength = 8)
        {
            _minLength = minLength;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult("Password is required", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            var password = value.ToString();

            if (password.Length < _minLength)
                return new ValidationResult($"Password must be at least {_minLength} characters long", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            if (!Regex.IsMatch(password, "[A-Z]"))
                return new ValidationResult("Password must contain at least one uppercase letter", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            if (!Regex.IsMatch(password, "[a-z]"))
                return new ValidationResult("Password must contain at least one lowercase letter", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            if (!Regex.IsMatch(password, @"\d"))
                return new ValidationResult("Password must contain at least one digit", validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);

            return ValidationResult.Success;
        }
    }
}

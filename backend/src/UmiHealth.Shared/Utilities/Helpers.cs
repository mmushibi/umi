namespace UmiHealth.Shared.Utilities;

public static class Helpers
{
    public static string GenerateUniqueNumber(string prefix)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}{timestamp}{random}";
    }

    public static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return string.Empty;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return email;

        var username = parts[0];
        var domain = parts[1];

        if (username.Length <= 2)
            return $"{username}@{domain}";

        var maskedUsername = username.Substring(0, 2) + new string('*', username.Length - 2);
        return $"{maskedUsername}@{domain}";
    }

    public static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return phoneNumber;

        var lastFour = phoneNumber.Substring(phoneNumber.Length - 4);
        var maskedPart = new string('*', phoneNumber.Length - 4);
        return $"{maskedPart}{lastFour}";
    }

    public static decimal CalculateTax(decimal amount, decimal taxRate)
    {
        return amount * (taxRate / 100);
    }

    public static decimal ApplyDiscount(decimal amount, decimal discountPercentage)
    {
        return amount * (1 - (discountPercentage / 100));
    }

    public static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    public static string FormatCurrency(decimal amount, string currency = "USD")
    {
        return currency switch
        {
            "USD" => $"${amount:N2}",
            "EUR" => $"€{amount:N2}",
            "GBP" => $"£{amount:N2}",
            "ZMW" => $"ZMW{amount:N2}",
            _ => $"{amount:N2} {currency}"
        };
    }

    public static bool IsExpired(DateTime? expiryDate)
    {
        return expiryDate.HasValue && expiryDate.Value < DateTime.UtcNow;
    }

    public static int DaysUntilExpiry(DateTime? expiryDate)
    {
        if (!expiryDate.HasValue)
            return int.MaxValue;

        return (int)(expiryDate.Value - DateTime.UtcNow).TotalDays;
    }

    public static string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new char[length];

        for (int i = 0; i < length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }

        return new string(password);
    }

    public static string GenerateVerificationCode(int length = 6)
    {
        const string digits = "0123456789";
        var random = new Random();
        var code = new char[length];

        for (int i = 0; i < length; i++)
        {
            code[i] = digits[random.Next(digits.Length)];
        }

        return new string(code);
    }

    public static string SanitizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d+]", "");
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static string GetFileExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            _ => string.Empty
        };
    }

    public static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    public static string ToTitleCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var words = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLowerInvariant();
            }
        }

        return string.Join(' ', words);
    }

    public static string TruncateWithEllipsis(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }
}

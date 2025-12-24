using System.Security.Cryptography;
using System.Text;

namespace UmiHealth.Identity.Services;

public class PasswordService : IPasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 10000;

    public string HashPassword(string password)
    {
        using var algorithm = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256);
        var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
        var salt = Convert.ToBase64String(algorithm.Salt);

        return $"{Iterations}.{salt}.{key}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            var parts = hash.Split('.', 3);

            if (parts.Length != 3)
                return false;

            var iterations = Convert.ToInt32(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);

            using var algorithm = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);
            var keyToCheck = algorithm.GetBytes(KeySize);

            return keyToCheck.Length == key.Length && 
                   CryptographicOperations.FixedTimeEquals(keyToCheck, key);
        }
        catch
        {
            return false;
        }
    }

    public bool IsValidPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // Minimum 8 characters
        if (password.Length < 8)
            return false;

        // At least one uppercase letter
        if (!password.Any(char.IsUpper))
            return false;

        // At least one lowercase letter
        if (!password.Any(char.IsLower))
            return false;

        // At least one digit
        if (!password.Any(char.IsDigit))
            return false;

        // At least one special character
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return false;

        return true;
    }

    public string GenerateRandomPassword(int length = 12)
    {
        const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        const string digitChars = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = uppercaseChars + lowercaseChars + digitChars + specialChars;
        var random = new Random();
        var password = new char[length];

        // Ensure at least one character from each category
        password[0] = uppercaseChars[random.Next(uppercaseChars.Length)];
        password[1] = lowercaseChars[random.Next(lowercaseChars.Length)];
        password[2] = digitChars[random.Next(digitChars.Length)];
        password[3] = specialChars[random.Next(specialChars.Length)];

        // Fill the rest with random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle the password
        for (int i = 0; i < length; i++)
        {
            var j = random.Next(length);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}

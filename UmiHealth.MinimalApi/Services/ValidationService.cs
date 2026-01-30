using System.Text.RegularExpressions;

namespace UmiHealth.MinimalApi.Services
{
    public interface IValidationService
    {
        bool IsValidEmail(string email);
        bool IsValidPassword(string password);
        bool IsValidUsername(string username);
        bool IsValidPhoneNumber(string phoneNumber);
        string SanitizeInput(string input);
        Dictionary<string, string> ValidateRegistrationInput(Dictionary<string, string> formData);
        Dictionary<string, string> ValidateLoginInput(Dictionary<string, string> loginData);
    }

    public class ValidationService : IValidationService
    {
        private readonly Regex _emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
        private readonly Regex _usernameRegex = new Regex(@"^[a-zA-Z0-9_-]{3,50}$", RegexOptions.Compiled);
        private readonly Regex _phoneRegex = new Regex(@"^[\+]?[1-9][\d]{0,15}$", RegexOptions.Compiled);

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            
            return _emailRegex.IsMatch(email.Trim()) && email.Length <= 255;
        }

        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
            
            // Minimum 8 characters, at least one uppercase, one lowercase, one number, one special character
            var hasUpperChar = password.Any(char.IsUpper);
            var hasLowerChar = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));
            
            return password.Length >= 8 && hasUpperChar && hasLowerChar && hasDigit && hasSpecialChar;
        }

        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;
            
            return _usernameRegex.IsMatch(username.Trim());
        }

        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true; // Phone is optional
            
            return _phoneRegex.IsMatch(phoneNumber.Trim());
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            
            // Remove potential HTML/JS tags and trim whitespace
            return System.Web.HttpUtility.HtmlEncode(input.Trim());
        }

        public Dictionary<string, string> ValidateRegistrationInput(Dictionary<string, string> formData)
        {
            var errors = new Dictionary<string, string>();

            if (!formData.TryGetValue("email", out var email) || !IsValidEmail(email))
                errors["email"] = "Valid email address is required";

            if (!formData.TryGetValue("pharmacyName", out var pharmacyName) || string.IsNullOrWhiteSpace(pharmacyName))
                errors["pharmacyName"] = "Pharmacy name is required";
            else if (pharmacyName.Length > 200)
                errors["pharmacyName"] = "Pharmacy name must be 200 characters or less";

            if (!formData.TryGetValue("password", out var password) || !IsValidPassword(password))
                errors["password"] = "Password must be at least 8 characters with uppercase, lowercase, number, and special character";

            if (formData.TryGetValue("username", out var username) && !string.IsNullOrWhiteSpace(username))
            {
                if (!IsValidUsername(username))
                    errors["username"] = "Username must be 3-50 characters, letters, numbers, underscore, or hyphen only";
            }

            if (formData.TryGetValue("phoneNumber", out var phoneNumber) && !string.IsNullOrWhiteSpace(phoneNumber))
            {
                if (!IsValidPhoneNumber(phoneNumber))
                    errors["phoneNumber"] = "Invalid phone number format";
            }

            if (formData.TryGetValue("adminFullName", out var adminFullName))
            {
                if (string.IsNullOrWhiteSpace(adminFullName))
                    errors["adminFullName"] = "Administrator full name is required";
                else if (adminFullName.Length > 200)
                    errors["adminFullName"] = "Full name must be 200 characters or less";
            }

            return errors;
        }

        public Dictionary<string, string> ValidateLoginInput(Dictionary<string, string> loginData)
        {
            var errors = new Dictionary<string, string>();

            if (!loginData.TryGetValue("username", out var username) || string.IsNullOrWhiteSpace(username))
                errors["username"] = "Username or email is required";

            if (!loginData.TryGetValue("password", out var password) || string.IsNullOrWhiteSpace(password))
                errors["password"] = "Password is required";

            return errors;
        }
    }
}

namespace UmiHealth.Identity.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool IsValidPassword(string password);
    string GenerateRandomPassword(int length = 12);
}

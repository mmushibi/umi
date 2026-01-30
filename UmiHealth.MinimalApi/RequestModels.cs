namespace UmiHealth.MinimalApi.Models;

public record UpdateProfileRequest(string? FirstName = null, string? LastName = null, string? Email = null, string? PhoneNumber = null, string? Bio = null);
public record ChangePasswordRequest(string UserId, string CurrentPassword, string NewPassword, string ConfirmPassword);
public record PharmacySettingsRequest(
    string? Name = null, 
    string? Email = null, 
    string? Phone = null, 
    string? Website = null, 
    string? Address = null, 
    string? City = null, 
    string? Province = null, 
    string? PostalCode = null, 
    string? ReceiptHeader = null, 
    string? ReceiptFooter = null, 
    decimal TaxRate = 0, 
    string? Currency = null, 
    string? ReportLogo = null, 
    string? ReportSignature = null, 
    string? PaymentTerms = null, 
    string? InvoicePrefix = null, 
    string? ReceiptPrefix = null
);
public record NotificationSettingsRequest(bool Email = false, bool Push = false, bool Sales = false, bool Inventory = false, bool Prescriptions = false);

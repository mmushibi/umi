using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.IO;
using System.Text.Json;

namespace UmiHealth.Infrastructure.Security
{
    /// <summary>
    /// Service for handling data encryption at rest
    /// </summary>
    public class DataEncryptionService
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<DataEncryptionService> _logger;

        public DataEncryptionService(IDataProtectionProvider dataProtectionProvider, ILogger<DataEncryptionService> logger)
        {
            _protector = dataProtectionProvider.CreateProtector("UmiHealth.DataProtection.v1");
            _logger = logger;
        }

        /// <summary>
        /// Encrypts sensitive data
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = _protector.Protect(plainBytes);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw new InvalidOperationException("Failed to encrypt data", ex);
            }
        }

        /// <summary>
        /// Decrypts sensitive data
        /// </summary>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var plainBytes = _protector.Unprotect(encryptedBytes);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw new InvalidOperationException("Failed to decrypt data", ex);
            }
        }

        /// <summary>
        /// Encrypts JSON objects
        /// </summary>
        public string EncryptObject<T>(T data)
        {
            if (data == null)
                return null;

            try
            {
                var jsonString = JsonSerializer.Serialize(data);
                return Encrypt(jsonString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting object of type {Type}", typeof(T).Name);
                throw new InvalidOperationException($"Failed to encrypt object of type {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Decrypts JSON objects
        /// </summary>
        public T DecryptObject<T>(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                return default(T);

            try
            {
                var decryptedJson = Decrypt(encryptedData);
                return JsonSerializer.Deserialize<T>(decryptedJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting object of type {Type}", typeof(T).Name);
                throw new InvalidOperationException($"Failed to decrypt object of type {typeof(T).Name}", ex);
            }
        }
    }

    /// <summary>
    /// Attribute for marking properties that should be encrypted
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EncryptedAttribute : Attribute
    {
        public EncryptedAttribute()
        {
        }
    }

    /// <summary>
    /// Entity framework interceptor for automatic encryption/decryption
    /// </summary>
    public class EncryptionInterceptor : ISaveChangesInterceptor
    {
        private readonly DataEncryptionService _encryptionService;

        public EncryptionInterceptor(DataEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            EncryptProperties(eventData.Context);
            return result;
        }

        public ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            EncryptProperties(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private void EncryptProperties(DbContext context)
        {
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    var properties = entry.Properties
                        .Where(p => p.Metadata.PropertyInfo?.GetCustomAttributes(typeof(EncryptedAttribute), false).Any() == true);

                    foreach (var property in properties)
                    {
                        if (property.CurrentValue is string stringValue && !string.IsNullOrEmpty(stringValue))
                        {
                            try
                            {
                                property.CurrentValue = _encryptionService.Encrypt(stringValue);
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't fail the save operation
                                // In production, you might want to handle this differently
                                Console.WriteLine($"Failed to encrypt property {property.Metadata.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configuration for data protection
    /// </summary>
    // Temporarily disabled due to build issues
    /*
    public static class DataProtectionExtensions
    {
        public static IServiceCollection AddDataProtectionServices(this IServiceCollection services, string applicationName = "UmiHealth")
        {
            services.AddDataProtection(options =>
            {
                options.ApplicationDiscriminator = applicationName;
            });

            services.AddScoped<DataEncryptionService>();

            return services;
        }

        public static IServiceCollection AddDataProtectionWithKeyRotation(this IServiceCollection services, 
            string keyDirectory = "./keys", 
            string applicationName = "UmiHealth")
        {
            services.AddDataProtection(options =>
            {
                options.ApplicationDiscriminator = applicationName;
                options.PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
                options.SetDefaultKeyLifetime(TimeSpan.FromDays(90));
                options.UseCryptographicAlgorithms(
                    new Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorConfiguration()
                    {
                        EncryptionAlgorithm = Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.EncryptionAlgorithm.AES_256_CBC,
                        ValidationAlgorithm = Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.ValidationAlgorithm.HMACSHA256
                    });
            });

            services.AddScoped<DataEncryptionService>();

            return services;
        }
    }
    */

    /// <summary>
    /// Database field encryption utilities
    /// </summary>
    public static class DatabaseEncryptionExtensions
    {
        /// <summary>
        /// Creates an encrypted column in the database
        /// </summary>
        public static string CreateEncryptedColumn(string columnName, string dataType = "TEXT")
        {
            return $"{columnName} {dataType} ENCRYPTED";
        }

        /// <summary>
        /// Generates a secure random password
        /// </summary>
        public static string GenerateSecurePassword(int length = 16)
        {
            const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
            var result = new char[length];

            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            for (int i = 0; i < length; i++)
            {
                result[i] = validChars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(validChars.Length)];
            }

            return new string(result);
        }

        /// <summary>
        /// Hashes a password securely
        /// </summary>
        public static string HashPassword(string password, string salt = null)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            salt = salt ?? GenerateSalt();
            
            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, 
                System.Text.Encoding.UTF8.GetBytes(salt), 
                10000); // iterations

            return Convert.ToBase64String(pbkdf2.GetBytes(256));
        }

        /// <summary>
        /// Generates a random salt
        /// </summary>
        public static string GenerateSalt(int length = 16)
        {
            var salt = new byte[length];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Verifies a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash, string salt)
        {
            var computedHash = HashPassword(password, salt);
            return hash == computedHash;
        }
    }

    /// <summary>
    /// Configuration for encryption settings
    /// </summary>
    public class EncryptionConfiguration
    {
        public string KeyDirectory { get; set; } = "./keys";
        public string ApplicationName { get; set; } = "UmiHealth";
        public int KeyRotationDays { get; set; } = 90;
        public bool EnableAutomaticKeyRotation { get; set; } = true;
        public string EncryptionAlgorithm { get; set; } = "AES_256_CBC";
        public string ValidationAlgorithm { get; set; } = "HMACSHA256";
    }

    /// <summary>
    /// Service for managing encryption keys
    /// </summary>
    // Temporarily disabled due to build issues
    /*
    public class EncryptionKeyManagementService
    {
        // ... implementation commented out
    }
    */
}

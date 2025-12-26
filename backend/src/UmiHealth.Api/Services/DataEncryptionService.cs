using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UmiHealth.Api.Services
{
    /// <summary>
    /// Service for data encryption and decryption
    /// </summary>
    public interface IDataEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        byte[] EncryptBytes(byte[] plainBytes);
        byte[] DecryptBytes(byte[] cipherBytes);
        string GenerateKey();
    }

    public class DataEncryptionService : IDataEncryptionService
    {
        private readonly ILogger<DataEncryptionService> _logger;
        private readonly byte[] _encryptionKey;
        private readonly byte[] _iv;

        public DataEncryptionService(IConfiguration configuration, ILogger<DataEncryptionService> logger)
        {
            _logger = logger;
            
            var keyBase64 = configuration["Encryption:Key"];
            if (string.IsNullOrEmpty(keyBase64))
            {
                _logger.LogWarning("Encryption key not found in configuration. Generating temporary key.");
                _encryptionKey = GenerateRandomKey();
            }
            else
            {
                _encryptionKey = Convert.FromBase64String(keyBase64);
            }

            _iv = new byte[12]; // IV size for GCM
            RandomNumberGenerator.Fill(_iv);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = EncryptBytes(plainBytes);
                return Convert.ToBase64String(cipherBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = DecryptBytes(cipherBytes);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public byte[] EncryptBytes(byte[] plainBytes)
        {
            if (plainBytes == null || plainBytes.Length == 0)
                return plainBytes;

            try
            {
                using var aes = new AesGcm(_encryptionKey);
                var cipherBytes = new byte[plainBytes.Length];
                var tag = new byte[16]; // Tag size for GCM

                aes.Encrypt(_iv, plainBytes, cipherBytes, tag);
                
                // Combine IV, cipher text, and tag
                var result = new byte[_iv.Length + cipherBytes.Length + tag.Length];
                Buffer.BlockCopy(_iv, 0, result, 0, _iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, result, _iv.Length, cipherBytes.Length);
                Buffer.BlockCopy(tag, 0, result, _iv.Length + cipherBytes.Length, tag.Length);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting bytes");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public byte[] DecryptBytes(byte[] cipherBytes)
        {
            if (cipherBytes == null || cipherBytes.Length < 28) // Minimum size: IV(12) + tag(16) + data(1)
                return cipherBytes;

            try
            {
                using var aes = new AesGcm(_encryptionKey);
                
                // Extract IV, cipher text, and tag
                var iv = new byte[12];
                var tag = new byte[16];
                var dataLength = cipherBytes.Length - iv.Length - tag.Length;
                var data = new byte[dataLength];

                Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, iv.Length, data, 0, dataLength);
                Buffer.BlockCopy(cipherBytes, iv.Length + dataLength, tag, 0, tag.Length);

                var plainBytes = new byte[dataLength];
                aes.Decrypt(iv, data, tag, plainBytes);

                return plainBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting bytes");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public string GenerateKey()
        {
            var key = GenerateRandomKey();
            return Convert.ToBase64String(key);
        }

        private byte[] GenerateRandomKey()
        {
            var key = new byte[32]; // 256 bits
            RandomNumberGenerator.Fill(key);
            return key;
        }
    }

    /// <summary>
    /// Extension methods for data encryption service
    /// </summary>
    public static class DataEncryptionExtensions
    {
        public static IServiceCollection AddDataEncryption(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDataEncryptionService>(provider => 
                new DataEncryptionService(configuration, provider.GetService<ILogger<DataEncryptionService>>()));
            
            return services;
        }
    }
}

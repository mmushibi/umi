using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace UmiHealth.API.Services
{
    public interface IRsaKeyService
    {
        RSA GetPrivateKey();
        RSA GetPublicKey();
    }

    public class RsaKeyService : IRsaKeyService
    {
        private readonly RSA _privateKey;
        private readonly RSA _publicKey;

        public RsaKeyService(IConfiguration configuration)
        {
            _privateKey = CreateOrLoadPrivateKey(configuration);
            _publicKey = CreateOrLoadPublicKey(_privateKey);
        }

        public RSA GetPrivateKey() => _privateKey;
        public RSA GetPublicKey() => _publicKey;

        private static RSA CreateOrLoadPrivateKey(IConfiguration configuration)
        {
            var rsa = RSA.Create();
            
            // Try to load from configuration
            var privateKeyPem = configuration["Jwt:PrivateKeyPem"] ?? configuration["Jwt:Secret"];
            if (!string.IsNullOrEmpty(privateKeyPem))
            {
                try
                {
                    rsa.ImportFromPem(privateKeyPem);
                    return rsa;
                }
                catch
                {
                    // If loading fails, create new key
                }
            }

            // Generate new key pair
            rsa.KeySize = 2048;
            
            // Export the keys for storage (in production, store these securely)
            var publicKeyPem = rsa.ExportRSAPublicKeyPem();
            var privateKeyPemGenerated = rsa.ExportRSAPrivateKeyPem();
            
            // Log the keys for development (REMOVE IN PRODUCTION)
            Console.WriteLine("=== JWT Keys (Development Only) ===");
            Console.WriteLine($"Public Key: {publicKeyPem}");
            Console.WriteLine($"Private Key: {privateKeyPemGenerated}");
            Console.WriteLine("=====================================");
            
            return rsa;
        }

        private static RSA CreateOrLoadPublicKey(RSA privateKey)
        {
            var rsa = RSA.Create();
            
            // Use the public part of the private key
            var publicKey = privateKey.ExportRSAPublicKey();
            rsa.ImportRSAPublicKey(publicKey, out _);
            
            return rsa;
        }
    }
}

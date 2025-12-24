using Microsoft.Extensions.Options;
using System.IO;
using System.Threading.Tasks;

namespace UmiHealth.Infrastructure.Storage
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(string containerName, string fileName, Stream fileStream, string contentType = null);
        Task<Stream> DownloadFileAsync(string containerName, string fileName);
        Task<bool> DeleteFileAsync(string containerName, string fileName);
        Task<bool> FileExistsAsync(string containerName, string fileName);
        Task<IEnumerable<string>> ListFilesAsync(string containerName, string prefix = null);
        Task<string> GetFileUrlAsync(string containerName, string fileName, TimeSpan? expiry = null);
        Task<string> GetPresignedUrlAsync(string containerName, string fileName, TimeSpan expiry);
    }

    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _baseStoragePath;
        private readonly string _baseUrl;

        public LocalFileStorageService(IOptions<FileStorageOptions> options)
        {
            var storageOptions = options.Value;
            _baseStoragePath = storageOptions.BasePath ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
            _baseUrl = storageOptions.BaseUrl ?? "/storage";
            
            // Ensure base directory exists
            Directory.CreateDirectory(_baseStoragePath);
        }

        public async Task<string> UploadFileAsync(string containerName, string fileName, Stream fileStream, string contentType = null)
        {
            try
            {
                var containerPath = Path.Combine(_baseStoragePath, containerName);
                Directory.CreateDirectory(containerPath);

                var filePath = Path.Combine(containerPath, fileName);
                
                // Ensure unique filename
                var uniqueFileName = GetUniqueFileName(containerPath, fileName);
                var uniqueFilePath = Path.Combine(containerPath, uniqueFileName);

                using (var fileStreamOutput = new FileStream(uniqueFilePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                return Path.Combine(containerName, uniqueFileName).Replace("\\", "/");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to upload file '{fileName}' to container '{containerName}': {ex.Message}", ex);
            }
        }

        public async Task<Stream> DownloadFileAsync(string containerName, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_baseStoragePath, containerName, fileName);
                
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File '{fileName}' not found in container '{containerName}'");

                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download file '{fileName}' from container '{containerName}': {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string containerName, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_baseStoragePath, containerName, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete file '{fileName}' from container '{containerName}': {ex.Message}");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string containerName, string fileName)
        {
            try
            {
                var filePath = Path.Combine(_baseStoragePath, containerName, fileName);
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check file existence for '{fileName}' in container '{containerName}': {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<string>> ListFilesAsync(string containerName, string prefix = null)
        {
            try
            {
                var containerPath = Path.Combine(_baseStoragePath, containerName);
                
                if (!Directory.Exists(containerPath))
                    return Enumerable.Empty<string>();

                var files = Directory.GetFiles(containerPath, prefix + "*.*", SearchOption.TopDirectoryOnly);
                return files.Select(f => Path.GetFileName(f));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to list files in container '{containerName}': {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

        public async Task<string> GetFileUrlAsync(string containerName, string fileName, TimeSpan? expiry = null)
        {
            // For local storage, return relative URL
            return $"{_baseUrl}/{containerName}/{fileName}";
        }

        public async Task<string> GetPresignedUrlAsync(string containerName, string fileName, TimeSpan expiry)
        {
            // For local storage, this is not typically used
            // In production with cloud storage, this would generate a temporary signed URL
            return await GetFileUrlAsync(containerName, fileName);
        }

        private string GetUniqueFileName(string directory, string fileName)
        {
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 1;

            var uniqueFileName = fileName;
            
            while (File.Exists(Path.Combine(directory, uniqueFileName)))
            {
                uniqueFileName = $"{baseFileName}_{counter}{extension}";
                counter++;
            }

            return uniqueFileName;
        }
    }

    public class FileStorageOptions
    {
        public string BasePath { get; set; }
        public string BaseUrl { get; set; }
        public int MaxFileSizeMB { get; set; } = 10;
        public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };
    }

    public static class StorageContainers
    {
        // Tenant-specific containers
        public static string TenantDocuments(string tenantId) => $"tenant-{tenantId}-documents";
        public static string TenantImages(string tenantId) => $"tenant-{tenantId}-images";
        public static string TenantExports(string tenantId) => $"tenant-{tenantId}-exports";
        public static string TenantBackups(string tenantId) => $"tenant-{tenantId}-backups";
        
        // Branch-specific containers
        public static string BranchDocuments(string tenantId, string branchId) => $"tenant-{tenantId}-branch-{branchId}-documents";
        public static string BranchImages(string tenantId, string branchId) => $"tenant-{tenantId}-branch-{branchId}-images";
        
        // System containers
        public static string SystemLogs() => "system-logs";
        public static string SystemBackups() => "system-backups";
        public static string SystemExports() => "system-exports";
        public static string UserProfiles() => "user-profiles";
        
        // Product images
        public static string ProductImages(string tenantId) => $"tenant-{tenantId}-product-images";
        
        // Patient documents
        public static string PatientDocuments(string tenantId, string patientId) => $"tenant-{tenantId}-patient-{patientId}-documents";
        
        // Prescription documents
        public static string PrescriptionDocuments(string tenantId, string prescriptionId) => $"tenant-{tenantId}-prescription-{prescriptionId}-documents";
    }
}

namespace Inventory.Domain.Interfaces
{
    public interface IStorageService
    {
        Task UploadAsync(Stream stream, string objectName, string contentType);
        Task DeleteAsync(string objectName);
        Task<string> GenerateSignedUrlAsync(string objectName, DateTimeOffset expiresAt);
    }
}

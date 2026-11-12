namespace Inventory.Domain.Options
{
    public class GoogleCloudStorageOptions
    {
        public const string SectionName = "GoogleCloudStorage";
        public const string RequiredProductImagePrefix = "sitracalco_inventory_dev/image/products";

        public string BucketName { get; set; } = "calco-storage";
        public string AuthenticationMode { get; set; } = "Adc";
        public string? Endpoint { get; set; }
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string ProductImagePrefix { get; set; } = RequiredProductImagePrefix;
        public int SignedUrlExpirationMinutes { get; set; } = 15;
        public int MaxImageSizeMb { get; set; } = 5;
        public long MaxImageSizeBytes => (long)MaxImageSizeMb * 1024 * 1024;
    }
}

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Options;
using Microsoft.Extensions.Options;
using System.Net;

namespace Inventory.Infrastructure.Persistance.Services
{
    public class GoogleCloudStorageService : IStorageService
    {
        private readonly GoogleCloudStorageOptions _options;
        private readonly Lazy<Task<GoogleCredential>> _credential;
        private readonly Lazy<Task<StorageClient>> _client;

        public GoogleCloudStorageService(IOptions<GoogleCloudStorageOptions> options)
        {
            _options = options.Value;
            // ADC se resuelve al usar imágenes; iniciar Swagger u otros endpoints no requiere credenciales GCP.
            _credential = new Lazy<Task<GoogleCredential>>(() => GoogleCredential.GetApplicationDefaultAsync());
            _client = new Lazy<Task<StorageClient>>(async () => await StorageClient.CreateAsync(await _credential.Value));
        }

        public async Task UploadAsync(Stream stream, string objectName, string contentType)
        {
            ValidateObjectName(objectName);
            var client = await _client.Value;
            await client.UploadObjectAsync(_options.BucketName, objectName, contentType, stream,
                new UploadObjectOptions { IfGenerationMatch = 0 });
        }

        public async Task DeleteAsync(string objectName)
        {
            ValidateObjectName(objectName);
            var client = await _client.Value;
            try
            {
                await client.DeleteObjectAsync(_options.BucketName, objectName);
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // La limpieza es idempotente: un objeto ya eliminado no requiere otra acción.
            }
        }

        public async Task<string> GenerateSignedUrlAsync(string objectName, DateTimeOffset expiresAt)
        {
            ValidateObjectName(objectName);
            var signer = UrlSigner.FromCredential(await _credential.Value);
            var request = UrlSigner.RequestTemplate
                .FromBucket(_options.BucketName)
                .WithObjectName(objectName)
                .WithHttpMethod(HttpMethod.Get);
            var options = UrlSigner.Options.FromExpiration(expiresAt).WithSigningVersion(SigningVersion.V4);
            return await signer.SignAsync(request, options);
        }

        private void ValidateObjectName(string objectName)
        {
            if (!objectName.StartsWith(_options.ProductImagePrefix + "/", StringComparison.Ordinal) ||
                objectName.Contains("..", StringComparison.Ordinal) || objectName.Contains('\\'))
                throw new ArgumentException("El objeto debe pertenecer al prefijo de imágenes de productos.", nameof(objectName));
        }
    }
}

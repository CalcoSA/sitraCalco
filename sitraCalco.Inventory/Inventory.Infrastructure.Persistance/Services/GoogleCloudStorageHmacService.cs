
using Google.Cloud.Storage.V1;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;

namespace Inventory.Infrastructure.Persistance.Services
{
    public sealed class GoogleCloudStorageHmacService : IStorageService
    {
        private static readonly HttpClient Http = new();

        private readonly GoogleCloudStorageOptions _options;
        private readonly ILogger<GoogleCloudStorageHmacService> _logger;
        private readonly UrlSigner _signer;

        public GoogleCloudStorageHmacService(
            IOptions<GoogleCloudStorageOptions> options,
            ILogger<GoogleCloudStorageHmacService> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (!HasValidConfiguration(_options))
                throw new InvalidOperationException(
                    "La configuración HMAC de GCS es inválida.");

            var hmacSigner = UrlSigner.HmacBlobSigner.Create(
                _options.AccessKey!,
                _options.SecretKey!);

            _signer = UrlSigner.FromBlobSigner(hmacSigner);
        }

        internal static bool HasValidConfiguration(
            GoogleCloudStorageOptions options)
        {
            return !string.IsNullOrWhiteSpace(options.BucketName)
                && !options.BucketName.Contains('/')
                && !string.IsNullOrWhiteSpace(options.AccessKey)
                && !string.IsNullOrWhiteSpace(options.SecretKey)
                && Uri.TryCreate(
                    options.Endpoint,
                    UriKind.Absolute,
                    out var endpoint)
                && endpoint.Scheme == Uri.UriSchemeHttps
                && endpoint.Host.Equals(
                    "storage.googleapis.com",
                    StringComparison.OrdinalIgnoreCase)
                && endpoint.AbsolutePath == "/"
                && string.IsNullOrEmpty(endpoint.UserInfo)
                && string.IsNullOrEmpty(endpoint.Query)
                && string.IsNullOrEmpty(endpoint.Fragment);
        }

        public async Task UploadAsync(
            Stream stream,
            string objectName,
            string contentType)
        {
            ValidateObjectName(objectName);

            // No modificar ni cerrar el stream del llamador.
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;

            while ((read = await stream.ReadAsync(chunk)) > 0)
            {
                if (buffer.Length + read > _options.MaxImageSizeBytes)
                    throw new InvalidOperationException(
                        "La imagen supera el tamaño permitido.");

                await buffer.WriteAsync(chunk.AsMemory(0, read));
            }

            buffer.Position = 0;

            var template = UrlSigner.RequestTemplate
                .FromBucket(_options.BucketName)
                .WithObjectName(objectName)
                .WithHttpMethod(HttpMethod.Put)
                .WithContentHeaders(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        ["Content-Type"] = new[] { contentType }
                    })
                .WithRequestHeaders(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        ["x-goog-if-generation-match"] = new[] { "0" }
                    });

            var options = UrlSigner.Options
                .FromDuration(TimeSpan.FromMinutes(5))
                .WithSigningVersion(SigningVersion.V4);

            var url = await _signer.SignAsync(template, options);

            using var request = new HttpRequestMessage(
                HttpMethod.Put, url);

            request.Headers.TryAddWithoutValidation(
                "x-goog-if-generation-match", "0");

            request.Content = new StreamContent(buffer);
            request.Content.Headers.ContentType =
                new MediaTypeHeaderValue(contentType);

            try
            {
                using var response = await Http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "GCS HMAC Upload falló. HTTP {Status}",
                        (int)response.StatusCode);

                    throw new InvalidOperationException(
                        "No se pudo subir la imagen a GCS.");
                }
            }
            catch (HttpRequestException)
            {
                _logger.LogError(
                    "Error de comunicación durante GCS HMAC Upload.");

                throw new InvalidOperationException(
                    "No se pudo conectar con Google Cloud Storage.");
            }
        }

        public async Task DeleteAsync(string objectName)
        {
            ValidateObjectName(objectName);

            var url = await _signer.SignAsync(
                _options.BucketName,
                objectName,
                TimeSpan.FromMinutes(5),
                HttpMethod.Delete,
                SigningVersion.V4);

            try
            {
                using var response = await Http.DeleteAsync(url);

                // Conserva el borrado idempotente.
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "GCS HMAC Delete falló. HTTP {Status}",
                        (int)response.StatusCode);

                    throw new InvalidOperationException(
                        "No se pudo eliminar la imagen de GCS.");
                }
            }
            catch (HttpRequestException)
            {
                _logger.LogError(
                    "Error de comunicación durante GCS HMAC Delete.");

                throw new InvalidOperationException(
                    "No se pudo conectar con Google Cloud Storage.");
            }
        }

        public async Task<string> GenerateSignedUrlAsync(
            string objectName,
            DateTimeOffset expiresAt)
        {
            ValidateObjectName(objectName);

            var duration = expiresAt - DateTimeOffset.UtcNow;

            if (duration <= TimeSpan.Zero ||
                duration > TimeSpan.FromDays(7))
                throw new InvalidOperationException(
                    "Tiempo de expiración inválido.");

            return await _signer.SignAsync(
                _options.BucketName,
                objectName,
                duration,
                HttpMethod.Get,
                SigningVersion.V4);
        }

        private void ValidateObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName) ||
                !objectName.StartsWith(
                    _options.ProductImagePrefix + "/",
                    StringComparison.Ordinal) ||
                objectName.Contains("..", StringComparison.Ordinal) ||
                objectName.Contains('\\'))
            {
                throw new ArgumentException(
                    "El objeto no pertenece al prefijo permitido.",
                    nameof(objectName));
            }
        }
    }
}

namespace Inventory.Domain.Helpers
{
    public static class ProductImageFiles
    {
        public static string? Validate(string fileName, string contentType, long length, long maxBytes)
        {
            if (length <= 0)
                return "El archivo de imagen está vacío.";

            if (length > maxBytes)
                return $"La imagen no puede superar {maxBytes / (1024 * 1024)} MB.";

            var expectedContentType = Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };

            if (expectedContentType is null)
                return "Solo se permiten imágenes .jpg, .jpeg, .png y .webp.";

            return string.Equals(contentType, expectedContentType, StringComparison.OrdinalIgnoreCase)
                ? null
                : "El Content-Type no es válido para la extensión de la imagen.";
        }
    }
}

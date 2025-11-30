using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace UpsaMe_API.Helpers
{
    public class BlobStorageHelper
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageHelper(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        /// <summary>
        /// Sube una imagen (PNG o JPG) a Azure Blob Storage.
        /// </summary>
        public async Task<string> UploadPngAsync(IFormFile file, string containerName, string fileNamePrefix)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("El archivo está vacío.");

            // ============================
            // VALIDACIÓN DE TIPO DE IMAGEN
            // ============================
            var contentType = (file.ContentType ?? string.Empty).ToLowerInvariant();
            var ext = (Path.GetExtension(file.FileName) ?? string.Empty).ToLowerInvariant();

            bool isPng =
                contentType == "image/png" ||
                contentType == "image/x-png" ||
                ext == ".png";

            bool isJpg =
                contentType == "image/jpeg" ||
                contentType == "image/jpg" ||
                contentType == "image/pjpeg" ||
                ext == ".jpg" ||
                ext == ".jpeg";

            if (!isPng && !isJpg)
                throw new InvalidOperationException("Solo se permiten imágenes PNG o JPG.");

            // Elegir extensión y content-type reales
            string extension = isPng ? ".png" : ".jpg";
            string blobContentType = isPng ? "image/png" : "image/jpeg";

            // ============================
            // OBTENER / CREAR CONTAINER
            // ============================
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // ============================
            // NOMBRE ÚNICO DEL ARCHIVO
            // ============================
            string fileName = $"{fileNamePrefix}_{Guid.NewGuid():N}{extension}";
            var blobClient = containerClient.GetBlobClient(fileName);

            // ============================
            // SUBIR IMAGEN
            // ============================
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = blobContentType
                });
            }

            return blobClient.Uri.ToString();
        }
    }
}

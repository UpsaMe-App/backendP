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
        /// Sube un archivo PNG a Azure Blob Storage.
        /// </summary>
        public async Task<string> UploadPngAsync(IFormFile file, string containerName, string fileNamePrefix)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("El archivo está vacío.");

            // Validar formato PNG
            bool isPng = file.ContentType == "image/png" 
                         || Path.GetExtension(file.FileName).Equals(".png", StringComparison.OrdinalIgnoreCase);

            if (!isPng)
                throw new InvalidOperationException("Solo se permiten imágenes PNG.");

            // Obtener container
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Crear nombre único
            string fileName = $"{fileNamePrefix}_{Guid.NewGuid():N}.png";
            var blobClient = containerClient.GetBlobClient(fileName);

            // Subir imagen
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = "image/png"
                });
            }

            return blobClient.Uri.ToString();
        }
    }
}
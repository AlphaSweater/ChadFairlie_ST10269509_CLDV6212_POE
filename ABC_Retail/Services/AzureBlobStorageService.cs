using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABC_Retail.Services
{
	public class AzureBlobStorageService
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly string _containerName = "imagefiles";

		/// <summary>
		/// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
		/// </summary>
		public AzureBlobStorageService(string storageConnectionString)
		{
			_blobServiceClient = new BlobServiceClient(storageConnectionString);
		}

		/// <summary>
		/// Uploads a file to Azure Blob Storage.
		/// </summary>
		/// <param name="file">The file to upload.</param>
		/// <returns>The URL of the uploaded file.</returns>
		public async Task<string> UploadFileAsync(IFormFile file)
		{
			// Get a reference to the container
			var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

			// Create the container if it does not exist
			await containerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);

			// Generate a unique name for the blob and get a reference to the blob client
			var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString() + "_" + Path.GetExtension(file.FileName));

			// Upload the file to the blob
			using (var stream = file.OpenReadStream())
			{
				await blobClient.UploadAsync(stream, true);
			}

			// Return the URL of the uploaded file
			return blobClient.Uri.ToString();
		}

		/// <summary>
		/// Deletes a file from Azure Blob Storage.
		/// </summary>
		/// <param name="blobUri">The URI of the blob to delete.</param>
		public async Task DeleteFileAsync(string blobUri)
		{
			// Create a blob client using the URI of the blob
			var blobClient = new BlobClient(new Uri(blobUri));

			// Delete the blob if it exists
			await blobClient.DeleteIfExistsAsync();
		}

		/// <summary>
		/// Lists all files in the specified container in Azure Blob Storage.
		/// </summary>
		/// <returns>A list of URLs of the blobs in the container.</returns>
		public async Task<IEnumerable<string>> ListFilesAsync()
		{
			// Get a reference to the container
			var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
			var blobs = new List<string>();

			// List all blobs in the container
			await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
			{
				blobs.Add(containerClient.GetBlobClient(blobItem.Name).Uri.ToString());
			}

			// Return the list of blob URLs
			return blobs;
		}
	}
}
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace ABC_Retail.Services
{
	public class AzureBlobStorageService
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly SasTokenGenerator _sasTokenGenerator;
		private readonly ILogger<AzureBlobStorageService> _logger;
		private readonly string _imageContainerName = "imagefiles";

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
		/// </summary>
		public AzureBlobStorageService(BlobServiceClient blobServiceClient, SasTokenGenerator sasTokenGenerator, ILogger<AzureBlobStorageService> logger)
		{
			// Initialize the BlobServiceClient, SasTokenGenerator, and Logger
			_blobServiceClient = blobServiceClient;
			_sasTokenGenerator = sasTokenGenerator;
			_logger = logger;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to interact with Azure Blob Storage
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Uploads a file to Azure Blob Storage.
		/// </summary>
		/// <param name="file">The file to upload.</param>
		/// <returns>The URL of the uploaded file.</returns>
		public async Task<string> UploadFileAsync(IFormFile file)
		{
			// Generate a SAS token for the container with write and create permissions
			var containerSasToken = _sasTokenGenerator.GenerateContainerSasToken(
				_imageContainerName,
				BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create);

			// Create the BlobClient using the SAS token
			var blobClient = new BlobClient(
				new Uri($"{_blobServiceClient.GetBlobContainerClient(_imageContainerName).Uri}{containerSasToken}"));

			// Generate a unique name for the blob and get a reference to the blob client
			var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
			blobClient = blobClient.GetParentBlobContainerClient().GetBlobClient(blobName);

			try
			{
				_logger.LogInformation("Uploading file {FileName} as blob {BlobName} to container {ContainerName}.", file.FileName, blobName, _imageContainerName);

				// Upload the file to the blob
				using (var stream = file.OpenReadStream())
				{
					await blobClient.UploadAsync(stream, true);
				}

				_logger.LogInformation("File {FileName} uploaded successfully to {BlobUri}.", file.FileName, blobClient.Uri);

				// Return the URL of the uploaded file
				return blobClient.Uri.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while uploading file {FileName}.", file.FileName);
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Deletes a file from Azure Blob Storage.
		/// </summary>
		/// <param name="blobName">The name of the blob to delete.</param>
		public async Task DeleteFileAsync(string blobName)
		{
			try
			{
				// Generate a SAS token for the blob with delete permission
				var blobSasToken = _sasTokenGenerator.GenerateBlobSasToken(
					_imageContainerName,
					blobName,
					BlobSasPermissions.Delete);

				// Build the blob URI using the container name and blob name
				var blobUri = new UriBuilder(_blobServiceClient.Uri)
				{
					Path = $"{_imageContainerName}/{blobName}",
					Query = blobSasToken
				}.Uri;

				// Create the BlobClient using the URI with the SAS token
				var blobClient = new BlobClient(blobUri);

				_logger.LogInformation("Deleting blob {BlobName} from {BlobUri}.", blobName, blobUri);

				// Delete the blob if it exists
				await blobClient.DeleteIfExistsAsync();

				_logger.LogInformation("Blob {BlobName} deleted successfully from {BlobUri}.", blobName, blobUri);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while deleting blob {BlobName}.", blobName);
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Lists all files in the specified container in Azure Blob Storage.
		/// </summary>
		/// <returns>A list of <see cref="BlobClient"/> items.</returns>
		public async Task<IEnumerable<BlobClient>> ListFilesAsync()
		{
			try
			{
				// Generate a SAS token for the container with list and read permissions
				var containerSasToken = _sasTokenGenerator.GenerateContainerSasToken(
					_imageContainerName,
					BlobContainerSasPermissions.List | BlobContainerSasPermissions.Read);

				// Create the BlobContainerClient using the SAS token
				var containerClient = new BlobContainerClient(
					new Uri($"{_blobServiceClient.GetBlobContainerClient(_imageContainerName).Uri}{containerSasToken}"));

				_logger.LogInformation("Listing all blobs in container {ContainerName}.", _imageContainerName);

				var blobs = new List<BlobClient>();

				// List all blobs in the container
				await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
				{
					var blobClient = containerClient.GetBlobClient(blobItem.Name);
					_logger.LogDebug("Found blob {BlobName} in container {ContainerName}.", blobItem.Name, _imageContainerName);
					blobs.Add(blobClient);
				}

				_logger.LogInformation("Retrieved {BlobCount} blobs from container {ContainerName}.", blobs.Count, _imageContainerName);

				// Return the list of blob URLs
				return blobs;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while listing blobs in container {ContainerName}.", _imageContainerName);
				throw;
			}
		}
	}

	public class SasTokenGenerator
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly ILogger<SasTokenGenerator> _logger;
		private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

		public SasTokenGenerator(BlobServiceClient blobServiceClient, ILogger<SasTokenGenerator> logger)
		{
			_blobServiceClient = blobServiceClient;
			_logger = logger;
		}

		public string GenerateContainerSasToken(string containerName, BlobContainerSasPermissions permissions, TimeSpan? expiration = null)
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

			if (!containerClient.CanGenerateSasUri)
			{
				_logger.LogError("Cannot generate SAS token. Account does not have access keys for container {ContainerName}.", containerName);
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}

			var sasToken = containerClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration));
			return sasToken.Query;
		}

		public string GenerateBlobSasToken(string containerName, string blobName, BlobSasPermissions permissions, TimeSpan? expiration = null)
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			var blobClient = containerClient.GetBlobClient(blobName);

			if (!blobClient.CanGenerateSasUri)
			{
				_logger.LogError("Cannot generate SAS token. Account does not have access keys for blob {BlobName}.", blobName);
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}

			var sasToken = blobClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration));
			return sasToken.Query;
		}
	}
}
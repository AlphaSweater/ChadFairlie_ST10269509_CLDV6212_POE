using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace ABC_Retail.Services
{
	public class AzureBlobStorageService
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly BlobContainerClient _containerClient;
		private readonly string _containerName = "imagefiles";
		private readonly ILogger<AzureBlobStorageService> _logger;

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
		/// </summary>
		public AzureBlobStorageService(string connectionString, ILogger<AzureBlobStorageService> logger)
		{
			// Initialize the BlobServiceClient and BlobContainerClient using the connection string
			_blobServiceClient = new BlobServiceClient(connectionString);
			_containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
			_logger = logger;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to generate SAS tokens
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		// Generate a SAS token for a container with the specified permissions and expiry time
		public string GenerateContainerSasToken(string containerName, BlobContainerSasPermissions permissions, DateTimeOffset expiresOn)
		{
			// Retrieve the container client
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

			// Check if the account has access keys, which is required to generate a SAS token
			if (containerClient.CanGenerateSasUri)
			{
				_logger.LogDebug("Generating SAS token for container {ContainerName} with permissions {Permissions} and expiry {Expiry}.", containerName, permissions, expiresOn);
				// Generate the SAS token for the container
				var sasToken = containerClient.GenerateSasUri(permissions, expiresOn);
				return sasToken.Query;
			}
			else
			{
				_logger.LogError("Cannot generate SAS token. Account does not have access keys for container {ContainerName}.", containerName);
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Generate a SAS token for a blob with the specified permissions and expiry time
		public string GenerateBlobSasToken(string blobName, BlobSasPermissions permissions, DateTimeOffset expiresOn)
		{
			// Retrieve the blob client
			var blobClient = _containerClient.GetBlobClient(blobName);

			// Check if the account has access keys, which is required to generate a SAS token
			if (blobClient.CanGenerateSasUri)
			{
				_logger.LogDebug("Generating SAS token for blob {BlobName} with permissions {Permissions} and expiry {Expiry}.", blobName, permissions, expiresOn);
				// Generate the SAS token for the blob
				var sasToken = blobClient.GenerateSasUri(permissions, expiresOn);
				return sasToken.Query;
			}
			else
			{
				_logger.LogError("Cannot generate SAS token. Account does not have access keys for blob {BlobName}.", blobName);
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}
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
			try
			{
				// Generate a SAS token for the container with write and create permissions
				var containerSasToken = GenerateContainerSasToken(
				_containerName,
				BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create,
				DateTimeOffset.UtcNow.AddHours(1)); // Token valid for 1 hour

				// Create the BlobClient using the SAS token
				var blobClient = new BlobClient(
					new Uri($"{_blobServiceClient.GetBlobContainerClient(_containerName).Uri}{containerSasToken}"));

				// Generate a unique name for the blob and get a reference to the blob client
				var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
				blobClient = blobClient.GetParentBlobContainerClient().GetBlobClient(blobName);

				_logger.LogInformation("Uploading file {FileName} as blob {BlobName} to container {ContainerName}.", file.FileName, blobName, _containerName);

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
		/// <param name="blobUri">The URI of the blob to delete.</param>
		public async Task DeleteFileAsync(string blobName)
		{
			try
			{
				// Generate a SAS token for the blob with delete permission
				var blobSasToken = GenerateBlobSasToken(
					blobName,
					BlobSasPermissions.Delete,
					DateTimeOffset.UtcNow.AddHours(1)); // Token valid for 1 hour

				// Build the blob URI using the container name and blob name
				var blobUri = new UriBuilder(_blobServiceClient.Uri)
				{
					Path = $"{_containerName}/{blobName}",
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
				var containerSasToken = GenerateContainerSasToken(
				_containerName,
				BlobContainerSasPermissions.List | BlobContainerSasPermissions.Read,
				DateTimeOffset.UtcNow.AddHours(1)); // Token valid for 1 hour

				// Create the BlobContainerClient using the SAS token
				var containerClient = new BlobContainerClient(
					new Uri($"{_blobServiceClient.GetBlobContainerClient(_containerName).Uri}{containerSasToken}"));

				_logger.LogInformation("Listing all blobs in container {ContainerName}.", _containerName);

				var blobs = new List<BlobClient>();

				// List all blobs in the container
				await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
				{
					var blobClient = containerClient.GetBlobClient(blobItem.Name);
					_logger.LogDebug("Found blob {BlobName} in container {ContainerName}.", blobItem.Name, _containerName);
					blobs.Add(blobClient);
				}

				_logger.LogInformation("Retrieved {BlobCount} blobs from container {ContainerName}.", blobs.Count, _containerName);

				// Return the list of blob URLs
				return blobs;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred while listing blobs in container {ContainerName}.", _containerName);
				throw;
			}
		}
	}
}
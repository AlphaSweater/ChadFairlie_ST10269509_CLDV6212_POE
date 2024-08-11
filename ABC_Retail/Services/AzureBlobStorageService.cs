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

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
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
		/// Uploads a file to Azure Blob Storage and returns the URL of the uploaded blob.
		/// </summary>
		/// <param name="file">The file to upload, encapsulated in an <see cref="IFormFile"/> object.</param>
		/// <returns>The URL of the uploaded file as a string.</returns>
		public async Task<string> UploadFileAsync(IFormFile file)
		{
			// Generate a SAS (Shared Access Signature) token for the container with write and create permissions.
			// This allows the client to upload files to the container without exposing the storage account keys.
			var containerSasToken = _sasTokenGenerator.GenerateContainerSasToken(
				_imageContainerName,
				BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Create);

			// Create a new BlobClient instance using the SAS token.
			// The BlobClient allows us to interact with the specified blob (file) in Azure Blob Storage.
			var blobClient = new BlobClient(
				new Uri($"{_blobServiceClient.GetBlobContainerClient(_imageContainerName).Uri}{containerSasToken}"));

			// Generate a unique name for the blob by combining a new GUID with the file extension.
			// This ensures that each file uploaded to the container has a unique name, preventing overwrites.
			var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

			// Obtain a reference to the BlobClient associated with the generated blob name.
			// The GetParentBlobContainerClient method is used to ensure we're working with the correct container.
			blobClient = blobClient.GetParentBlobContainerClient().GetBlobClient(blobName);

			try
			{
				// Log the intention to upload the file. This log entry includes the original file name, the generated blob name,
				// and the container name, providing valuable information for debugging and audit purposes.
				_logger.LogInformation("Uploading file {FileName} as blob {BlobName} to container {ContainerName}.", file.FileName, blobName, _imageContainerName);

				// Open a stream to read the content of the file and upload it to the blob in Azure Blob Storage.
				// The 'true' parameter in UploadAsync ensures that any existing blob with the same name will be overwritten.
				using (var stream = file.OpenReadStream())
				{
					await blobClient.UploadAsync(stream, true);
				}

				// Log the successful upload of the file. The log entry includes the original file name and the URI of the uploaded blob.
				_logger.LogInformation("File {FileName} uploaded successfully to {BlobUri}.", file.FileName, blobClient.Uri);

				// Return the URI of the uploaded blob as a string. This URI can be used to access the uploaded file.
				return blobClient.Uri.ToString();
			}
			catch (Exception ex)
			{
				// Log the error that occurred during the upload process, including the original file name.
				// The exception is re-thrown to ensure that the calling code is aware of the failure.
				_logger.LogError(ex, "An error occurred while uploading file {FileName}.", file.FileName);
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Deletes a file (blob) from Azure Blob Storage.
		/// </summary>
		/// <param name="blobName">The name of the blob to delete.</param>
		public async Task DeleteFileAsync(string blobName)
		{
			try
			{
				// Generate a SAS (Shared Access Signature) token for the specific blob with delete permissions.
				// This allows the client to delete the blob without exposing the storage account keys.
				var blobSasToken = _sasTokenGenerator.GenerateBlobSasToken(
					_imageContainerName,
					blobName,
					BlobSasPermissions.Delete);

				// Build the URI (Uniform Resource Identifier) for the blob by combining the container name and blob name.
				// The SAS token is appended to the URI query string, granting the necessary permissions to perform the delete operation.
				var blobUri = new UriBuilder(_blobServiceClient.Uri)
				{
					Path = $"{_imageContainerName}/{blobName}",
					Query = blobSasToken
				}.Uri;

				// Create a new BlobClient instance using the constructed URI with the SAS token.
				// The BlobClient is used to interact with the blob (file) in Azure Blob Storage.
				var blobClient = new BlobClient(blobUri);

				// Log the intention to delete the blob. This log entry includes the blob name and its URI,
				// providing valuable information for debugging and audit purposes.
				_logger.LogInformation("Deleting blob {BlobName} from {BlobUri}.", blobName, blobUri);

				// Attempt to delete the blob from Azure Blob Storage. If the blob does not exist, the method returns false.
				// The DeleteIfExistsAsync method ensures that no exception is thrown if the blob is not found.
				await blobClient.DeleteIfExistsAsync();

				// Log the successful deletion of the blob. The log entry includes the blob name and its URI.
				_logger.LogInformation("Blob {BlobName} deleted successfully from {BlobUri}.", blobName, blobUri);
			}
			catch (Exception ex)
			{
				// Log any error that occurs during the deletion process, including the blob name.
				// The exception is re-thrown to ensure that the calling code is aware of the failure.
				_logger.LogError(ex, "An error occurred while deleting blob {BlobName}.", blobName);
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Lists all files (blobs) in the specified container in Azure Blob Storage.
		/// </summary>
		/// <returns>A list of <see cref="BlobClient"/> items representing each blob in the container.</returns>
		public async Task<IEnumerable<BlobClient>> ListFilesAsync()
		{
			try
			{
				// Generate a SAS (Shared Access Signature) token for the container with permissions to list and read blobs.
				// This allows secure access to enumerate and retrieve the blobs within the container without exposing storage account keys.
				var containerSasToken = _sasTokenGenerator.GenerateContainerSasToken(
					_imageContainerName,
					BlobContainerSasPermissions.List | BlobContainerSasPermissions.Read);

				// Create a BlobContainerClient using the container's URI and the generated SAS token.
				// The BlobContainerClient provides operations for interacting with the blobs in the container.
				var containerClient = new BlobContainerClient(
					new Uri($"{_blobServiceClient.GetBlobContainerClient(_imageContainerName).Uri}{containerSasToken}"));

				// Log the initiation of the blob listing process.
				// This log entry includes the name of the container being accessed.
				_logger.LogInformation("Listing all blobs in container {ContainerName}.", _imageContainerName);

				// Initialize a list to store BlobClient instances for each blob found in the container.
				var blobs = new List<BlobClient>();

				// Enumerate all blobs in the container asynchronously using the GetBlobsAsync method.
				// This method returns a stream of BlobItem instances representing each blob in the container.
				await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
				{
					// For each blob found, create a BlobClient instance to interact with that specific blob.
					// The BlobClient provides methods to perform operations on the individual blob.
					var blobClient = containerClient.GetBlobClient(blobItem.Name);

					// Log the discovery of each blob, including its name and the container name.
					_logger.LogDebug("Found blob {BlobName} in container {ContainerName}.", blobItem.Name, _imageContainerName);

					// Add the BlobClient instance to the list of blobs.
					blobs.Add(blobClient);
				}

				// Log the total number of blobs retrieved from the container.
				// This provides valuable feedback on the success and scope of the operation.
				_logger.LogInformation("Retrieved {BlobCount} blobs from container {ContainerName}.", blobs.Count, _imageContainerName);

				// Return the list of BlobClient instances representing each blob in the container.
				return blobs;
			}
			catch (Exception ex)
			{
				// Log any error that occurs during the blob listing process, including the container name.
				// The exception is re-thrown to ensure that the calling code is aware of the failure.
				_logger.LogError(ex, "An error occurred while listing blobs in container {ContainerName}.", _imageContainerName);
				throw;
			}
		}
	}

	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
	//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

	/// <summary>
	/// Generates SAS (Shared Access Signature) tokens for Azure Blob Storage access.
	/// </summary>
	public class SasTokenGenerator
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly ILogger<SasTokenGenerator> _logger;
		private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		/// <summary>
		/// Initializes a new instance of the <see cref="SasTokenGenerator"/> class.
		/// </summary>
		/// <param name="blobServiceClient">The BlobServiceClient instance used to interact with Azure Blob Storage.</param>
		/// <param name="logger">The logger used to record information and errors.</param>
		public SasTokenGenerator(BlobServiceClient blobServiceClient, ILogger<SasTokenGenerator> logger)
		{
			_blobServiceClient = blobServiceClient;
			_logger = logger;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to generate SAS tokens
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Generates a SAS token for a container.
		/// </summary>
		/// <param name="containerName">The name of the container for which the SAS token is generated.</param>
		/// <param name="permissions">The permissions to grant with the SAS token, e.g., read, write.</param>
		/// <param name="expiration">The expiration time for the SAS token. If null, uses a default expiration time.</param>
		/// <returns>The generated SAS token as a query string.</returns>
		public string GenerateContainerSasToken(string containerName, BlobContainerSasPermissions permissions, TimeSpan? expiration = null)
		{
			// Get a BlobContainerClient instance for the specified container.
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

			// Check if the container client can generate a SAS URI.
			// This requires the storage account to have access keys.
			if (!containerClient.CanGenerateSasUri)
			{
				_logger.LogError("Cannot generate SAS token. Account does not have access keys for container {ContainerName}.", containerName);
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}

			// Generate the SAS token with the specified permissions and expiration time.
			// The token is valid until the provided expiration time or the default expiration if none is provided.
			var sasToken = containerClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration));
			return sasToken.Query;
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Generates a SAS token for a blob.
		/// </summary>
		/// <param name="containerName">The name of the container that contains the blob.</param>
		/// <param name="blobName">The name of the blob for which the SAS token is generated.</param>
		/// <param name="permissions">The permissions to grant with the SAS token, e.g., read, write.</param>
		/// <param name="expiration">The expiration time for the SAS token. If null, uses a default expiration time.</param>
		/// <returns>The generated SAS token as a query string.</returns>
		public string GenerateBlobSasToken(string containerName, string blobName, BlobSasPermissions permissions, TimeSpan? expiration = null)
		{
			// Get a BlobContainerClient instance for the specified container.
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

			// Get a BlobClient instance for the specified blob within the container.
			var blobClient = containerClient.GetBlobClient(blobName);

			// Check if the blob client can generate a SAS URI.
			// This requires the storage account to have access keys.
			if (!blobClient.CanGenerateSasUri)
			{
				_logger.LogError("Cannot generate SAS token. Account does not have access keys for blob {BlobName}.", blobName);
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}

			// Generate the SAS token with the specified permissions and expiration time.
			// The token is valid until the provided expiration time or the default expiration if none is provided.
			var sasToken = blobClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration));
			return sasToken.Query;
		}
	}
}
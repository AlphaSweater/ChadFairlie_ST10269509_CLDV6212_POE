using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;

namespace ABC_Retail_Shared.Services
{
	/// <summary>
	/// Provides methods to interact with Azure Blob Storage, including uploading, deleting, and listing files.
	/// </summary>
	public class AzureBlobStorageService
	{
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Fields and Dependencies
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		// The BlobServiceClient instance used to interact with Azure Blob Storage.
		private readonly BlobServiceClient _blobServiceClient;

		// The SasTokenGenerator instance used to generate SAS tokens for Azure Blob Storage access.
		private readonly SasTokenGenerator _sasTokenGenerator;

		// The name of the container used to store image files.
		private readonly string _imageContainerName = "imagefiles";

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		/// <summary>
		/// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
		/// </summary>
		public AzureBlobStorageService(BlobServiceClient blobServiceClient, SasTokenGenerator sasTokenGenerator)
		{
			// Initialize the BlobServiceClient, SasTokenGenerator, and Logger
			_blobServiceClient = blobServiceClient;
			_sasTokenGenerator = sasTokenGenerator;
		}

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Methods to interact with Azure Blob Storage
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//

		//TODO: Fix

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Uploads a file to Azure Blob Storage and returns the new filename of the uploaded blob.
		/// </summary>
		/// <param name="file">The file to upload, encapsulated in an <see cref="IFormFile"/> object.</param>
		/// <returns>The new filename of the uploaded file as a string.</returns>
		public async Task<string> UploadFileAsync(IFormFile file)
		{
			// Generate a unique name for the blob by combining a new GUID with the file extension.
			var blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

			// Get a reference to the container client.
			var containerClient = _blobServiceClient.GetBlobContainerClient(_imageContainerName);

			// Get a reference to the blob client.
			var blobClient = containerClient.GetBlobClient(blobName);

			try
			{
				// Open a stream to read the content of the file and upload it to the blob in Azure Blob Storage.
				using (var stream = file.OpenReadStream())
				{
					await blobClient.UploadAsync(stream, true);
				}

				// Return the new filename of the uploaded blob.
				return blobName;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		/// <summary>
		/// Retrieves a BlobClient for a specific blob in Azure Blob Storage.
		/// </summary>
		/// <param name="blobName">The name of the blob to retrieve.</param>
		/// <returns>A <see cref="BlobClient"/> object representing the blob.</returns>
		public BlobClient GetFile(string blobName)
		{
			try
			{
				// Generate a SAS (Shared Access Signature) token for the specific blob with read permissions.
				// This allows the client to access the blob without exposing the storage account keys.
				var blobSasToken = _sasTokenGenerator.GenerateBlobSasToken(
					_imageContainerName,
					blobName,
					BlobSasPermissions.Read);

				// Build the URI for the blob by combining the container name and blob name.
				// The SAS token is appended to the URI query string, granting the necessary permissions to access the blob.
				var blobUri = new UriBuilder(_blobServiceClient.Uri)
				{
					Path = $"{_imageContainerName}/{blobName}",
					Query = blobSasToken
				}.Uri;

				// Create a new BlobClient instance using the constructed URI with the SAS token.
				var blobClient = new BlobClient(blobUri);

				// Return the BlobClient instance representing the blob.
				return blobClient;
			}
			catch (Exception ex)
			{
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
				// Attempt to delete the blob from Azure Blob Storage. If the blob does not exist, the method returns false.
				// The DeleteIfExistsAsync method ensures that no exception is thrown if the blob is not found.
				await blobClient.DeleteIfExistsAsync();
			}
			catch (Exception ex)
			{
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

				// Initialize a list to store BlobClient instances for each blob found in the container.
				var blobs = new List<BlobClient>();

				// Enumerate all blobs in the container asynchronously using the GetBlobsAsync method.
				// This method returns a stream of BlobItem instances representing each blob in the container.
				await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
				{
					// For each blob found, create a BlobClient instance to interact with that specific blob.
					// The BlobClient provides methods to perform operations on the individual blob.
					var blobClient = containerClient.GetBlobClient(blobItem.Name);

					// Add the BlobClient instance to the list of blobs.
					blobs.Add(blobClient);
				}

				// Return the list of BlobClient instances representing each blob in the container.
				return blobs;
			}
			catch (Exception ex)
			{
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
		private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(1);

		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		// Constructor
		//<><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><><>//
		/// <summary>
		/// Initializes a new instance of the <see cref="SasTokenGenerator"/> class.
		/// </summary>
		/// <param name="blobServiceClient">The BlobServiceClient instance used to interact with Azure Blob Storage.</param>
		/// <param name="logger">The logger used to record information and errors.</param>
		public SasTokenGenerator(BlobServiceClient blobServiceClient)
		{
			_blobServiceClient = blobServiceClient;
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
				throw new InvalidOperationException("Cannot generate SAS token. Account does not have access keys.");
			}

			// Generate the SAS token with the specified permissions and expiration time.
			// The token is valid until the provided expiration time or the default expiration if none is provided.
			var sasToken = blobClient.GenerateSasUri(permissions, DateTimeOffset.UtcNow.Add(expiration ?? _defaultExpiration));
			return sasToken.Query;
		}
	}
}
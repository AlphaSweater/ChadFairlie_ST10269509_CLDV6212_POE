using ABC_Retail.ViewModels;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ABC_Retail_Functions.Services
{
	public class AzureFileStorageService
	{
		private readonly string _connectionString;

		public AzureFileStorageService(string connectionString)
		{
			_connectionString = connectionString;
		}

		// Upload a file to Azure Files
		public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string shareName)
		{
			var shareClient = new ShareClient(_connectionString, shareName);
			var directoryClient = shareClient.GetRootDirectoryClient();

			var fileClient = directoryClient.GetFileClient(fileName);
			await fileClient.CreateAsync(fileStream.Length);
			await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);

			return fileClient.Uri.ToString();
		}

		// Get details of a single file from Azure Files
		public async Task<ShareFileProperties> GetFileDetailsAsync(string fileName, string shareName)
		{
			var shareClient = new ShareClient(_connectionString, shareName);
			var directoryClient = shareClient.GetRootDirectoryClient();

			var fileClient = directoryClient.GetFileClient(fileName);
			var properties = await fileClient.GetPropertiesAsync();

			return properties.Value;
		}

		// Delete a file from Azure Files
		public async Task DeleteFileAsync(string fileName, string shareName)
		{
			var shareClient = new ShareClient(_connectionString, shareName);
			var directoryClient = shareClient.GetRootDirectoryClient();

			var fileClient = directoryClient.GetFileClient(fileName);
			await fileClient.DeleteAsync();
		}

		// Download a file from Azure Files
		public async Task<Stream> DownloadFileAsync(string fileName, string shareName)
		{
			var shareClient = new ShareClient(_connectionString, shareName);
			var directoryClient = shareClient.GetRootDirectoryClient();

			var fileClient = directoryClient.GetFileClient(fileName);
			var downloadResponse = await fileClient.DownloadAsync();

			return downloadResponse.Value.Content;
		}

		// List all files in Azure Files
		public async Task<IEnumerable<ContractFileViewModel>> ListFilesAsync(string shareName)
		{
			var shareClient = new ShareClient(_connectionString, shareName);
			var directoryClient = shareClient.GetRootDirectoryClient();

			var files = new List<ContractFileViewModel>();

			await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
			{
				if (!item.IsDirectory)
				{
					var fileClient = directoryClient.GetFileClient(item.Name);
					var properties = await fileClient.GetPropertiesAsync();

					files.Add(new ContractFileViewModel
					{
						FileName = item.Name,
						Url = fileClient.Uri.ToString(),
						FileSize = properties.Value.ContentLength,
						LastModified = properties.Value.LastModified
					});
				}
			}

			return files;
		}
	}
}
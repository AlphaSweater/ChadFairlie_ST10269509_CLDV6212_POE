using ABC_Retail.Models;
using ABC_Retail.Services;
using ABC_Retail.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ABC_Retail.Controllers
{
	public class ContractController : Controller
	{
		private readonly AzureFileStorageService _fileStorageService;
		private readonly HttpClient _httpClient;
		private readonly string _uploadFileFunctionUrl;
		private readonly string _contractShareName = "contracts";

		public ContractController(AzureFileStorageService fileStorageService, HttpClient httpClient, IConfiguration configuration)
		{
			_fileStorageService = fileStorageService;
			_httpClient = httpClient;
			_uploadFileFunctionUrl = configuration["AzureFunctions:UploadFileFunctionUrl"] ?? throw new ArgumentNullException(nameof(configuration), "UploadFileFunctionUrl configuration is missing.");
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// List all contracts
		public async Task<IActionResult> Index()
		{
			var contractFiles = await _fileStorageService.ListFilesAsync(_contractShareName);
			return View(contractFiles);
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Upload a new contract file
		/// <summary>
		/// Handles the upload of a new contract file.
		/// </summary>
		/// <param name="file">The file to be uploaded.</param>
		/// <returns>JSON result indicating success or failure of the upload operation.</returns>
		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file)
		{
			// Check if a file is provided and is not empty
			if (file == null || file.Length == 0)
			{
				return Json(new { success = false, message = "No file selected or file is empty." });
			}

			// Validate the file extension
			var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

			if (!allowedExtensions.Contains(extension))
			{
				return Json(new { success = false, message = "Invalid file type. Only PDF and Word documents are allowed." });
			}

			// Get the URL of the Azure Function to upload the file
			var functionUrl = _uploadFileFunctionUrl;

			// Create a multipart form content to send the file
			using var content = new MultipartFormDataContent();
			using var fileStream = file.OpenReadStream();
			var fileContent = new StreamContent(fileStream);
			content.Add(fileContent, "file", file.FileName);

			// Send the file to the Azure Function
			var response = await _httpClient.PostAsync(functionUrl, content);

			// Check if the upload was successful
			if (!response.IsSuccessStatusCode)
			{
				return Json(new { success = false, message = "Failed to upload the file." });
			}

			return Json(new { success = true, message = "File uploaded successfully." });
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Display a Contract in detail
		public async Task<IActionResult> Details(string fileName)
		{
			var fileDetails = await _fileStorageService.GetFileDetailsAsync(fileName, _contractShareName);

			return View(new ContractFileViewModel
			{
				FileName = fileName,
				Url = Url.Action("Download", new { fileName }), // For download
				InlineUrl = Url.Action("InlineView", new { fileName }), // For inline view
				FileSize = fileDetails.ContentLength,
				LastModified = fileDetails.LastModified
			});
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Inline view of a contract file
		public async Task<IActionResult> InlineView(string fileName)
		{
			var stream = await _fileStorageService.DownloadFileAsync(fileName, _contractShareName);
			return File(stream, "application/pdf");
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Download a contract file
		public async Task<IActionResult> Download(string fileName)
		{
			var stream = await _fileStorageService.DownloadFileAsync(fileName, _contractShareName);

			if (stream == null)
			{
				return NotFound();
			}

			// Set the correct content type and force download
			return File(stream, "application/octet-stream", fileName);
		}

		//--------------------------------------------------------------------------------------------------------------------------//
		// Delete a contract file
		[HttpPost]
		public async Task<IActionResult> Delete(string fileName)
		{
			await _fileStorageService.DeleteFileAsync(fileName, _contractShareName);
			return RedirectToAction("Index");
		}
	}
}